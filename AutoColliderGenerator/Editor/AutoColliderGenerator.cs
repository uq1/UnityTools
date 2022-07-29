// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using static ProgressBarAPI.API;

namespace AutoColliderGenerator
{
    [ExecuteInEditMode]
    public class AutoColliderGenerator : EditorWindow
    {
        //private fields
        private MeshCollider MeshCollider;
        public bool removeOld = false;

        [MenuItem("Window/Unique Tools/Auto Collider Generator")]
        static void Open()
        {
            GetWindow<AutoColliderGenerator>("AutoColliderGenerator").Show();
        }

        void OnGUI()
        {
            removeOld = EditorGUILayout.Toggle("Remove Old Colliders", removeOld);
            
            if (GUILayout.Button("Generate Colliders"))
            {
                GenerateColliders();
            }
        }

        public void GenerateColliders()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Generating Colliders...");
            
            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Generating Colliders for \"" + obj.name + "\".", (float)numCompleted / (float)totalObjects);
                
                if (obj.GetComponent<MeshRenderer>() || obj.GetComponent<SkinnedMeshRenderer>())
                {
                    MakeSureColliderExists(obj);
                }
                
                numCompleted++;
            }

            ProgressBarEnd();
        }

        private bool MakeSureColliderExists(GameObject mesh) 
        {
            if (mesh == null) return false;

            if (removeOld)
            {
                bool deleted = true;

                while (deleted)
                {
                    MeshCollider = mesh.GetComponent<MeshCollider>();

                    if (MeshCollider != null)
                    {
                        MeshCollider.DestroyImmediate(MeshCollider);
                        Debug.Log("MakeSureColliderExists: Mesh " + mesh.name + " removing old mesh collider.");
                    }
                    else
                    {
                        deleted = false;
                    }
                }
            }
            else
            {
                MeshCollider = mesh.GetComponent<MeshCollider>();

                if (MeshCollider != null)
                {// Still attempt any fixes needed...
                    FixMeshCollider(mesh, MeshCollider);
                    return false;
                }
            }
            
            var BoxCollider = mesh.GetComponent<BoxCollider>();

            if (BoxCollider != null)
            {
                Debug.Log("MakeSureColliderExists: Mesh " + mesh.name + " already has a box collider, ignoring.");
                return false;
            }

            var CapsuleCollider = mesh.GetComponent<CapsuleCollider>();

            if (CapsuleCollider != null)
            {
                Debug.Log("MakeSureColliderExists: Mesh " + mesh.name + " already has a capsule collider, ignoring.");
                return false;
            }

            var SphereCollider = mesh.GetComponent<SphereCollider>();

            if (SphereCollider != null)
            {
                Debug.Log("MakeSureColliderExists: Mesh " + mesh.name + " already has a sphere collider, ignoring.");
                return false;
            }

            var WheelCollider = mesh.GetComponent<WheelCollider>();

            if (WheelCollider != null)
            {
                Debug.Log("MakeSureColliderExists: Mesh " + mesh.name + " already has a wheel collider, ignoring.");
                return false;
            }

            var TerrainCollider = mesh.GetComponent<TerrainCollider>();

            if (TerrainCollider != null)
            {
                Debug.Log("MakeSureColliderExists: Mesh " + mesh.name + " already has a terrain collider, ignoring.");
                return false;
            }

            MeshCollider = mesh.AddComponent<MeshCollider>();
            FixMeshCollider(mesh, MeshCollider);

            return true;
        }

        void FixMeshCollider(GameObject mesh, MeshCollider MeshCollider)
        {
            if (MeshCollider.sharedMesh == null)
            {// Make sure we have a mesh specified...
                Debug.Log("MakeSureColliderExists: Missing shared mesh for " + mesh.name + ". Attempting to fix.");

                if (mesh.GetComponent<MeshFilter>() != null)
                {
                    var renderer = mesh.GetComponent<MeshFilter>();
                    var mesh2 = renderer.sharedMesh;

                    if (mesh2 != null)
                    {
                        MeshCollider.sharedMesh = mesh2;
                        Debug.Log("MakeSureColliderExists: Missing shared mesh for " + mesh.name + " was set to " + mesh2.name + ".");
                    }
                    else
                    {
                        Debug.LogError("MakeSureColliderExists: Missing shared mesh for " + mesh.name + " could not be fixed.");
                    }
                }
                else if (mesh.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    var renderer = mesh.GetComponent<SkinnedMeshRenderer>();
                    var mesh2 = renderer.sharedMesh;

                    if (mesh2 != null)
                    {
                        MeshCollider.sharedMesh = mesh2;
                        Debug.Log("MakeSureColliderExists: Missing shared mesh for " + mesh.name + " was set to " + mesh2.name + ".");
                    }
                    else
                    {
                        Debug.LogError("MakeSureColliderExists: Missing shared mesh for " + mesh.name + " could not be fixed.");
                    }
                }
            }
        }
    
        public void ClearDuplicateColliders(GameObject mesh)
        {
            if (mesh.GetComponents<MeshCollider>().Length <= 1) return;
        
            foreach (var c in mesh.GetComponents<MeshCollider>())
                DestroyImmediate(c);
        
            MakeSureColliderExists(mesh);
        }
    }
}