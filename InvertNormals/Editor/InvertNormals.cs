// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using System;
using static ProgressBarAPI.API;

namespace InvertNormals
{
    public class InvertNormals : EditorWindow
    {
        [MenuItem("Window/Unique Tools/Invert Normals")]
        static void Open()
        {
            GetWindow<InvertNormals>("InvertNormals").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Invert Normals"))
            {
                GenerateMeshNormals();
            }
        }

        void GenerateMeshNormals()
        {
            try
            {
                ProgressBarInit("Inverting Normals...");

                int totalObjects = Selection.gameObjects.Length;
                int numCompleted = 0;

                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    var gameObject = Selection.gameObjects[i];

                    ProgressBarShow("Inverting Normals in \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                    if (gameObject.GetComponent<MeshFilter>())
                    {
                        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                        var norms = mesh.normals;

                        Vector3[] newNormals = new Vector3[norms.Length];

                        for (int j = 0; j < norms.Length; j++)
                        {
                            Vector3 normal = norms[j];
                            normal.x *= -1.0f;
                            normal.y *= -1.0f;
                            normal.z *= -1.0f;
                            newNormals[j] = normal;
                        }

                        mesh.normals = newNormals;
                        mesh.RecalculateTangents();
                        mesh.UploadMeshData(false);
                    }
                    
                    if (gameObject.GetComponent<SkinnedMeshRenderer>())
                    {
                        Mesh mesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                        var norms = mesh.normals;

                        Vector3[] newNormals = new Vector3[norms.Length];

                        for (int j = 0; j < norms.Length; j++)
                        {
                            Vector3 normal = norms[j];
                            normal.x *= -1.0f;
                            normal.y *= -1.0f;
                            normal.z *= -1.0f;
                            newNormals[j] = normal;
                        }

                        mesh.normals = newNormals;
                        mesh.RecalculateTangents();
                        mesh.UploadMeshData(false);
                    }

                    numCompleted++;
                }

                ProgressBarEnd();

                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception thrown when executing: " + ex.Message);
            }

            ProgressBarEnd();
        }
    }
}
