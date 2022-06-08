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
using System.Reflection;

using System.Threading;
using System.Threading.Tasks;
using System;

namespace MeshCombiner
{
    public class MeshCombiner : EditorWindow
    {
        private DefaultAsset _exportDirectory = null;
        private GameObject _combineTarget = null;
        private bool _exportMesh;

        /*
        [DllImport("simpleProgressBar", CallingConvention = CallingConvention.Cdecl)]
        public static extern int simpleProgressBarShow(string header, string msg, float percent, float step);

        [DllImport("simpleProgressBar", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool simpleProgressBarCancelled();

        [DllImport("simpleProgressBar", CallingConvention = CallingConvention.Cdecl)]
        public static extern void simpleProgressBarEnd();

        public static string progressBarText;
        public static float progressBarPercent = 0;
        float progressBarStep = 0;
        public static bool progressBarEnabled = false;
        static bool userCanceled = false;
        int progressSteps, progressStepsDone;
        IEnumerator progressFunc;
        public static bool bakeInProgress = false;
        void ProgressBarInit(string startText)
        {
            ProgressBarSetStep(0);
            progressBarText = startText;
            progressBarEnabled = true;
            simpleProgressBarShow("Mesh Combine", progressBarText, progressBarPercent, progressBarStep);
        }
        void ProgressBarSetStep(float step)
        {
            progressBarStep = step;
        }
        void ProgressBarShow(string text, float percent)
        {
            progressBarText = text;
            progressBarPercent = percent;
            simpleProgressBarShow("Mesh Combine", progressBarText, progressBarPercent, progressBarStep);
            userCanceled = simpleProgressBarCancelled();
        }
        public static void ProgressBarEnd(bool freeAreas = true)
        {
            progressBarEnabled = false;
            simpleProgressBarEnd();
        }
        */

        
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
        

        /*
        System.Action ProgressUpdate;
        bool processing = false;
        float progress = 0;

        void ProgressBarInit(string startText)
        {
            progress = 0;
            processing = true;
            ProgressBarShow(startText, 0);
        }
        void ProgressBarShow(string text, float percent)
        {
            progress = percent;
            ProgressUpdate();

            if (percent == 1.0f)
            {
                ProgressBarEnd();
            }
        }
        public void ProgressBarEnd(bool freeAreas = true)
        {
            processing = false;
            progress = 1;
            ProgressUpdate = null;
        }
        */

        [MenuItem("Window/Unique Tools/Mesh Combiner")]
        static void Open()
        {
            GetWindow<MeshCombiner>("Mesh Combiner").Show();
        }

        void OnGUI()
        {
            _combineTarget = (GameObject)EditorGUILayout.ObjectField("CombineTarget", _combineTarget, typeof(GameObject), true);
            _exportMesh = EditorGUILayout.Toggle("Export Mesh", _exportMesh);
            _exportDirectory = (DefaultAsset) EditorGUILayout.ObjectField("Export Directory", _exportDirectory, typeof(DefaultAsset), true);

            if (GUILayout.Button("Combine Meshes"))
            {
                if (_combineTarget == null)
                {
                    return;
                }
                CombineMesh();
            }

            /*
            if (processing)
            {
                ProgressBarShow("unused", progress);
                Repaint();
            }

            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 30), progress, "progress");
            */
        }

        void CombineMesh()
        {
            try
            {
                ProgressBarInit("Calculating Meshes...");

                var meshFilters = _combineTarget.GetComponentsInChildren<MeshFilter>();
                var combineMeshInstanceDictionary = new Dictionary<Material, List<CombineInstance>>();

                /* get count first */
                int totalCount = 0;

                foreach (var meshFilter in meshFilters)
                {
                    var mesh = meshFilter.sharedMesh;
                    var vertices = new List<Vector3>();
                    var materials = meshFilter.GetComponent<Renderer>().sharedMaterials;
                    var subMeshCount = meshFilter.sharedMesh.subMeshCount;
                    mesh.GetVertices(vertices);

                    for (var i = 0; i < subMeshCount; i++)
                    {
                        
                        totalCount++;
                    }
                }

                int numTotal = totalCount;// meshFilters.Length;
                int numCompleted = 0;

                foreach (var meshFilter in meshFilters)
                {
                    //ProgressBarShow("Object \"" + _combineTarget.name + "\" - " + "MeshFilter \"" + meshFilter.name + "\"", numCompleted / (float)numTotal);
                    //numCompleted++;

                    var mesh = meshFilter.sharedMesh;
                    var vertices = new List<Vector3>();
                    var materials = meshFilter.GetComponent<Renderer>().sharedMaterials;
                    var subMeshCount = meshFilter.sharedMesh.subMeshCount;
                    mesh.GetVertices(vertices);

                    for (var i = 0; i < subMeshCount; i++)
                    {
                        //Debug.Log("Object \"" + mesh.name + "\" has " + subMeshCount + " sub meshes.");

                        ProgressBarShow("Object \"" + mesh.name + "\" - " + "MeshFilter \"" + meshFilter.name + "\"", numCompleted / (float)totalCount);

                        var material = materials[i];
                        var triangles = new List<int>();
                        mesh.GetTriangles(triangles, i);

                        var newMesh = new Mesh
                        {
                            vertices = vertices.ToArray(),
                            triangles = triangles.ToArray(),
                            uv = mesh.uv,
                            normals = mesh.normals
                        };

                        if (!combineMeshInstanceDictionary.ContainsKey(material))
                        {
                            combineMeshInstanceDictionary.Add(material, new List<CombineInstance>());
                        }

                        var combineInstance = new CombineInstance
                        { transform = meshFilter.transform.localToWorldMatrix, mesh = newMesh };
                        combineMeshInstanceDictionary[material].Add(combineInstance);

                        numCompleted++;
                    }
                }

                ProgressBarEnd();

                ProgressBarInit("Combining Meshes...");

                numTotal = combineMeshInstanceDictionary.Count;
                numCompleted = 0;


                _combineTarget.SetActive(false);

                foreach (var kvp in combineMeshInstanceDictionary)
                {
                    ProgressBarShow("Combining...", numCompleted / (float)numTotal);

                    var newObject = new GameObject(kvp.Key.name);

                    var meshRenderer = newObject.AddComponent<MeshRenderer>();
                    var meshFilter = newObject.AddComponent<MeshFilter>();

                    meshRenderer.material = kvp.Key;
                    var mesh = new Mesh();
                    mesh.CombineMeshes(kvp.Value.ToArray());
                    Unwrapping.GenerateSecondaryUVSet(mesh);

                    meshFilter.sharedMesh = mesh;
                    newObject.transform.parent = _combineTarget.transform.parent;

                    if (!_exportMesh || _exportDirectory == null)
                    {
                        numCompleted++;
                    }
                    else
                    {
                        ExportMesh(mesh, kvp.Key.name);
                        numCompleted++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception thrown when executing: " + ex.Message);
            }

            ProgressBarEnd();
        }

        void ExportMesh(Mesh mesh, string fileName)
        {
            var exportDirectoryPath = AssetDatabase.GetAssetPath(_exportDirectory);
            if (Path.GetExtension(fileName) != ".asset")
            {
                fileName += ".asset";
            }
            var exportPath = Path.Combine(exportDirectoryPath, fileName);
            AssetDatabase.CreateAsset(mesh, exportPath);
        }
    }
}
