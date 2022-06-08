// Disable 'obsolete' warnings
#pragma warning disable 0618

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Reflection;

using System.Threading;
using System.Threading.Tasks;
using UnityEngine.AI;

namespace RemoveAllColliders
{
    [ExecuteInEditMode]
    public class RemoveAllColliders : EditorWindow
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

        [MenuItem("Window/Unique Tools/Remove All Colliders")]
        static void Open()
        {
            GetWindow<RemoveAllColliders>("RemoveAllColliders").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Remove Colliders"))
            {
                RemoveInactiveColliderObjects();
            }
        }

        public void RemoveInactiveColliderObjects()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Removing Colliders...");

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Removing Colliders...", (float)numCompleted / (float)totalObjects);
                InactiveColliders(obj);
                numCompleted++;
            }

            ProgressBarEnd();
        }
    
        public void InactiveColliders(GameObject obj)
        {
            if (obj.GetComponents<MeshCollider>().Length > 0)
            {
                Debug.Log("Clear Colliders: Object " + obj.name + " removing " + obj.GetComponents<MeshCollider>().Length + " mesh colliders.");
                
                foreach (var c in obj.GetComponents<MeshCollider>())
                {
                    if (c != null)
                    {
                        DestroyImmediate(c);
                    }
                }
            }
            
            if (obj.GetComponents<BoxCollider>().Length > 0)
            {
                Debug.Log("Clear Colliders: Object " + obj.name + " removing " + obj.GetComponents<BoxCollider>().Length + " box colliders.");
                
                foreach (var c in obj.GetComponents<BoxCollider>())
                {
                    if (c != null)
                    {
                        DestroyImmediate(c);
                    }
                }
            }
            
            if (obj.GetComponents<SphereCollider>().Length > 0)
            {
                Debug.Log("Clear Colliders: Object " + obj.name + " removing " + obj.GetComponents<SphereCollider>().Length + " sphere colliders.");
                
                foreach (var c in obj.GetComponents<SphereCollider>())
                {
                    if (c != null)
                    {
                        DestroyImmediate(c);
                    }
                }
            }
            
            if (obj.GetComponents<WheelCollider>().Length > 0)
            {
                Debug.Log("Clear Colliders: Object " + obj.name + " removing " + obj.GetComponents<WheelCollider>().Length + " wheel colliders.");
                
                foreach (var c in obj.GetComponents<WheelCollider>())
                {
                    if (c != null)
                    {
                        DestroyImmediate(c);
                    }
                }
            }
        }
    }
}