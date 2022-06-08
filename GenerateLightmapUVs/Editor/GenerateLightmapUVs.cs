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

namespace GenerateLightmapUVs
{
    public class GenerateLightmapUVs : EditorWindow
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
