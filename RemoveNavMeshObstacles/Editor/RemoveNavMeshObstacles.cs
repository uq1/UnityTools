// Disable 'obsolete' warnings
#pragma warning disable 0618

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Reflection;

using System.Threading;
using System.Threading.Tasks;
using UnityEngine.AI;

namespace RemoveNavMeshObstacles
{
    [ExecuteInEditMode]
    public class RemoveNavMeshObstacles : EditorWindow
    {
        void ProgressBarInit(string startText)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayProgressBar(startText, startText, 0);
        }
        void ProgressBarShow(string text, float percent)
        {
            EditorUtility.DisplayProgressBar(text, text, percent);
        }
        public static void ProgressBarEnd(bool freeAreas = true)
        {
            EditorUtility.ClearProgressBar();
        }

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