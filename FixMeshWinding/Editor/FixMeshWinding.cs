// Disable 'obsolete' warnings
#pragma warning disable 0618

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using static ProgressBarAPI.API;

namespace FixMeshWinding
{
    public class FixMeshWinding : EditorWindow
    {
        [MenuItem("Window/Unique Tools/Fix Mesh Winding")]
        static void Open()
        {
            GetWindow<FixMeshWinding>("FixMeshWinding").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Fix Mesh Winding"))
            {
                RepairWindings();
            }
        }

        void RepairWindings()
        {
            try
            {
                ProgressBarInit("Repairing Windings...");

                int totalObjects = Selection.gameObjects.Length;
                int numCompleted = 0;

                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    var gameObject = Selection.gameObjects[i];

                    ProgressBarShow("Repairing Windings in \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                    if (gameObject.GetComponent<MeshFilter>())
                    {
                        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                        FixMesh(mesh);
                    }
                    
                    if (gameObject.GetComponent<SkinnedMeshRenderer>())
                    {
                        Mesh mesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                        FixMesh(mesh);
                    }
                    
                    numCompleted++;
                }

                ProgressBarEnd();

                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception thrown when executing: " + ex.Message);
            }

            ProgressBarEnd();
        }

        void FixMesh(Mesh mesh)
        {
            //Debug.Log("(input) mesh " + gameObject.name + " - verts " + mesh.vertices.Length + " - tris " + mesh.triangles.Length);

            var dm3 = new g3.DMesh3();

            //Debug.Log("DEBUG: gen dm3");

            for (int j = 0; j < mesh.vertices.Length; j++)
            {
                dm3.AppendVertex(mesh.vertices[j]);
                //dm3.SetVertex(j, mesh.vertices[j]);

                dm3.SetVertexNormal(j, mesh.normals[j]);

                if (mesh.colors != null && mesh.colors.Length > 0 && mesh.colors.Length >= j)
                {
                    dm3.SetVertexColor(j, mesh.colors[j]);
                }

                if (mesh.uv != null && mesh.uv.Length > 0 && mesh.uv.Length >= j)
                {
                    dm3.SetVertexUV(j, mesh.uv[j]);
                }
            }

            //Debug.Log("DEBUG: gen dm3 tris");

            for (int j = 0; j < mesh.triangles.Length; j += 3)
            {
                g3.Index3i tri;
                tri.a = mesh.triangles[j];
                tri.b = mesh.triangles[j + 1];
                tri.c = mesh.triangles[j + 2];

                //dm3.SetTriangle(j, tri);
                dm3.AppendTriangle(tri);
            }

            dm3.CompactInPlace();

            //var gsRepairRepair = new gs.MeshAutoRepair(dm3);
            //gsRepairRepair.Apply();
            //dm3 = gsRepairRepair.Mesh;


            //g3.MeshProjectionTarget target = new g3.MeshProjectionTarget(dm3, spatial);
            //r.SetProjectionTarget(target);

            /*
            var rdt = new gs.RemoveDuplicateTriangles(dm3);
            rdt.Apply();
            dm3 = new g3.DMesh3(rdt.Mesh, true);
            */

            g3.DMeshAABBTree3 spatial = new g3.DMeshAABBTree3(dm3, true);
            dm3 = new g3.DMesh3(spatial.Mesh, true);

            var gsRepairOrientation = new gs.MeshRepairOrientation(dm3, spatial);
            gsRepairOrientation.OrientComponents();
            //gsRepairOrientation.SolveGlobalOrientation();
            dm3 = new g3.DMesh3(gsRepairOrientation.Mesh, true);


            List<int> newTris = new List<int>();
            List<Vector3> newVerts = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Color> newColors = new List<Color>();
            List<Vector2> newUVs = new List<Vector2>();

            //Debug.Log("DEBUG: gen new");
            for (int j = 0; j < dm3.VertexCount; j++)
            {
                var vt = dm3.GetVertexAll(j);

                newVerts.Add((Vector3)vt.v);

                if (vt.bHaveC)
                {
                    newColors.Add(vt.c);
                }

                if (vt.bHaveN)
                {
                    newNormals.Add((Vector3)vt.n);
                }

                if (vt.bHaveUV)
                {
                    newUVs.Add((Vector2)vt.uv);
                }
            }

            //Debug.Log("DEBUG: gen new tris");
            for (int j = 0; j < dm3.TriangleCount; j++)
            {
                newTris.Add(dm3.GetTriangle(j).a);
                newTris.Add(dm3.GetTriangle(j).b);
                newTris.Add(dm3.GetTriangle(j).c);
            }

            //Debug.Log("(output) mesh " + gameObject.name + " - verts " + newVerts.Count + " - tris " + newTris.Count);

            mesh.SetVertices(newVerts);
            mesh.SetNormals(newNormals);
            mesh.SetColors(newColors);
            mesh.SetTriangles(newTris, 0);

            if (dm3.HasVertexUVs)
            {
                mesh.SetUVs(0, newUVs);
            }

            mesh.RecalculateTangents();
            mesh.UploadMeshData(false);
        }
    }
}
