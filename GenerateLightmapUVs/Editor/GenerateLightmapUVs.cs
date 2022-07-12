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
using System;

namespace GenerateLightmapUVs
{
    public class GenerateLightmapUVs : EditorWindow
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

        [MenuItem("Window/Unique Tools/Generate Lightmap UVs")]
        static void Open()
        {
            GetWindow<GenerateLightmapUVs>("GenerateLightmapUVs").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Generate Lightmap UVs"))
            {
                GenerateMeshUVs();
            }
        }

        void GenerateMeshUVs()
        {
            try
            {
                ProgressBarInit("Generating UVs...");

                int totalObjects = Selection.gameObjects.Length;
                int numCompleted = 0;

                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    var gameObject = Selection.gameObjects[i];

                    ProgressBarShow("Generating Lightmap UVs in \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                    if (gameObject.GetComponent<MeshFilter>())
                    {
                        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                        
                        if (mesh.indexFormat != UnityEngine.Rendering.IndexFormat.UInt32)
                        {// Using 16 bit indexes, use Unity's unwrapper...
                            Unwrapping.GenerateSecondaryUVSet(mesh);
                        }
                        else
                        {// Using 32 bit indexes, Unity is completely broken, use bakery's instance of xatlas to generate an atlas...
                            UnwrapParam uparams;
                            uparams = new UnwrapParam();
                            UnwrapParam.SetDefaults(out uparams);
                            xatlas.Unwrap(mesh, uparams);
                        }

                        mesh.UploadMeshData(false);
                    }
                    
                    if (gameObject.GetComponent<SkinnedMeshRenderer>())
                    {
                        Mesh mesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;

                        if (mesh.indexFormat != UnityEngine.Rendering.IndexFormat.UInt32)
                        {// Using 16 bit indexes, use Unity's unwrapper...
                            Unwrapping.GenerateSecondaryUVSet(mesh);
                        }
                        else
                        {// Using 32 bit indexes, Unity is completely broken, use bakery's instance of xatlas to generate an atlas...
                            UnwrapParam uparams;
                            uparams = new UnwrapParam();
                            UnwrapParam.SetDefaults(out uparams);
                            xatlas.Unwrap(mesh, uparams);
                        }

                        mesh.UploadMeshData(false);
                    }

                    numCompleted++;
                }

                ProgressBarEnd();
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception thrown when executing: " + ex.Message);
            }

            ProgressBarEnd();
        }
    }
}
