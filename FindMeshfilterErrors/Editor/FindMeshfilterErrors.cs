#define UNITY_PROGRESSBAR

// Disable 'obsolete' warnings
#pragma warning disable 0618

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using NUnit.Framework;


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

namespace FindMeshfilterErrors
{
    public class FindMeshfilterErrorsWindow : EditorWindow
    {
        [MenuItem("Window/Unique Tools/Find Meshfilter Errors")]
        static void Open()
        {
            GetWindow<FindMeshfilterErrorsWindow>("FindMeshfilterErrors").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Error Check"))
            {
                FindMeshErrors();
            }
        }

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

        void FindMeshErrors()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Checking...");

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Checking \"" + obj.name + "\"...", (float)numCompleted / (float)totalObjects);

                if (obj.GetComponent<MeshRenderer>() || obj.GetComponent<SkinnedMeshRenderer>())
                {
                    FindMeshErrors(obj);
                }

                numCompleted++;
            }

            ProgressBarEnd();
        }

        void FindMeshErrors(GameObject _target)
        {
            if (_target == null)
            {
                //Debug.Log("No target selected.");
                return;
            }

            var meshFilters = _target.GetComponentsInChildren<MeshFilter>();

            if (meshFilters == null)
            {
                Debug.Log("Object \"" + _target.name + "\" - " + "meshFilters is null.");
                return;
            }

            foreach (var meshFilter in meshFilters)
            {
                //Debug.Log("Object \"" + _target.name + "\" - " + "MeshFilter \"" + meshFilter.name + "\"");
                
                var mesh = meshFilter.sharedMesh;
                
                if (mesh == null)
                {
                    Debug.Log("Object \"" + _target.name + "\" - " + "MeshFilter \"" + meshFilter.name + "\" has no mesh.");
                    continue;
                }
                
                if (meshFilter.GetComponent<Renderer>() == null)
                {
                    Debug.Log("Object \"" + _target.name + "\" - " + "MeshFilter \"" + meshFilter.name + "\" has no renderer.");
                    continue;
                }
                
                var materials = meshFilter.GetComponent<Renderer>().sharedMaterials;
                
                if (materials == null)
                {
                    Debug.Log("Object \"" + _target.name + "\" - " + "MeshFilter \"" + meshFilter.name + "\" has no materials.");
                    continue;
                }
            }
        }
    }
}
