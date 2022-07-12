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

namespace SkinnedMeshToRegular
{
    [ExecuteInEditMode]
    public class SkinnedMeshToRegular : EditorWindow
    {
        private MeshFilter MeshFilter;
        private MeshRenderer MeshRenderer;

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
