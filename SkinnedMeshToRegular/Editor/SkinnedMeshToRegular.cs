// Disable 'obsolete' warnings
#pragma warning disable 0618

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ProgressBarAPI.API;

namespace SkinnedMeshToRegular
{
    [ExecuteInEditMode]
    public class SkinnedMeshToRegular : EditorWindow
    {
        [MenuItem("Window/Unique Tools/SkinnedMesh To Regular")]
        static void Open()
        {
            GetWindow<SkinnedMeshToRegular>("SkinnedMeshToRegular").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Convert Selected SkinnedMeshes To Regular"))
            {
                ConvertSelectedSkinnedMeshToRegular();
            }
            if (GUILayout.Button("Convert all SkinnedMeshes To Regular"))
            {
                ConvertAllSkinnedMeshToRegular();
            }
        }

        public void ConvertSelectedSkinnedMeshToRegular()
        {
            ProgressBarInit("Converting SkinnedMeshes...");

            int totalObjects = Selection.gameObjects.Length;
            int numCompleted = 0;

            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                var obj = Selection.gameObjects[i];

                ProgressBarShow("Converting SkinnedMeshes in \"" + obj.name + "\".", (float)numCompleted / (float)totalObjects);

                var oldRenderer = obj.GetComponent<SkinnedMeshRenderer>();

                if (oldRenderer != null && oldRenderer.enabled)
                {
                    if (ConvertToRegularMesh(obj))
                    {
                        
                    }
                }

                numCompleted++;
            }

            ProgressBarEnd();

            AssetDatabase.SaveAssets();
        }
        
        public void ConvertAllSkinnedMeshToRegular()
        {
            ProgressBarInit("Converting SkinnedMeshes...");

            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Converting SkinnedMeshes in \"" + obj.name + "\".", (float)numCompleted / (float)totalObjects);

                var oldRenderer = obj.GetComponent<SkinnedMeshRenderer>();

                if (oldRenderer != null && oldRenderer.enabled)
                {
                    if (ConvertToRegularMesh(obj))
                    {
                        
                    }
                }

                numCompleted++;
            }

            ProgressBarEnd();

            AssetDatabase.SaveAssets();
        }
        
        private bool ConvertToRegularMesh(GameObject obj) 
        {
            if (obj == null) return false;

            if (!AssetDatabase.IsValidFolder("Assets/Skinned2Regular Meshes"))
                AssetDatabase.CreateFolder("Assets", "Skinned2Regular Meshes");

            var oldRenderer = obj.GetComponent<SkinnedMeshRenderer>();

            MeshFilter MF = obj.AddComponent<MeshFilter>();

            Mesh newMesh = new Mesh();
            newMesh.indexFormat = oldRenderer.sharedMesh.indexFormat;

            oldRenderer.BakeMesh(newMesh);
            newMesh.name = oldRenderer.sharedMesh.name + "_Baked";

            string prefabPath = "Assets/Skinned2Regular Meshes/" + newMesh.name + ".asset";

            MF.sharedMesh = newMesh;

            // Build a new materials list...
            Material[] newMats = new Material[(oldRenderer.sharedMaterials != null && oldRenderer.sharedMaterials.Length > 0) ? oldRenderer.sharedMaterials.Length : 1];

            if (oldRenderer.sharedMaterials != null)
            {
                // Build a completely new list of materials, and copy the original list to it, because, we have no direct access...
                for (int j = 0; j < oldRenderer.sharedMaterials.Length; j++)
                {
                    newMats[j] = oldRenderer.sharedMaterials[j];
                }
            }
            else if (oldRenderer.materials != null) // legacy
            {
                // Build a completely new list of materials, and copy the original list to it, because, we have no direct access...
                for (int j = 0; j < oldRenderer.materials.Length; j++)
                {
                    newMats[j] = oldRenderer.materials[j];
                }
            }
            else if (oldRenderer.sharedMaterial != null)
            {
                // If it's not a list...
                newMats[0] = oldRenderer.sharedMaterial;
            }
            else if (oldRenderer.material != null) // legacy
            {
                // If it's not a list...
                newMats[0] = oldRenderer.material;
            }
            else
            {
                // No materials??!?!?!?
                newMats[0] = oldRenderer.material;
            }

            oldRenderer.enabled = false;
            DestroyImmediate(oldRenderer, false);

            MeshRenderer MR = obj.AddComponent<MeshRenderer>();
            MR.sharedMaterials = newMats;

            Debug.Log("Generated a static mesh for skinned mesh " + obj.name + ".");

            AssetDatabase.CreateAsset(newMesh, prefabPath);

            return true;
        }
    }
}
