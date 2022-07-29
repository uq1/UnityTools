// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using static ProgressBarAPI.API;

namespace SkinnedMeshToRegular
{
    [ExecuteInEditMode]
    public class SkinnedMeshToRegular : EditorWindow
    {
        private MeshFilter MeshFilter;
        private MeshRenderer MeshRenderer;

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
                var gameObject = Selection.gameObjects[i];

                ProgressBarShow("Converting SkinnedMeshes in \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                if (gameObject.GetComponent<SkinnedMeshRenderer>())
                {
                    ConvertToRegularMesh(gameObject);
                }

                numCompleted++;
            }

            ProgressBarEnd();
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

                if (obj.GetComponent<SkinnedMeshRenderer>())
                {
                    ConvertToRegularMesh(obj);
                }

                numCompleted++;
            }

            ProgressBarEnd();
        }
        
        private bool ConvertToRegularMesh(GameObject obj) 
        {
            if (obj == null) return false;
            
            var oldRenderer = obj.GetComponent<SkinnedMeshRenderer>();

            oldRenderer.enabled = false;

            MeshFilter = obj.AddComponent<MeshFilter>();
            MeshRenderer = obj.AddComponent<MeshRenderer>();

            Mesh newMesh = new Mesh();
            oldRenderer.BakeMesh(newMesh);
            newMesh.name = oldRenderer.sharedMesh.name + "_Baked";

            // Build a completely new list of materials, and copy the original list to it, because, we have no direct access...
            Material[] newMats = new Material[oldRenderer.sharedMaterials.Length];

            for (int j = 0; j < oldRenderer.sharedMaterials.Length; j++)
            {
                newMats[j] = oldRenderer.sharedMaterials[j];
            }

            MeshFilter.sharedMesh = newMesh;
            MeshRenderer.sharedMaterials = newMats;

            Debug.Log("Generated a mesh collider for skinned mesh " + obj.name + ".");

            return true;
        }
    }
}
