using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MeshUnbaker
{
    public class MeshUnbaker : EditorWindow
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
        public void ProgressBarEnd(bool freeAreas = true)
        {
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Window/Unique Tools/Mesh Unbaker")]
        static void Open()
        {
            GetWindow<MeshUnbaker>("MeshUnbaker").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Unbake Meshes"))
            {
                Process();
            }
        }

        void Process()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError("MeshUnbaker:: No selected object.");
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

            ProgressBarInit("Unbaking Meshes...");

            foreach (Transform transform in transforms)
            {
                ProgressBarShow("Unbaking Meshes...", (float)numCompleted / (float)totalObjects);
                ApplyTransformRecursive(transform);
                numCompleted++;
            }

            ProgressBarEnd();
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
                            Debug.Log("MeshUnbaker:: Applied transform to " + transforms[i].name);
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
            if (meshRenderer.isPartOfStaticBatch != true)
            {
                return;
            }
            if (meshFilter.sharedMesh == null)
            {
                return;
            }
            if (!meshFilter.sharedMesh.name.StartsWith("Combined Mesh (root scene)"))
            {
                return;
            }
            Debug.Log("MeshUnbaker:: Unbaking mesh for object (" + transform.name + ").");
            var originalMeshName = meshFilter.sharedMesh.name;

            var newMesh = Instantiate(meshFilter.sharedMesh);

            // move required submeshes lower
            var totalsubmeshcount = newMesh.subMeshCount > meshRenderer.sharedMaterials.Length ? meshRenderer.sharedMaterials.Length : newMesh.subMeshCount;

            List<Vector2> uv1s = new List<Vector2>();
            List<Vector2> uv2s = new List<Vector2>();
            List<Vector2> uv3s = new List<Vector2>();
            List<Vector2> uv4s = new List<Vector2>();
            meshFilter.sharedMesh.GetUVs(0, uv1s);
            meshFilter.sharedMesh.GetUVs(1, uv2s);
            meshFilter.sharedMesh.GetUVs(2, uv3s);
            meshFilter.sharedMesh.GetUVs(3, uv4s);

            for (int i = 0; i < totalsubmeshcount; i += 1)
            {
                newMesh.SetTriangles(newMesh.GetTriangles((int)(meshRenderer.subMeshStartIndex + i)), i, false, (int)newMesh.GetBaseVertex(meshRenderer.subMeshStartIndex + i));
            }

            newMesh.SetUVs(0, uv1s);
            newMesh.SetUVs(1, uv2s);
            newMesh.SetUVs(2, uv3s);
            newMesh.SetUVs(3, uv4s);

            newMesh.subMeshCount = totalsubmeshcount;

            // baked transformation -> world space
            ApplyInverseTransform(transform, newMesh);
            // world space -> local space
            //ApplyTransform(meshRenderer.staticBatchRootTransform, newMesh);

            if (!AssetDatabase.IsValidFolder("Assets/Unbaked Meshes"))
                AssetDatabase.CreateFolder("Assets", "Unbaked Meshes");

            var prefabPath = "";

            var new_mesh_name = string.Format("UnbakedMesh_{0}_{1}_{2}",
                transform.name, originalMeshName, (int)Mathf.Abs(newMesh.GetHashCode()));
            if (originalMeshName.StartsWith("UnbakedMesh"))
            {
                Debug.Log("MeshUnbaker:: Replacing existing unbaked mesh (" + originalMeshName + ").");
                prefabPath = "Assets/Unbaked Meshes/" + originalMeshName + ".asset";
            }
            else
            {
                newMesh.name = new_mesh_name;
                prefabPath = "Assets/Unbaked Meshes/" + new_mesh_name + ".asset";
            }

            meshFilter.sharedMesh = newMesh;

            AssetDatabase.CreateAsset(newMesh, prefabPath);
            AssetDatabase.SaveAssets();
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
