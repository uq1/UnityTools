﻿// Disable 'obsolete' warnings
#pragma warning disable 0618

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using static ProgressBarAPI.API;

namespace GenerateNormals
{
    public class GenerateNormals : EditorWindow
    {
        [MenuItem("Window/Unique Tools/Generate Normals")]
        static void Open()
        {
            GetWindow<GenerateNormals>("GenerateNormals").Show();
        }

        private bool enhanced_method = true;
        private float smoothing_angle = 60.0f;

        void OnGUI()
        {
            enhanced_method = EditorGUILayout.Toggle("Use Enhanced Method?", enhanced_method);
            smoothing_angle = EditorGUILayout.FloatField("Smoothing Angle", smoothing_angle);

            if (GUILayout.Button("Generate Normals"))
            {
                GenerateMeshNormals();
            }
        }

        void GenerateMeshNormals()
        {
            try
            {
                ProgressBarInit("Generating Normals...");

                int totalObjects = Selection.gameObjects.Length;
                int numCompleted = 0;

                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    var gameObject = Selection.gameObjects[i];

                    ProgressBarShow("Generating Normals in \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                    if (gameObject.GetComponent<MeshFilter>())
                    {
                        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;

                        if (enhanced_method)
                        {
                            NormalSolver.RecalculateNormals(mesh, smoothing_angle);
                        }
                        else
                        {
                            mesh.RecalculateNormals(smoothing_angle);
                        }

                        mesh.RecalculateTangents();
                        mesh.UploadMeshData(false);
                    }
                    
                    if (gameObject.GetComponent<SkinnedMeshRenderer>())
                    {
                        Mesh mesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;

                        if (enhanced_method)
                        {
                            NormalSolver.RecalculateNormals(mesh, smoothing_angle);
                        }
                        else
                        {
                            mesh.RecalculateNormals(smoothing_angle);
                        }

                        mesh.RecalculateTangents();
                        mesh.UploadMeshData(false);
                    }

                    numCompleted++;
                }

                ProgressBarEnd();
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception thrown when executing: " + ex.Message);
            }

            ProgressBarEnd();
        }
    }

    public static class NormalSolver
    {
        /// <summary>
        ///     Recalculate the normals of a mesh based on an angle threshold. This takes
        ///     into account distinct vertices that have the same position.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="angle">
        ///     The smoothing angle. Note that triangles that already share
        ///     the same vertex will be smooth regardless of the angle! 
        /// </param>
        public static void RecalculateNormals(this Mesh mesh, float angle)
        {
            var cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);

            var vertices = mesh.vertices;
            var normals = new Vector3[vertices.Length];

            // Holds the normal of each triangle in each sub mesh.
            var triNormals = new Vector3[mesh.subMeshCount][];

            var dictionary = new Dictionary<VertexKey, List<VertexEntry>>(vertices.Length);

            for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
            {

                var triangles = mesh.GetTriangles(subMeshIndex);

                triNormals[subMeshIndex] = new Vector3[triangles.Length / 3];

                for (var i = 0; i < triangles.Length; i += 3)
                {
                    int i1 = triangles[i];
                    int i2 = triangles[i + 1];
                    int i3 = triangles[i + 2];

                    // Calculate the normal of the triangle
                    Vector3 p1 = vertices[i2] - vertices[i1];
                    Vector3 p2 = vertices[i3] - vertices[i1];
                    Vector3 normal = Vector3.Cross(p1, p2).normalized;
                    int triIndex = i / 3;
                    triNormals[subMeshIndex][triIndex] = normal;

                    List<VertexEntry> entry;
                    VertexKey key;

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i1]), out entry))
                    {
                        entry = new List<VertexEntry>(4);
                        dictionary.Add(key, entry);
                    }
                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i1));

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i2]), out entry))
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }
                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i2));

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i3]), out entry))
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }
                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i3));
                }
            }

            // Each entry in the dictionary represents a unique vertex position.

            foreach (var vertList in dictionary.Values)
            {
                for (var i = 0; i < vertList.Count; ++i)
                {

                    var sum = new Vector3();
                    var lhsEntry = vertList[i];

                    for (var j = 0; j < vertList.Count; ++j)
                    {
                        var rhsEntry = vertList[j];

                        if (lhsEntry.VertexIndex == rhsEntry.VertexIndex)
                        {
                            sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                        }
                        else
                        {
                            // The dot product is the cosine of the angle between the two triangles.
                            // A larger cosine means a smaller angle.
                            var dot = Vector3.Dot(
                                triNormals[lhsEntry.MeshIndex][lhsEntry.TriangleIndex],
                                triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                            if (dot >= cosineThreshold)
                            {
                                sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                            }
                        }
                    }

                    sum = sum.normalized;

                    /*if (sum.x == 0.0f && sum.y == 0.0f && sum.z == 0.0f)
                    {
                        sum.x = 0.0f;
                        sum.y = 1.0f;
                        sum.z = 0.0f;
                        sum = sum.normalized;
                    }*/

                    normals[lhsEntry.VertexIndex] = sum.normalized;
                }
            }

            // Because it is possible to get 0,0,0 normals using cross product, this is very bad... Find them and use the closest valid normal...
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 n = normals[i];

                if (n.x == 0.0f && n.y == 0.0f && n.z == 0.0f)
                {
                    int closest = 0;
                    float closestDist = 99999.9f;

                    for (int j = 0; j < normals.Length; j++)
                    {
                        Vector3 n2 = normals[j];

                        if (!(n2.x == 0.0f && n2.y == 0.0f && n2.z == 0.0f))
                        {
                            float dist = Vector3.Distance(vertices[i], vertices[j]);

                            if (dist < closestDist)
                            {
                                closest = j;
                                closestDist = dist;
                            }
                        }
                        
                    }

                    normals[i] = normals[closest];
                }
            }

            mesh.normals = normals;
        }

        private struct VertexKey
        {
            private readonly long _x;
            private readonly long _y;
            private readonly long _z;

            // Change this if you require a different precision.
            private const int Tolerance = 100000;

            // Magic FNV values. Do not change these.
            private const long FNV32Init = 0x811c9dc5;
            private const long FNV32Prime = 0x01000193;

            public VertexKey(Vector3 position)
            {
                _x = (long)(Mathf.Round(position.x * Tolerance));
                _y = (long)(Mathf.Round(position.y * Tolerance));
                _z = (long)(Mathf.Round(position.z * Tolerance));
            }

            public override bool Equals(object obj)
            {
                var key = (VertexKey)obj;
                return _x == key._x && _y == key._y && _z == key._z;
            }

            public override int GetHashCode()
            {
                long rv = FNV32Init;
                rv ^= _x;
                rv *= FNV32Prime;
                rv ^= _y;
                rv *= FNV32Prime;
                rv ^= _z;
                rv *= FNV32Prime;

                return rv.GetHashCode();
            }
        }

        private struct VertexEntry
        {
            public int MeshIndex;
            public int TriangleIndex;
            public int VertexIndex;

            public VertexEntry(int meshIndex, int triIndex, int vertIndex)
            {
                MeshIndex = meshIndex;
                TriangleIndex = triIndex;
                VertexIndex = vertIndex;
            }
        }
    }
}
