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

namespace ResetAllLayers
{
    [ExecuteInEditMode]
    public class ResetAllLayers : EditorWindow
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

        [MenuItem("Window/Unique Tools/Reset Layers")]
        static void Open()
        {
            GetWindow<ResetAllLayers>("ResetAllLayers").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Reset Layers"))
            {
                RemoveRigidBodyObjects();
            }
        }

        public void RemoveRigidBodyObjects()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Resetting Layers...");

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Resetting Layers...", (float)numCompleted / (float)totalObjects);
                obj.layer = LayerMask.NameToLayer("Default");
                numCompleted++;
            }

            ProgressBarEnd();
        }
    }
}