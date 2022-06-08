//#define INWINDOW_PROGRESS
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

namespace RemoveInactiveMeshRenderers
{
    [ExecuteInEditMode]
    public class RemoveInactiveMeshRenderers : EditorWindow
    {
        public bool removeFilters = true;
        
#if UNITY_PROGRESSBAR
        static MethodInfo m_Display = null;

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
            Canvas.ForceUpdateCanvases();
        }
#elif INWINDOW_PROGRESS
        float progress = -1.0f;
        string progressText = "";

        void ProgressBarInit(string startText)
        {
            progress = 0.0f;
            progressText = startText;
        }
        void ProgressBarShow(string text, float percent)
        {
            progress = percent;
            progressText = text;

            //Debug.Log("prog " + progress);

            //Debug.Log("REPAINT!");
            //Repaint();
        }
        void ProgressBarEnd()
        {
            progress = -1.0f;
            progressText = "";
        }
#else //
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
#endif //INWINDOW_PROGRESS

        [MenuItem("Window/Unique Tools/Remove Inactive Mesh Renderers")]
        static void Open()
        {
            GetWindow<RemoveInactiveMeshRenderers>("RemoveInactiveMeshRenderers").Show();
        }

        void OnGUI()
        {
#if INWINDOW_PROGRESS
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 15), progress, progressText);
#endif //INWINDOW_PROGRESS

            removeFilters = EditorGUILayout.Toggle("Remove Mesh Filters", removeFilters);

            if (GUILayout.Button("Remove Inactive Mesh Renderers"))
            {
                RemoveInactiveRendererObjects();
            }
        }

        public void RemoveInactiveRendererObjects()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Removing Inactive Mesh Renderers");

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Removing Inactive Mesh Renderers", (float)numCompleted / (float)totalObjects);
                InactiveRenderers(obj);
                numCompleted++;
            }

            ProgressBarEnd();
        }
    
        public void InactiveRenderers(GameObject obj)
        {
            if (obj.GetComponents<MeshRenderer>().Length > 0)
            {
                foreach (var c in obj.GetComponents<MeshRenderer>())
                {
                    if (c != null && !c.enabled)
                    {
                        Debug.Log("Object " + c.name + " removing inactive Mesh Renderer.");
                        DestroyImmediate(c);
                        
                        if (removeFilters)
                        {
                            if (obj.GetComponents<MeshFilter>().Length > 0)
                            {
                                foreach (var f in obj.GetComponents<MeshFilter>())
                                {
                                    if (f != null)
                                    {
                                        Debug.Log("Object " + f.name + " removing inactive Mesh Filter.");
                                        DestroyImmediate(f);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            if (obj.GetComponents<SkinnedMeshRenderer>().Length > 0)
            {
                foreach (var c in obj.GetComponents<SkinnedMeshRenderer>())
                {
                    if (c != null && !c.enabled)
                    {
                        Debug.Log("Object " + c.name + " removing inactive Mesh Renderer.");
                        DestroyImmediate(c);
                    }
                }
            }
        }
    }
}