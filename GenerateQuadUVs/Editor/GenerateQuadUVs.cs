// Disable 'obsolete' warnings
#pragma warning disable 0618

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using static ProgressBarAPI.API;
using System.Linq;
using System.Collections;

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
        private bool _swapXY = false;
        private int _vertOrder = 0;

        List<Vector2> uvs = new List<Vector2> { new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0) };
        List<List<Vector2>> uv_combinations = new List<List<Vector2>>();
        List<string> uv_combination_names = new List<string>();

        void OnGUI()
        {
            _flipX = EditorGUILayout.Toggle("Flip X?", _flipX);
            _flipY = EditorGUILayout.Toggle("Flip Y?", _flipY);
            _swapXY = EditorGUILayout.Toggle("Swap X and Y coordinates?", _swapXY);

            uv_combinations.Clear();
            Permute<Vector2>(uvs.ToArray(), uv_combinations);

            uv_combination_names.Clear();
            for (int i = 0; i < uv_combinations.Count; i++)
            {
                uv_combination_names.Add("Order " + i.ToString());
            }

            _vertOrder = EditorGUILayout.Popup("Vertex Order", _vertOrder, uv_combination_names.ToArray());

            if (GUILayout.Button("Generate Quad UVs"))
            {
                GenerateMeshUVs();
            }
        }

        Vector2 SwapXY(Vector2 uv_in)
        {
            Vector2 uv = new Vector2();
            uv.x = uv_in.y;
            uv.y = uv_in.x;
            return uv;
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

                        List<Vector2> useUVs = uv_combinations[_vertOrder];
                        uv0 = new Vector2(useUVs[0].x, useUVs[0].y);
                        uv1 = new Vector2(useUVs[1].x, useUVs[1].y);
                        uv2 = new Vector2(useUVs[2].x, useUVs[2].y);
                        uv3 = new Vector2(useUVs[3].x, useUVs[3].y);

                        if (_flipX)
                        {
                            if (uv0.x == 1.0f)
                                uv0.x = -1.0f;

                            if (uv1.x == 1.0f)
                                uv1.x = -1.0f;

                            if (uv2.x == 1.0f)
                                uv2.x = -1.0f;

                            if (uv3.x == 1.0f)
                                uv3.x = -1.0f;
                        }

                        if (_flipY)
                        {
                            if (uv0.y == 1.0f)
                                uv0.y = -1.0f;

                            if (uv1.y == 1.0f)
                                uv1.y = -1.0f;

                            if (uv2.y == 1.0f)
                                uv2.y = -1.0f;

                            if (uv3.y == 1.0f)
                                uv3.y = -1.0f;
                        }

                        if (_swapXY)
                        {
                            uv0 = SwapXY(uv0);
                            uv1 = SwapXY(uv1);
                            uv2 = SwapXY(uv2);
                            uv3 = SwapXY(uv3);
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

                        List<Vector2> useUVs = uv_combinations[_vertOrder];
                        uv0 = new Vector2(useUVs[0].x, useUVs[0].y);
                        uv1 = new Vector2(useUVs[1].x, useUVs[1].y);
                        uv2 = new Vector2(useUVs[2].x, useUVs[2].y);
                        uv3 = new Vector2(useUVs[3].x, useUVs[3].y);

                        if (_flipX)
                        {
                            if (uv0.x == 1.0f)
                                uv0.x = -1.0f;

                            if (uv1.x == 1.0f)
                                uv1.x = -1.0f;

                            if (uv2.x == 1.0f)
                                uv2.x = -1.0f;

                            if (uv3.x == 1.0f)
                                uv3.x = -1.0f;
                        }

                        if (_flipY)
                        {
                            if (uv0.y == 1.0f)
                                uv0.y = -1.0f;

                            if (uv1.y == 1.0f)
                                uv1.y = -1.0f;

                            if (uv2.y == 1.0f)
                                uv2.y = -1.0f;

                            if (uv3.y == 1.0f)
                                uv3.y = -1.0f;
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

        /*
        public static List<List<Vector2>> GetCombination(List<Vector2> lst, int index, int count)
        {
            List<List<Vector2>> combinations = new List<List<Vector2>>();
            List<Vector2> comb;
            if (count == 0 || index == lst.Count)
            {
                return null;
            }
            for (int i = index; i < lst.Count; i++)
            {
                comb = new List<Vector2>();
                comb.Add(lst.ElementAt(i));
                combinations.Add(comb);
                var rest = GetCombination(lst, i + 1, count - 1);
                if (rest != null)
                {
                    foreach (var item in rest)
                    {
                        combinations.Add(comb.Union(item).ToList());
                    }
                }
            }
            return combinations;
        }*/

        public static void Permute<T>(T[] items, List<List<T>> output)
        {
            Permute(items, 0, new T[items.Length], new bool[items.Length], output);
        }

        private static void Permute<T>(T[] items, int item, T[] permutation, bool[] used, List<List<T>> output)
        {
            for (int i = 0; i < items.Length; ++i)
            {
                if (!used[i])
                {
                    used[i] = true;
                    permutation[item] = items[i];

                    if (item < (items.Length - 1))
                    {
                        Permute(items, item + 1, permutation, used, output);
                    }
                    else
                    {
                        output.Add(permutation.ToList());
                    }

                    used[i] = false;
                }
            }
        }
    }
}

