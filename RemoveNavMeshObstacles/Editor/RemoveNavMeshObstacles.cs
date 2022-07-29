// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static ProgressBarAPI.API;

namespace RemoveNavMeshObstacles
{
    [ExecuteInEditMode]
    public class RemoveNavMeshObstacles : EditorWindow
    {
        [MenuItem("Window/Unique Tools/Remove NavMesh Obstacles")]
        static void Open()
        {
            GetWindow<RemoveNavMeshObstacles>("RemoveNavMeshObstacles").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Remove NavMesh Obstacles"))
            {
                RemoveObstacles();
            }
        }

        public void RemoveObstacles()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Removing NavMesh Obstacles...");

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Removing NavMesh Obstacles...", (float)numCompleted / (float)totalObjects);
                ClearNavmeshObstacles(obj);
                numCompleted++;
            }

            ProgressBarEnd();
        }
    
        public void ClearNavmeshObstacles(GameObject obj)
        {
            if (obj.GetComponents<NavMeshObstacle>().Length > 0)
            {
                Debug.Log("ClearNavmeshObstacles: Object " + obj.name + " removing " + obj.GetComponents<NavMeshObstacle>().Length + " old NavMeshObstacles.");

                foreach (var c in obj.GetComponents<NavMeshObstacle>())
                {
                    DestroyImmediate(c);
                }
            }
        }
    }
}