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

namespace SkinnedMeshCollider
{
    [ExecuteInEditMode]
    public class SkinnedMeshCollider : EditorWindow
    {
        private MeshCollider MeshCollider;

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
