// Disable 'obsolete' warnings
#pragma warning disable 0618

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using static ProgressBarAPI.API;

namespace GenerateQuadUVs
{
    public class GenerateQuadUVs : EditorWindow
    {
        [MenuItem("Window/Unique Tools/Generate Quad UVs")]
        static void Open()
        {
            GetWindow<GenerateQuadUVs>("GenerateQuadUVs").Show();
        }
        
        private bool _flipX = false;
        private bool _flipY = false;
        private bool _invertedUVs = false;

        void OnGUI()
        {
            _flipX = EditorGUILayout.Toggle("Flip X?", _flipX);
            _flipY = EditorGUILayout.Toggle("Flip Y?", _flipY);
            _invertedUVs = EditorGUILayout.Toggle("Inverted UVs?", _invertedUVs);
            
            if (GUILayout.Button("Generate Quad UVs"))
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
                        //Vector3[] vertices = mesh.vertices;
                        List<Vector2> uvs = new List<Vector2>();
                        
                        var numVerts = mesh.vertices.Length;
                        
                        if (numVerts != 4 && numVerts != 8)
                        {
                            Debug.Log("This mesh on gameobject " + Selection.gameObjects[i].name + " does not seem to be a quad or dual sided quad, skipping.");
                            continue;
                        }
                        
                        bool is2sided = (numVerts == 8) ? true : false;

                        Vector2 uv0, uv1, uv2, uv3;
                        
                        if (_invertedUVs)
                        {
                            uv3 = new Vector2(0, _flipY ? -1 : 1);
                            uv2 = new Vector2(_flipX ? -1 : 1, _flipY ? -1 : 1);
                            uv1 = new Vector2(_flipX ? -1 : 1, 0);
                            uv0 = new Vector2(0, 0);
                        }
                        else
                        {
                            uv0 = new Vector2(0, _flipY ? -1 : 1);
                            uv1 = new Vector2(_flipX ? -1 : 1, _flipY ? -1 : 1);
                            uv2 = new Vector2(_flipX ? -1 : 1, 0);
                            uv3 = new Vector2(0, 0);
                        }
                        
                        uvs.Add(uv0);
                        uvs.Add(uv1);
                        uvs.Add(uv2);
                        uvs.Add(uv3);
                        
                        if (is2sided)
                        {// This mesh has 8 verts, assume it is double sided, and add a second set of quad uv's.
                            uvs.Add(uv0);
                            uvs.Add(uv1);
                            uvs.Add(uv2);
                            uvs.Add(uv3);
                        }

                        mesh.SetUVs(0, uvs);
                        mesh.UploadMeshData(false);
                    }
                    
                    if (gameObject.GetComponent<SkinnedMeshRenderer>())
                    {
                        Mesh mesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                        //Vector3[] vertices = mesh.vertices;
                        List<Vector2> uvs = new List<Vector2>();
                        
                        var numVerts = mesh.vertices.Length;
                        
                        if (numVerts != 4 && numVerts != 8)
                        {
                            Debug.Log("This mesh on gameobject " + Selection.gameObjects[i].name + " does not seem to be a quad or dual sided quad, skipping.");
                            continue;
                        }
                        
                        bool is2sided = (numVerts == 8) ? true : false;

                        Vector2 uv0, uv1, uv2, uv3;
                        
                        if (_invertedUVs)
                        {
                            uv3 = new Vector2(0, _flipY ? -1 : 1);
                            uv2 = new Vector2(_flipX ? -1 : 1, _flipY ? -1 : 1);
                            uv1 = new Vector2(_flipX ? -1 : 1, 0);
                            uv0 = new Vector2(0, 0);
                        }
                        else
                        {
                            uv0 = new Vector2(0, _flipY ? -1 : 1);
                            uv1 = new Vector2(_flipX ? -1 : 1, _flipY ? -1 : 1);
                            uv2 = new Vector2(_flipX ? -1 : 1, 0);
                            uv3 = new Vector2(0, 0);
                        }
                        
                        uvs.Add(uv0);
                        uvs.Add(uv1);
                        uvs.Add(uv2);
                        uvs.Add(uv3);
                        
                        if (is2sided)
                        {// This mesh has 8 verts, assume it is double sided, and add a second set of quad uv's.
                            uvs.Add(uv0);
                            uvs.Add(uv1);
                            uvs.Add(uv2);
                            uvs.Add(uv3);
                        }

                        mesh.SetUVs(0, uvs);
                        mesh.UploadMeshData(false);
                    }

                    numCompleted++;
                }

                AssetDatabase.SaveAssets();
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
