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

namespace RemoveAllColliders
{
    [ExecuteInEditMode]
    public class RemoveAllColliders : EditorWindow
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

        [MenuItem("Window/Unique Tools/Remove All Colliders")]
        static void Open()
        {
            GetWindow<RemoveAllColliders>("RemoveAllColliders").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Remove Colliders"))
            {
                RemoveInactiveColliderObjects();
            }
        }

        public void RemoveInactiveColliderObjects()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Removing Colliders...");

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Removing Colliders...", (float)numCompleted / (float)totalObjects);
                InactiveColliders(obj);
                numCompleted++;
            }

            ProgressBarEnd();
        }
    
        public void InactiveColliders(GameObject obj)
        {
            if (obj.GetComponents<MeshCollider>().Length > 0)
            {
                Debug.Log("Clear Colliders: Object " + obj.name + " removing " + obj.GetComponents<MeshCollider>().Length + " mesh colliders.");
                
                foreach (var c in obj.GetComponents<MeshCollider>())
                {
                    if (c != null)
                    {
                        DestroyImmediate(c);
                    }
                }
            }
            
            if (obj.GetComponents<BoxCollider>().Length > 0)
            {
                Debug.Log("Clear Colliders: Object " + obj.name + " removing " + obj.GetComponents<BoxCollider>().Length + " box colliders.");
                
                foreach (var c in obj.GetComponents<BoxCollider>())
                {
                    if (c != null)
                    {
                        DestroyImmediate(c);
                    }
                }
            }
            
            if (obj.GetComponents<SphereCollider>().Length > 0)
            {
                Debug.Log("Clear Colliders: Object " + obj.name + " removing " + obj.GetComponents<SphereCollider>().Length + " sphere colliders.");
                
                foreach (var c in obj.GetComponents<SphereCollider>())
                {
                    if (c != null)
                    {
                        DestroyImmediate(c);
                    }
                }
            }
            
            if (obj.GetComponents<WheelCollider>().Length > 0)
            {
                Debug.Log("Clear Colliders: Object " + obj.name + " removing " + obj.GetComponents<WheelCollider>().Length + " wheel colliders.");
                
                foreach (var c in obj.GetComponents<WheelCollider>())
                {
                    if (c != null)
                    {
                        DestroyImmediate(c);
                    }
                }
            }
        }
    }
}