#define UNITY_PROGRESSBAR

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
using System.Linq;
using System.Reflection;

using System.Threading;
using System.Threading.Tasks;
using UnityEngine.AI;

namespace RemoveNavMeshObstacles
{
    [ExecuteInEditMode]
    public class RemoveNavMeshObstacles : EditorWindow
    {
#if UNITY_PROGRESSBAR
        static MethodInfo m_Display = null;
        static MethodInfo m_Clear = null;

        float progress = -1.0f;
        string progressText = "";

        void ProgressBarInit(string startText)
        {
            progress = 0.0f;
            progressText = startText;

            var type = typeof(Editor).Assembly.GetTypes().Where(t => t.Name == "AsyncProgressBar").FirstOrDefault();

            if (type != null)
            {
                m_Display = type.GetMethod("Display");
                m_Clear = type.GetMethod("Clear");
            }
        }
        void ProgressBarShow(string text, float percent)
        {
            progress = percent;
            progressText = text;

            if (m_Display != null)
            {
                m_Display.Invoke(null, new object[] { progressText, progress });
                //Debug.Log("prog " + progress);
                Canvas.ForceUpdateCanvases();
            }
        }
        void ProgressBarEnd()
        {
            progress = 0.0f;
            progressText = "";

            if (m_Display != null)
            {
                m_Display.Invoke(null, new object[] { progressText, progress });
                Canvas.ForceUpdateCanvases();
            }

            if (m_Clear != null)
            {
                m_Clear.Invoke(null, null);
            }

            m_Display = null;
        }
#else //!UNITY_PROGRESSBAR
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
#endif //!UNITY_PROGRESSBAR

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