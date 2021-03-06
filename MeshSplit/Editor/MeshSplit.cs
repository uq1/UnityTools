#define UNITY_PROGRESSBAR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;

namespace MeshSplit
{
    public class MeshSplit : EditorWindow
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

        [MenuItem("Window/Unique Tools/Mesh Splitter")]
        static void Open()
        {
            GetWindow<MeshSplit>("MeshSplit").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Split Meshes"))
            {
                Process();
            }
        }

        void Process()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError("MeshSplit:: No selected object.");
                return;
            }

            ApplyTransformRecursive(Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.ExcludePrefab));
        }

        // Apply transform recursively for multiple parents, the list of transforms
        // should not include any children of another item in the list. They should
        // all be separate parents.
        public void ApplyTransformRecursive(Transform[] transforms)
        {
            int totalObjects = transforms.Length;
            int numCompleted = 0;

            ProgressBarInit("Splitting Meshes...");

            foreach (Transform transform in transforms)
            {
                ProgressBarShow("Splitting Meshes...", (float)numCompleted / (float)totalObjects);
                ApplyTransformRecursive(transform);
                numCompleted++;
            }

            ProgressBarEnd();

            AssetDatabase.SaveAssets();
        }

        // Apply transform recursively for a parent and it's children.
        public static void ApplyTransformRecursive(Transform transform)
        {
            ApplyTransform(transform);

            foreach (Transform child in transform)
            {
                ApplyTransformRecursive(child);
            }
        }

        // Apply individual transforms for multiple selected transforms.
        // It will apply the top-level transforms first, then update
        // children, but not siblings if those aren't selected.
        public static void ApplyTransform(Transform[] transforms)
        {
            bool[] applied = new bool[transforms.Length];
            for (int i = 0; i < applied.Length; ++i) applied[i] = false;

            int applyCount = 0;

            while (applyCount != transforms.Length)
            {
                // Pass over list, finding unapplied transforms with no parents or applied parents.
                for (int i = 0; i < transforms.Length; ++i)
                {
                    // If this entry is unapplied...
                    if (!applied[i])
                    {
                        bool canApply = true; // assume we can apply the transform.

                        // Is the entry a child of an unapplied parent?
                        for (int j = 0; j < transforms.Length; ++j)
                        {
                            if (i == j) continue; // ignore same entry.

                            // If it's a child of unapplied parent,
                            // we can't apply the transform to [i] in this pass.
                            if (transforms[i].IsChildOf(transforms[j]) && !applied[j])
                            {
                                canApply = false;
                                break;
                            }
                        }

                        if (canApply)
                        {
                            ApplyTransform(transforms[i]);
                            applied[i] = true;
                            Debug.Log("MeshSplit:: Applied transform to " + transforms[i].name);
                            ++applyCount;
                        }
                    }
                }
            }
        }

        // Apply an individual transform.
        public static void ApplyTransform(Transform transform)
        {
            var meshFilter = transform.GetComponent<MeshFilter>();
            var meshRenderer = transform.GetComponent<MeshRenderer>();
            if (meshFilter == null || meshRenderer == null)
            {
                return;
            }
            /*if (meshRenderer.isPartOfStaticBatch != true)
            {
                return;
            }*/
            if (meshFilter.sharedMesh == null)
            {
                return;
            }
            /*if (!meshFilter.sharedMesh.name.StartsWith("Combined Mesh (root scene)"))
            {
                return;
            }*/
            if (meshRenderer.sharedMaterials.Length < 2)
            {// Skip, it has no subs...
                return;
            }

            Debug.Log("MeshSplit:: Splitting mesh for object (" + transform.name + ").");
            string originalMeshName = meshFilter.sharedMesh.name;
            
            

            // move required submeshes lower
            var totalsubmeshcount = meshRenderer.sharedMaterials.Length;

            List<Vector2> uv1s = new List<Vector2>();
            List<Vector2> uv2s = new List<Vector2>();
            List<Vector2> uv3s = new List<Vector2>();
            List<Vector2> uv4s = new List<Vector2>();
            meshFilter.sharedMesh.GetUVs(0, uv1s);
            meshFilter.sharedMesh.GetUVs(1, uv2s);
            meshFilter.sharedMesh.GetUVs(2, uv3s);
            meshFilter.sharedMesh.GetUVs(3, uv4s);

            var materials = meshRenderer.sharedMaterials;

            for (int i = 0; i < totalsubmeshcount; i++)
            {
                var newMesh = new Mesh();//Instantiate(meshFilter.sharedMesh);
                newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                string materialName = materials[i].name;
                string new_mesh_name = "SplitMesh_" + materialName + "_" + (int)Mathf.Abs(newMesh.GetHashCode());
                string prefabPath = "Assets/Split Meshes/" + new_mesh_name + ".asset";
                newMesh.name = new_mesh_name;

                var oldVerts = new List<Vector3>();
                meshFilter.sharedMesh.GetVertices(oldVerts);
                
                var vertReplacements = new List<int>();
                for (int j = 0; j < oldVerts.Count; j++)
                {
                    vertReplacements.Add(-1);
                }

                var oldTris = meshFilter.sharedMesh.GetTriangles(i);

                var oldNormals = new List<Vector3>();
                meshFilter.sharedMesh.GetNormals(oldNormals);

                List<int> newTris = new List<int>();
                List<Vector3> newVerts = new List<Vector3>();

                List<Vector3> newNormals = new List<Vector3>();

                List<Vector2> newUv1s = new List<Vector2>();
                List<Vector2> newUv2s = new List<Vector2>();
                List<Vector2> newUv3s = new List<Vector2>();
                List<Vector2> newUv4s = new List<Vector2>();

                for (int j = 0; j < oldTris.Length; j++)
                {
                    var oldTri = oldTris[j];
                    var oldVert = oldVerts[oldTri];
                    var oldNormal = oldNormals[oldTri];

                    // First check if we already have a vert at the same position in the new mesh, re-use it if we do...
                    int found = vertReplacements[oldTri];

                    if (found >= 0)
                    {// Reuse the old vert...
                        newTris.Add(found);
                    }
                    else
                    {// Vert position not already in the list, add a new one...
                        newVerts.Add(oldVert);
                        newNormals.Add(oldNormal);
                        newTris.Add(newVerts.Count - 1);

                        if (uv1s.Count > 0 && uv1s.Count >= oldTri)
                        {
                            newUv1s.Add(uv1s[oldTri]);
                        }

                        if (uv2s.Count > 0 && uv2s.Count >= oldTri)
                        {
                            newUv2s.Add(uv2s[oldTri]);
                        }

                        if (uv3s.Count > 0 && uv3s.Count >= oldTri)
                        {
                            newUv3s.Add(uv3s[oldTri]);
                        }

                        if (uv4s.Count > 0 && uv4s.Count >= oldTri)
                        {
                            newUv4s.Add(uv4s[oldTri]);
                        }
                        
                        vertReplacements[oldTri] = newVerts.Count - 1;
                    }
                }

                newMesh.SetVertices(newVerts);
                newMesh.SetNormals(newNormals);
                newMesh.SetTriangles(newTris, 0);
                newMesh.SetUVs(0, newUv1s);
                newMesh.SetUVs(1, newUv2s);
                newMesh.SetUVs(2, newUv3s);
                newMesh.SetUVs(3, newUv4s);


                if (!AssetDatabase.IsValidFolder("Assets/Split Meshes"))
                    AssetDatabase.CreateFolder("Assets", "Split Meshes");

                var newGameObject = new GameObject();
                var newMeshFilter = newGameObject.AddComponent<MeshFilter>();
                var newMeshRenderer = newGameObject.AddComponent<MeshRenderer>();

                newGameObject.name = new_mesh_name;
                newGameObject.transform.parent = transform;

                Vector3 zero;
                zero.x = 0.0f;
                zero.y = 0.0f;
                zero.z = 0.0f;

                Vector3 scale;
                scale.x = 1.0f;
                scale.y = 1.0f;
                scale.z = 1.0f;
                newGameObject.transform.localPosition = zero;
                newGameObject.transform.localRotation = Quaternion.identity;
                newGameObject.transform.localScale = scale;

                Material[] newMats = new Material[1];
                newMats.SetValue(materials[i], 0);
                newMeshRenderer.sharedMaterials = newMats;
                newMeshFilter.sharedMesh = newMesh;

                AssetDatabase.CreateAsset(newMesh, prefabPath);
            }

            //AssetDatabase.SaveAssets();


            //meshRenderer.enabled = false;
            DestroyImmediate(meshRenderer, false);
            DestroyImmediate(meshFilter, false);
        }

        public static void ApplyInverseTransform(Transform transform, Mesh mesh)
        {
            var verts = mesh.vertices;
            var norms = mesh.normals;
            var tans = mesh.tangents;
            var bounds = mesh.bounds;

            // Handle vertices.
            for (int i = 0; i < verts.Length; ++i)
            {
                var nvert = verts[i];

                nvert = transform.InverseTransformPoint(nvert);

                verts[i] = nvert;
            }

            // Handle normals.
            for (int i = 0; i < norms.Length; ++i)
            {
                var nnorm = norms[i];

                nnorm = transform.InverseTransformDirection(nnorm);

                norms[i] = nnorm;
            }

            // Handle tangents.
            for (int i = 0; i < tans.Length; ++i)
            {
                var ntan = tans[i];

                var transformed = transform.InverseTransformDirection(ntan.x, ntan.y, ntan.z);

                ntan = new Vector4(transformed.x, transformed.y, transformed.z, ntan.w);

                tans[i] = ntan;
            }

            bounds.center = transform.InverseTransformPoint(bounds.center);
            bounds.extents = transform.InverseTransformPoint(bounds.extents);

            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.tangents = tans;
            mesh.bounds = bounds;
        }

        public static void ApplyTransform(Transform transform, Mesh mesh)
        {
            var verts = mesh.vertices;
            var norms = mesh.normals;
            var tans = mesh.tangents;
            var bounds = mesh.bounds;

            // Handle vertices.
            for (int i = 0; i < verts.Length; ++i)
            {
                var nvert = verts[i];

                nvert = transform.TransformPoint(nvert);

                verts[i] = nvert;
            }

            // Handle normals.
            for (int i = 0; i < norms.Length; ++i)
            {
                var nnorm = norms[i];

                nnorm = transform.TransformDirection(nnorm);

                norms[i] = nnorm;
            }

            // Handle tangents.
            for (int i = 0; i < tans.Length; ++i)
            {
                var ntan = tans[i];

                var transformed = transform.TransformDirection(ntan.x, ntan.y, ntan.z);

                ntan = new Vector4(transformed.x, transformed.y, transformed.z, ntan.w);

                tans[i] = ntan;
            }

            bounds.center = transform.TransformPoint(bounds.center);
            bounds.extents = transform.TransformPoint(bounds.extents);

            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.tangents = tans;
            mesh.bounds = bounds;
        }

    }
}
