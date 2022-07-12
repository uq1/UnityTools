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

namespace GenerateUVs
{
    public class GenerateUVs : EditorWindow
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

        [MenuItem("Window/Unique Tools/Generate UVs")]
        static void Open()
        {
            GetWindow<GenerateUVs>("GenerateUVs").Show();
        }

        private bool _smoothing = true;
        private bool _regenerateUV2 = false;

        void OnGUI()
        {
            _smoothing = EditorGUILayout.Toggle("Use Smoothing?", _smoothing);
            _regenerateUV2 = EditorGUILayout.Toggle("Regenerate Lightmap UVs?", _regenerateUV2);

            if (GUILayout.Button("Generate UVs"))
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

                    ProgressBarShow("Generating UVs in \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                    if (gameObject.GetComponent<MeshFilter>())
                    {
                        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                        Vector3[] vertices = mesh.vertices;
                        Vector3[] normals = mesh.normals;
                        List<Vector2> uvs = new List<Vector2>();

                        mesh.RecalculateBounds();
                        var bounds = mesh.bounds;

                        int j = 0;
                        while (j < vertices.Length) {
                            var y = Mathf.Abs(normals[j].y);
                            var x = Mathf.Abs(normals[j].x);

                            if (_smoothing)
                            {
                                if (y > 0.5f)
                                {                      // if normal is like vector3.up
                                    Vector2 uv;
                                    uv.x = (bounds.min.x + vertices[j].x) / bounds.max.x;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uv.x *= 1.0f - y;
                                    uv.y *= 1.0f - y;

                                    uv.x += y * (bounds.min.z + vertices[j].z) / bounds.max.z;
                                    uv.y += y * (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uvs.Add(uv);
                                }
                                else if (x > 0.5f)
                                {             // if normal is like vector3.right
                                    Vector2 uv;
                                    uv.x = (bounds.min.z + vertices[j].z) / bounds.max.z;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uv.x *= 1.0f - x;
                                    uv.y *= 1.0f - x;

                                    uv.x += x * (bounds.min.z + vertices[j].z) / bounds.max.z;
                                    uv.y += x * (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uvs.Add(uv);
                                }
                                else
                                {                                           // last case if it's like vector3.forward
                                    Vector2 uv;
                                    uv.x = (bounds.min.x + vertices[j].x) / bounds.max.x;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uv.x *= 1.0f - y;
                                    uv.y *= 1.0f - y;

                                    uv.x += y * (bounds.min.z + vertices[j].z) / bounds.max.z;
                                    uv.y += y * (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uvs.Add(uv);
                                }
                            }
                            else
                            {
                                if (y > 0.5f)
                                {                      // if normal is like vector3.up
                                    Vector2 uv;
                                    uv.x = (bounds.min.x + vertices[j].x) / bounds.max.x;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;
                                    uvs.Add(uv);
                                }
                                else if (x > 0.5f)
                                {             // if normal is like vector3.right
                                    Vector2 uv;
                                    uv.x = (bounds.min.z + vertices[j].z) / bounds.max.z;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;
                                    uvs.Add(uv);
                                }
                                else
                                {                                           // last case if it's like vector3.forward
                                    Vector2 uv;
                                    uv.x = (bounds.min.x + vertices[j].x) / bounds.max.x;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;
                                    uvs.Add(uv);
                                }
                            }

                            j++;
                        }

                        mesh.SetUVs(0, uvs);

                        if (_regenerateUV2)
                        {
                            Unwrapping.GenerateSecondaryUVSet(mesh);
                        }

                        mesh.UploadMeshData(false);
                    }
                    
                    if (gameObject.GetComponent<SkinnedMeshRenderer>())
                    {
                        Mesh mesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                        Vector3[] vertices = mesh.vertices;
                        Vector3[] normals = mesh.normals;
                        List<Vector2> uvs = new List<Vector2>();

                        mesh.RecalculateBounds();
                        var bounds = mesh.bounds;

                        int j = 0;
                        while (j < vertices.Length)
                        {
                            var y = Mathf.Abs(normals[j].y);
                            var x = Mathf.Abs(normals[j].x);

                            if (_smoothing)
                            {
                                if (y > 0.5f)
                                {                      // if normal is like vector3.up
                                    Vector2 uv;
                                    uv.x = (bounds.min.x + vertices[j].x) / bounds.max.x;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uv.x *= 1.0f - y;
                                    uv.y *= 1.0f - y;

                                    uv.x += y * (bounds.min.z + vertices[j].z) / bounds.max.z;
                                    uv.y += y * (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uvs.Add(uv);
                                }
                                else if (x > 0.5f)
                                {             // if normal is like vector3.right
                                    Vector2 uv;
                                    uv.x = (bounds.min.z + vertices[j].z) / bounds.max.z;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uv.x *= 1.0f - x;
                                    uv.y *= 1.0f - x;

                                    uv.x += x * (bounds.min.z + vertices[j].z) / bounds.max.z;
                                    uv.y += x * (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uvs.Add(uv);
                                }
                                else
                                {                                           // last case if it's like vector3.forward
                                    Vector2 uv;
                                    uv.x = (bounds.min.x + vertices[j].x) / bounds.max.x;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uv.x *= 1.0f - y;
                                    uv.y *= 1.0f - y;

                                    uv.x += y * (bounds.min.z + vertices[j].z) / bounds.max.z;
                                    uv.y += y * (bounds.min.y + vertices[j].y) / bounds.max.y;

                                    uvs.Add(uv);
                                }
                            }
                            else
                            {
                                if (y > 0.5f)
                                {                      // if normal is like vector3.up
                                    Vector2 uv;
                                    uv.x = (bounds.min.x + vertices[j].x) / bounds.max.x;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;
                                    uvs.Add(uv);
                                }
                                else if (x > 0.5f)
                                {             // if normal is like vector3.right
                                    Vector2 uv;
                                    uv.x = (bounds.min.z + vertices[j].z) / bounds.max.z;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;
                                    uvs.Add(uv);
                                }
                                else
                                {                                           // last case if it's like vector3.forward
                                    Vector2 uv;
                                    uv.x = (bounds.min.x + vertices[j].x) / bounds.max.x;
                                    uv.y = (bounds.min.y + vertices[j].y) / bounds.max.y;
                                    uvs.Add(uv);
                                }
                            }

                            j++;
                        }

                        mesh.SetUVs(0, uvs);

                        if (_regenerateUV2)
                        {
                            Unwrapping.GenerateSecondaryUVSet(mesh);
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
