// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using static ProgressBarAPI.API;

namespace SkinnedMeshCollider
{
    [ExecuteInEditMode]
    public class SkinnedMeshCollider : EditorWindow
    {
        private MeshCollider MeshCollider;

        [MenuItem("Window/Unique Tools/Create SkinnedMesh Collider")]
        static void Open()
        {
            GetWindow<SkinnedMeshCollider>("SkinnedMeshCollider").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Create SkinnedMesh Collider"))
            {
                CreateSkinnedMeshCollider();
            }
        }

        public void CreateSkinnedMeshCollider()
        {
            ProgressBarInit("Creating SkinnedMesh Collider...");

            int totalObjects = Selection.gameObjects.Length;
            int numCompleted = 0;

            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                var gameObject = Selection.gameObjects[i];

                ProgressBarShow("Creating SkinnedMesh Collider for \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                if (gameObject.GetComponent<SkinnedMeshRenderer>())
                {
                    MakeSureColliderExists(gameObject);
                }

                numCompleted++;
            }

            ProgressBarEnd();
        }
        
        private bool MakeSureColliderExists(GameObject obj) 
        {
            if (obj == null) return false;
            
            MeshCollider = obj.GetComponent<MeshCollider>();
            
            if (MeshCollider == null)
            {
                MeshCollider = obj.AddComponent<MeshCollider>();
            }
            
            // Create a new collider mesh...
            Mesh colliderMesh = new Mesh();
            obj.GetComponent<SkinnedMeshRenderer>().BakeMesh(colliderMesh);
            MeshCollider.sharedMesh = null;
            MeshCollider.sharedMesh = colliderMesh;
            
            Debug.Log("Generated a mesh collider for skinned mesh " + obj.name + ".");

            return true;
        }
    }
}
