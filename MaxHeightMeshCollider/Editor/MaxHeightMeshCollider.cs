using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxHeightMeshCollider
{
    public class MaxHeightMeshCollider : EditorWindow
    {
        float MinColliderHeight = -9999.9f;
        float MaxColliderHeight = 3.0f;
        bool SimplifyMesh = true;
        float MeshSimplificationQuality = 1.0f;

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

        [MenuItem("Window/Unique Tools/Max Height Mesh Collider")]
        static void Open()
        {
            GetWindow<MaxHeightMeshCollider>("MaxHeightMeshCollider").Show();
        }

        void OnGUI()
        {
            MinColliderHeight = EditorGUILayout.FloatField("Min Collision Height", MinColliderHeight);
            MaxColliderHeight = EditorGUILayout.FloatField("Max Collision Height", MaxColliderHeight);
            EditorGUILayout.Space();
            SimplifyMesh = EditorGUILayout.Toggle("Simplify Mesh?", SimplifyMesh);

            if (SimplifyMesh)
            {
                EditorGUILayout.LabelField("Set simplification quality to 1.0 for lossless simplification.");
                MeshSimplificationQuality = EditorGUILayout.Slider("Mesh Simplification Quality", MeshSimplificationQuality, 0.0f, 1.0f);
                //MeshSimplificationQuality = EditorGUILayout.FloatField("Mesh Simplification Quality", MeshSimplificationQuality);
            }

            if (GUILayout.Button("Generate Collider Meshes"))
            {
                ProcessMeshes();
            }
        }

        void ProcessMeshes()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError("MaxHeightMeshCollider:: No selected object.");
                return;
            }

            ProgressBarInit("Creating Mesh Colliders...");

            int totalObjects = Selection.gameObjects.Length;
            int numCompleted = 0;

            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                var gameObject = Selection.gameObjects[i];

                ProgressBarShow("Creating Mesh Collider for \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                if (gameObject.GetComponent<MeshFilter>() && gameObject.GetComponent<MeshRenderer>())
                {
                    GenerateCollider(gameObject);
                }

                numCompleted++;
            }

            ProgressBarEnd();
        }

        // Apply an individual transform.
        public void GenerateCollider(GameObject gameObject)
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

            Debug.Log("MaxHeightMeshCollider:: Creating collider mesh for object (" + gameObject.name + ").");

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

            if (!AssetDatabase.IsValidFolder("Assets/Mesh Collider Meshes"))
            {
                AssetDatabase.CreateFolder("Assets", "Mesh Collider Meshes");
            }

            var prefabPath = "";

            var new_mesh_name = string.Format("ColliderMesh_{0}_{1}_{2}",
                gameObject.name, originalMeshName, (int)Mathf.Abs(newMesh.GetHashCode()));

            if (new_mesh_name.StartsWith("ColliderMesh"))
            {
                Debug.Log("MaxHeightMeshCollider:: Replacing existing split mesh (" + new_mesh_name + ").");
                prefabPath = "Assets/Mesh Collider Meshes/" + new_mesh_name + ".asset";
            }
            else
            {
                newMesh.name = new_mesh_name;
                prefabPath = "Assets/Mesh Collider Meshes/" + new_mesh_name + ".asset";
            }

            List<int> newTris = new List<int>();
            List<Vector3> newVerts = new List<Vector3>();

            List<Vector3> newNormals = new List<Vector3>();

            int current_vert = 0;

            for (int i = 0; i < totalsubmeshcount; i++)
            {
                var materialName = materials[i].name;

                var oldVerts = new List<Vector3>();
                meshFilter.sharedMesh.GetVertices(oldVerts);

                var oldTris = meshFilter.sharedMesh.GetTriangles(i);

                var oldNormals = new List<Vector3>();
                meshFilter.sharedMesh.GetNormals(oldNormals);

                for (int j = 0; j < oldTris.Length; j+=3)
                {
                    var oldTri1 = oldTris[j];
                    var oldVert1 = oldVerts[oldTri1];
                    var oldNormal1 = oldNormals[oldTri1];

                    var oldTri2 = oldTris[j+1];
                    var oldVert2 = oldVerts[oldTri2];
                    var oldNormal2 = oldNormals[oldTri2];

                    var oldTri3 = oldTris[j + 2];
                    var oldVert3 = oldVerts[oldTri3];
                    var oldNormal3 = oldNormals[oldTri3];

                    if (oldVert1.y < MinColliderHeight && oldVert2.y < MinColliderHeight && oldVert3.y < MinColliderHeight)
                    {
                        continue;
                    }

                    if (oldVert1.y > MaxColliderHeight && oldVert2.y > MaxColliderHeight && oldVert3.y > MaxColliderHeight)
                    {
                        continue;
                    }

                    newVerts.Add(oldVert1);
                    newNormals.Add(oldNormal1);
                    newTris.Add(current_vert);
                    current_vert++;

                    newVerts.Add(oldVert2);
                    newNormals.Add(oldNormal2);
                    newTris.Add(current_vert);
                    current_vert++;

                    newVerts.Add(oldVert3);
                    newNormals.Add(oldNormal3);
                    newTris.Add(current_vert);
                    current_vert++;
                }
            }

            newMesh.SetVertices(newVerts);
            newMesh.SetTriangles(newTris, 0);
            newMesh.SetNormals(newNormals);

            var collider = gameObject.GetComponent<MeshCollider>();

            if (collider == null)
            {
                collider = gameObject.AddComponent<MeshCollider>();
            }
            
            if (SimplifyMesh)
            {// Simplify the mesh before saving...
                var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
                meshSimplifier.Initialize(newMesh);

                if (MeshSimplificationQuality >= 1.0f)
                {
                    meshSimplifier.SimplifyMeshLossless();
                }
                else
                {
                    meshSimplifier.SimplifyMesh(MeshSimplificationQuality);
                }
                
                var simplifiedMesh = meshSimplifier.ToMesh();
                simplifiedMesh.name = newMesh.name;
                collider.sharedMesh = simplifiedMesh;
                AssetDatabase.CreateAsset(simplifiedMesh, prefabPath);
                AssetDatabase.SaveAssets();
            }
            else
            {// Directly save the generated mesh, no simplification...
                collider.sharedMesh = newMesh;
                AssetDatabase.CreateAsset(newMesh, prefabPath);
                AssetDatabase.SaveAssets();
            }
        }

    }
}
