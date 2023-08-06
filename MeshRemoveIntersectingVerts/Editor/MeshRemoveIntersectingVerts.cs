using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MeshRemoveIntersectingVerts
{
    public class MeshRemoveIntersectingVerts : EditorWindow
    {
        GameObject bounds = null;

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

        [MenuItem("Window/Unique Tools/Mesh Remove Intersecting Verts")]
        static void Open()
        {
            GetWindow<MeshRemoveIntersectingVerts>("MeshRemoveIntersectingVerts").Show();
        }

        void OnGUI()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            bounds = (GameObject)EditorGUILayout.ObjectField("Bounds GameObject", bounds, typeof(GameObject));
#pragma warning restore CS0618 // Type or member is obsolete

            if (GUILayout.Button("Cull Meshes"))
            {
                ProcessMeshes();
            }
        }

        void ProcessMeshes()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError("MeshRemoveIntersectingVerts:: No selected object.");
                return;
            }

            MeshFilter mf = bounds.GetComponent<MeshFilter>();

            if (mf == null)
            {
                Debug.LogError("MeshRemoveIntersectingVerts:: No meshfilter in cube bounds gameobject.");
                return;
            }

            Mesh mesh = mf.sharedMesh;

            if (mesh == null)
            {
                mesh = mf.mesh;

                if (mesh == null)
                {
                    Debug.LogError("MeshRemoveIntersectingVerts:: No mesh found in cube bounds gameobject.");
                    return;
                }
            }

            // Make sure we have some bounds to use...
            mf.sharedMesh.RecalculateBounds();

            ProgressBarInit("Culling Meshes By Cube Bounds...");

            int totalObjects = Selection.gameObjects.Length;
            int numCompleted = 0;

            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                var gameObject = Selection.gameObjects[i];

                ProgressBarShow("Culling Mesh \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                if (gameObject.GetComponent<MeshFilter>() && gameObject.GetComponent<MeshRenderer>())
                {
                    GenerateNewMesh(gameObject);
                }

                numCompleted++;
            }

            ProgressBarEnd();
        }

        bool CheckLineBox(Vector3 B1, Vector3 B2, Vector3 L1, Vector3 L2, ref Vector3 Hit)
        {
            if (L2.x < B1.x && L1.x < B1.x) return false;
            if (L2.x > B2.x && L1.x > B2.x) return false;
            if (L2.y < B1.y && L1.y < B1.y) return false;
            if (L2.y > B2.y && L1.y > B2.y) return false;
            if (L2.z < B1.z && L1.z < B1.z) return false;
            if (L2.z > B2.z && L1.z > B2.z) return false;
            if (L1.x > B1.x && L1.x < B2.x &&
                L1.y > B1.y && L1.y < B2.y &&
                L1.z > B1.z && L1.z < B2.z)
            {
                Hit = L1;
                return true;
            }
            if ((GetIntersection(L1.x - B1.x, L2.x - B1.x, L1, L2, ref Hit) && InBox(Hit, B1, B2, 1))
              || (GetIntersection(L1.y - B1.y, L2.y - B1.y, L1, L2, ref Hit) && InBox(Hit, B1, B2, 2))
              || (GetIntersection(L1.z - B1.z, L2.z - B1.z, L1, L2, ref Hit) && InBox(Hit, B1, B2, 3))
              || (GetIntersection(L1.x - B2.x, L2.x - B2.x, L1, L2, ref Hit) && InBox(Hit, B1, B2, 1))
              || (GetIntersection(L1.y - B2.y, L2.y - B2.y, L1, L2, ref Hit) && InBox(Hit, B1, B2, 2))
              || (GetIntersection(L1.z - B2.z, L2.z - B2.z, L1, L2, ref Hit) && InBox(Hit, B1, B2, 3)))
                return true;

            return false;
        }

        bool GetIntersection(float fDst1, float fDst2, Vector3 P1, Vector3 P2, ref Vector3 Hit)
        {
            if ((fDst1 * fDst2) >= 0.0f) return false;
            if (fDst1 == fDst2) return false;
            Hit = P1 + (P2 - P1) * (-fDst1 / (fDst2 - fDst1));
            return true;
        }

        bool InBox(Vector3 Hit, Vector3 B1, Vector3 B2, int Axis)
        {
            if (Axis == 1 && Hit.z > B1.z && Hit.z < B2.z && Hit.y > B1.y && Hit.y < B2.y) return true;
            if (Axis == 2 && Hit.z > B1.z && Hit.z < B2.z && Hit.x > B1.x && Hit.x < B2.x) return true;
            if (Axis == 3 && Hit.x > B1.x && Hit.x < B2.x && Hit.y > B1.y && Hit.y < B2.y) return true;
            return false;
        }

        // Apply an individual transform.
        public void GenerateNewMesh(GameObject gameObject)
        {
            MeshFilter mf = bounds.GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;
            Bounds boundsCube = mesh.bounds;

            var meshFilter = gameObject.GetComponent<MeshFilter>();
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshFilter == null || meshRenderer == null)
            {
                return;
            }
            if (meshFilter.sharedMesh == null)
            {
                return;
            }

            Debug.Log("MeshRemoveIntersectingVerts:: Creating new mesh for object (" + gameObject.name + ").");

            var originalMeshName = meshFilter.sharedMesh.name;

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

            var newMesh = new Mesh();
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            if (!AssetDatabase.IsValidFolder("Assets/Culled Meshes"))
            {
                AssetDatabase.CreateFolder("Assets", "Culled Meshes");
            }

            var prefabPath = "";

            var new_mesh_name = string.Format("CulledMesh_{0}_{1}_{2}",
                gameObject.name, originalMeshName, (int)Mathf.Abs(newMesh.GetHashCode()));

            if (new_mesh_name.StartsWith("CulledMesh"))
            {
                Debug.Log("MeshRemoveIntersectingVerts:: Replacing existing culled mesh (" + new_mesh_name + ").");
                prefabPath = "Assets/Culled Meshes/" + new_mesh_name + ".asset";
            }
            else
            {
                newMesh.name = new_mesh_name;
                prefabPath = "Assets/Culled Meshes/" + new_mesh_name + ".asset";
            }

            List<int> newTris = new List<int>();
            List<Vector3> newVerts = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUV1s = new List<Vector2>();
            List<Vector2> newUV2s = new List<Vector2>();

            int current_vert = 0;

            for (int i = 0; i < totalsubmeshcount; i++)
            {
                var materialName = materials[i].name;

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

                bool haveUV1 = false;
                bool haveUV2 = false;

                var oldUV1s = new List<Vector2>();
                meshFilter.sharedMesh.GetUVs(0, oldUV1s);

                if (oldUV1s.Count > 0) haveUV1 = true;

                var oldUV2s = new List<Vector2>();
                meshFilter.sharedMesh.GetUVs(1, oldUV2s);

                if (oldUV2s.Count > 0) haveUV2 = true;

                for (int j = 0; j < oldTris.Length; j += 3)
                {
                    var oldTri1 = oldTris[j];
                    var oldVert1 = oldVerts[oldTri1];
                    var oldNormal1 = oldNormals[oldTri1];
                    Vector2 oldUV11 = new Vector2();
                    Vector2 oldUV21 = new Vector2();

                    var oldTri2 = oldTris[j + 1];
                    var oldVert2 = oldVerts[oldTri2];
                    var oldNormal2 = oldNormals[oldTri2];
                    Vector2 oldUV12 = new Vector2();
                    Vector2 oldUV22 = new Vector2();

                    var oldTri3 = oldTris[j + 2];
                    var oldVert3 = oldVerts[oldTri3];
                    var oldNormal3 = oldNormals[oldTri3];
                    Vector2 oldUV13 = new Vector2();
                    Vector2 oldUV23 = new Vector2();

                    /*
                    Ray ray1 = new Ray();
                    ray1.origin = oldVert1;
                    ray1.direction = oldVert1 - oldVert2;
                    //ray1.direction.Normalize();

                    Ray ray2 = new Ray();
                    ray2.origin = oldVert2;
                    ray2.direction = oldVert2 - oldVert3;
                    //ray2.direction.Normalize();

                    Ray ray3 = new Ray();
                    ray3.origin = oldVert3;
                    ray3.direction = oldVert3 - oldVert1;
                    //ray3.direction.Normalize();

                    float distance = 0.0f;

                    if (boundsCube.IntersectRay(ray1, out distance))
                    {
                        Debug.Log("MeshRemoveIntersectingVerts:: ray1 (tri " + oldTri1 + " -> " + oldTri2 + ") intersects bounds.");

                        //if (boundsCube.Contains(ray1.GetPoint(distance)))
                        {
                            //Debug.Log("MeshRemoveIntersectingVerts:: ray1 hit inside bounds.");
                            continue;
                        }
                    }

                    if (boundsCube.IntersectRay(ray2, out distance))
                    {
                        Debug.Log("MeshRemoveIntersectingVerts:: ray2 (tri " + oldTri2 + " -> " + oldTri3 + ") intersects bounds.");

                        //if (boundsCube.Contains(ray2.GetPoint(distance)))
                        {
                            //Debug.Log("MeshRemoveIntersectingVerts:: ray2 hit inside bounds.");
                            continue;
                        }
                    }

                    if (boundsCube.IntersectRay(ray3, out distance))
                    {
                        Debug.Log("MeshRemoveIntersectingVerts:: ray3 (tri " + oldTri3 + " -> " + oldTri1 + ") intersects bounds.");

                        //if (boundsCube.Contains(ray3.GetPoint(distance)))
                        {
                            //Debug.Log("MeshRemoveIntersectingVerts:: ray3 hit inside bounds.");
                            continue;
                        }
                    }
                    */

                    Vector3 hit = new Vector3();

                    if (CheckLineBox(boundsCube.min + bounds.transform.position, boundsCube.max + bounds.transform.position, oldVert1, oldVert2, ref hit))
                    {
                        Debug.Log("MeshRemoveIntersectingVerts:: ray1 (tri " + oldTri1 + " -> " + oldTri2 + ") intersects bounds.");
                        continue;
                    }

                    if (CheckLineBox(boundsCube.min + bounds.transform.position, boundsCube.max + bounds.transform.position, oldVert2, oldVert3, ref hit))
                    {
                        Debug.Log("MeshRemoveIntersectingVerts:: ray2 (tri " + oldTri2 + " -> " + oldTri3 + ") intersects bounds.");
                        continue;
                    }

                    if (CheckLineBox(boundsCube.min + bounds.transform.position, boundsCube.max + bounds.transform.position, oldVert3, oldVert1, ref hit))
                    {
                        Debug.Log("MeshRemoveIntersectingVerts:: ray3 (tri " + oldTri3 + " -> " + oldTri1 + ") intersects bounds.");
                        continue;
                    }

                    // First check if we already have a vert at the same position in the new mesh, re-use it if we do...
                    int found1 = vertReplacements[oldTri1];
                    int found2 = vertReplacements[oldTri2];
                    int found3 = vertReplacements[oldTri3];

                    if (haveUV1)
                    {
                        oldUV11 = oldUV1s[oldTri1];
                        oldUV12 = oldUV1s[oldTri2];
                        oldUV13 = oldUV1s[oldTri3];
                    }

                    if (haveUV2)
                    {
                        oldUV21 = oldUV2s[oldTri1];
                        oldUV22 = oldUV2s[oldTri2];
                        oldUV23 = oldUV2s[oldTri3];
                    }

                    if (found1 >= 0)
                    {// Reuse the old vert...
                        newTris.Add(found1);
                    }
                    else
                    {
                        newVerts.Add(oldVert1);
                        newNormals.Add(oldNormal1);
                        newTris.Add(current_vert);
                        if (haveUV1) newUV1s.Add(oldUV11);
                        if (haveUV2) newUV2s.Add(oldUV21);
                        current_vert++;

                        vertReplacements[oldTri1] = newVerts.Count - 1;
                    }

                    if (found2 >= 0)
                    {// Reuse the old vert...
                        newTris.Add(found2);
                    }
                    else
                    {
                        newVerts.Add(oldVert2);
                        newNormals.Add(oldNormal2);
                        newTris.Add(current_vert);
                        if (haveUV1) newUV1s.Add(oldUV12);
                        if (haveUV2) newUV2s.Add(oldUV22);
                        current_vert++;

                        vertReplacements[oldTri2] = newVerts.Count - 1;
                    }

                    if (found3 >= 0)
                    {// Reuse the old vert...
                        newTris.Add(found3);
                    }
                    else
                    {
                        newVerts.Add(oldVert3);
                        newNormals.Add(oldNormal3);
                        newTris.Add(current_vert);
                        if (haveUV1) newUV1s.Add(oldUV13);
                        if (haveUV2) newUV2s.Add(oldUV23);
                        current_vert++;

                        vertReplacements[oldTri3] = newVerts.Count - 1;
                    }
                }
            }

            newMesh.SetVertices(newVerts);
            newMesh.SetTriangles(newTris, 0);
            newMesh.SetNormals(newNormals);
            newMesh.SetUVs(0, newUV1s);
            newMesh.SetUVs(1, newUV2s);

            AssetDatabase.CreateAsset(newMesh, prefabPath);

            AssetDatabase.SaveAssets();

            meshFilter.sharedMesh = newMesh;

        }

    }
}
