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

namespace RemoveRigidBodies
{
    [ExecuteInEditMode]
    public class RemoveRigidBodies : EditorWindow
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

        [MenuItem("Window/Unique Tools/Remove Rigid Bodies")]
        static void Open()
        {
            GetWindow<RemoveRigidBodies>("RemoveRigidBodies").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Remove Rigid Bodies"))
            {
                RemoveRigidBodyObjects();
            }
        }

        public void RemoveRigidBodyObjects()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Removing Rigid Bodies...");

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Removing Rigid Bodies...", (float)numCompleted / (float)totalObjects);
                ClearRigidBodies(obj);
                numCompleted++;
            }

            ProgressBarEnd();
        }
    
        public void ClearRigidBodies(GameObject obj)
        {
            if (obj.GetComponents<Rigidbody>().Length > 0)
            {
                Debug.Log("ClearRigidBodies: Object " + obj.name + " removing " + obj.GetComponents<Rigidbody>().Length + " old RigidBodies.");

                foreach (var c in obj.GetComponents<Rigidbody>())
                {
                    DestroyImmediate(c);
                }
            }
        }
    }
}