using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MeshVerticalLimiter
{
    public class MeshVerticalLimiter : EditorWindow
    {
        float MeshMinHeight = -9999.0f;
        float MeshMaxHeight = 9999.0f;

        void ProgressBarInit(string startText)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayProgressBar(startText, startText, 0);
        }
        void ProgressBarShow(string text, float percent)
        {
            EditorUtility.DisplayProgressBar(text, text, percent);
        }
        public void ProgressBarEnd(bool freeAreas = true)
        {
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Window/Unique Tools/Mesh Vertical Limiter")]
        static void Open()
        {
            GetWindow<MeshVerticalLimiter>("MeshVerticalLimiter").Show();
        }

        void OnGUI()
        {
            MeshMinHeight = EditorGUILayout.FloatField("Min Height", MeshMinHeight);
            MeshMaxHeight = EditorGUILayout.FloatField("Max Height", MeshMaxHeight);

            if (GUILayout.Button("Cull Meshes"))
            {
                ProcessMeshes();
            }
        }

        void ProcessMeshes()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError("MeshVerticalLimiter:: No selected object.");
                return;
            }

            ProgressBarInit("Culling Meshes By Limits...");

            int totalObjects = Selection.gameObjects.Length;
            int numCompleted = 0;

            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                var gameObject = Selection.gameObjects[i];

                ProgressBarShow("Culling Mesh \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                if (gameObject.GetComponent<MeshFilter>() && gameObject.GetComponent<MeshRenderer>())
                {
                    GenerateNewMesh(gameObject);
                }

                numCompleted++;
            }

            ProgressBarEnd();
        }

        // Apply an individual transform.
        public void GenerateNewMesh(GameObject gameObject)
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshFilter == null || meshRenderer == null)
            {
                return;
            }
            if (meshFilter.sharedMesh == null)
            {
                return;
            }

            Debug.Log("MeshVerticalLimiter:: Creating new mesh for object (" + gameObject.name + ").");

            var originalMeshName = meshFilter.sharedMesh.name;

            // move required submeshes lower
            var totalsubmeshcount = meshRenderer.sharedMaterials.Length;

            List<Vector2> uv1s = new List<Vector2>();
            List<Vector2> uv2s = new List<Vector2>();
            List<Vector2> uv3s = new List<Vector2>();
            List<Vector2> uv4s = new List<Vector2>();
            meshFilter.sharedMesh.GetUVs(0, uv1s);
            meshFilter.sharedMesh.GetUVs(1, uv2s);
            meshFilter.sharedMesh.GetUVs(2, uv3s);
            meshFilter.sharedMesh.GetUVs(3, uv4s);

            var materials = meshRenderer.sharedMaterials;

            var newMesh = new Mesh();
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            if (!AssetDatabase.IsValidFolder("Assets/Height Culled Meshes"))
            {
                AssetDatabase.CreateFolder("Assets", "Height Culled Meshes");
            }

            var prefabPath = "";

            var new_mesh_name = string.Format("HeightCulledMesh_{0}_{1}_{2}",
                gameObject.name, originalMeshName, (int)Mathf.Abs(newMesh.GetHashCode()));

            if (new_mesh_name.StartsWith("HeightCulledMesh"))
            {
                Debug.Log("MeshVerticalLimiter:: Replacing existing culled mesh (" + new_mesh_name + ").");
                prefabPath = "Assets/Height Culled Meshes/" + new_mesh_name + ".asset";
            }
            else
            {
                newMesh.name = new_mesh_name;
                prefabPath = "Assets/Height Culled Meshes/" + new_mesh_name + ".asset";
            }

            List<int> newTris = new List<int>();
            List<Vector3> newVerts = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUV1s = new List<Vector2>();
            List<Vector2> newUV2s = new List<Vector2>();

            int current_vert = 0;

            for (int i = 0; i < totalsubmeshcount; i++)
            {
                var materialName = materials[i].name;

                var oldVerts = new List<Vector3>();
                meshFilter.sharedMesh.GetVertices(oldVerts);

                var vertReplacements = new List<int>();
                for (int j = 0; j < oldVerts.Count; j++)
                {
                    vertReplacements.Add(-1);
                }

                var oldTris = meshFilter.sharedMesh.GetTriangles(i);

                var oldNormals = new List<Vector3>();
                meshFilter.sharedMesh.GetNormals(oldNormals);

                bool haveUV1 = false;
                bool haveUV2 = false;

                var oldUV1s = new List<Vector2>();
                meshFilter.sharedMesh.GetUVs(0, oldUV1s);

                if (oldUV1s.Count > 0) haveUV1 = true;

                var oldUV2s = new List<Vector2>();
                meshFilter.sharedMesh.GetUVs(1, oldUV2s);

                if (oldUV2s.Count > 0) haveUV2 = true;

                for (int j = 0; j < oldTris.Length; j += 3)
                {
                    var oldTri1 = oldTris[j];
                    var oldVert1 = oldVerts[oldTri1];
                    var oldNormal1 = oldNormals[oldTri1];
                    Vector2 oldUV11 = new Vector2();
                    Vector2 oldUV21 = new Vector2();

                    var oldTri2 = oldTris[j + 1];
                    var oldVert2 = oldVerts[oldTri2];
                    var oldNormal2 = oldNormals[oldTri2];
                    Vector2 oldUV12 = new Vector2();
                    Vector2 oldUV22 = new Vector2();

                    var oldTri3 = oldTris[j + 2];
                    var oldVert3 = oldVerts[oldTri3];
                    var oldNormal3 = oldNormals[oldTri3];
                    Vector2 oldUV13 = new Vector2();
                    Vector2 oldUV23 = new Vector2();

                    if (oldVert1.y < MeshMinHeight && oldVert2.y < MeshMinHeight && oldVert3.y < MeshMinHeight)
                    {
                        continue;
                    }

                    if (oldVert1.y > MeshMaxHeight && oldVert2.y > MeshMaxHeight && oldVert3.y > MeshMaxHeight)
                    {
                        continue;
                    }

                    // First check if we already have a vert at the same position in the new mesh, re-use it if we do...
                    int found1 = vertReplacements[oldTri1];
                    int found2 = vertReplacements[oldTri2];
                    int found3 = vertReplacements[oldTri3];

                    if (haveUV1)
                    {
                        oldUV11 = oldUV1s[oldTri1];
                        oldUV12 = oldUV1s[oldTri2];
                        oldUV13 = oldUV1s[oldTri3];
                    }

                    if (haveUV2)
                    {
                        oldUV21 = oldUV2s[oldTri1];
                        oldUV22 = oldUV2s[oldTri2];
                        oldUV23 = oldUV2s[oldTri3];
                    }

                    if (found1 >= 0)
                    {// Reuse the old vert...
                        newTris.Add(found1);
                    }
                    else
                    {
                        newVerts.Add(oldVert1);
                        newNormals.Add(oldNormal1);
                        newTris.Add(current_vert);
                        if (haveUV1) newUV1s.Add(oldUV11);
                        if (haveUV2) newUV2s.Add(oldUV21);
                        current_vert++;

                        vertReplacements[oldTri1] = newVerts.Count - 1;
                    }

                    if (found2 >= 0)
                    {// Reuse the old vert...
                        newTris.Add(found2);
                    }
                    else
                    {
                        newVerts.Add(oldVert2);
                        newNormals.Add(oldNormal2);
                        newTris.Add(current_vert);
                        if (haveUV1) newUV1s.Add(oldUV12);
                        if (haveUV2) newUV2s.Add(oldUV22);
                        current_vert++;

                        vertReplacements[oldTri2] = newVerts.Count - 1;
                    }

                    if (found3 >= 0)
                    {// Reuse the old vert...
                        newTris.Add(found3);
                    }
                    else
                    {
                        newVerts.Add(oldVert3);
                        newNormals.Add(oldNormal3);
                        newTris.Add(current_vert);
                        if (haveUV1) newUV1s.Add(oldUV13);
                        if (haveUV2) newUV2s.Add(oldUV23);
                        current_vert++;

                        vertReplacements[oldTri3] = newVerts.Count - 1;
                    }
                }
            }

            newMesh.SetVertices(newVerts);
            newMesh.SetTriangles(newTris, 0);
            newMesh.SetNormals(newNormals);
            newMesh.SetUVs(0, newUV1s);
            newMesh.SetUVs(1, newUV2s);

            AssetDatabase.CreateAsset(newMesh, prefabPath);

            AssetDatabase.SaveAssets();

            meshFilter.sharedMesh = newMesh;

        }

    }
}
