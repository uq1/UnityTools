// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using static ProgressBarAPI.API;

namespace RemoveInactiveMeshColliders
{
    [ExecuteInEditMode]
    public class RemoveInactiveMeshColliders : EditorWindow
    {
        [MenuItem("Window/Unique Tools/Remove Inactive Mesh Colliders")]
        static void Open()
        {
            GetWindow<RemoveInactiveMeshColliders>("RemoveInactiveMeshColliders").Show();
        }

        void OnGUI()
        {
#if INWINDOW_PROGRESS
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 15), progress, progressText);
#endif //INWINDOW_PROGRESS

            if (GUILayout.Button("Remove Inactive Mesh Colliders"))
            {
                RemoveInactiveColliderObjects();
            }
        }

        public void RemoveInactiveColliderObjects()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Removing Inactive Mesh Colliders...");

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Removing Inactive Mesh Colliders...", (float)numCompleted / (float)totalObjects);
                InactiveColliders(obj);
                numCompleted++;
            }

            ProgressBarEnd();
        }
    
        public void InactiveColliders(GameObject obj)
        {
            if (obj.GetComponents<MeshCollider>().Length > 0)
            {
                foreach (var c in obj.GetComponents<MeshCollider>())
                {
                    if (c != null && !c.enabled)
                    {
                        Debug.Log("Clear Inactive Mesh Colliders: Object " + c.name + " removing inactive Mesh Collider.");
                        DestroyImmediate(c);
                    }
                }
            }
        }
    }
}