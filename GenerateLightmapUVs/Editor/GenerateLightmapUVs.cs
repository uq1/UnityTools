// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using System;
using static ProgressBarAPI.API;

namespace GenerateLightmapUVs
{
    public class GenerateLightmapUVs : EditorWindow
    {
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
