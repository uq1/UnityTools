#define UNITY_PROGRESSBAR

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
using System.Linq;
using System.Reflection;

using System.Threading;
using System.Threading.Tasks;

namespace AutoColliderGenerator
{
    [ExecuteInEditMode]
    public class AutoColliderGenerator : EditorWindow
    {
        //private fields
        private MeshCollider MeshCollider;
        public bool removeOld = false;

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