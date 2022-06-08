﻿// Disable 'obsolete' warnings
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
using System.Reflection;

using System.Threading;
using System.Threading.Tasks;

namespace FindMeshfilterErrors
{
    public class FindMeshfilterErrorsWindow : EditorWindow
    {
        [MenuItem("Window/Unique Tools/Find Meshfilter Errors")]
        static void Open()
        {
            GetWindow<FindMeshfilterErrorsWindow>("FindMeshfilterErrors").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Error Check"))
            {
                FindMeshErrors();
            }
        }

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

        void FindMeshErrors()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Checking...");

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Checking \"" + obj.name + "\"...", (float)numCompleted / (float)totalObjects);

                if (obj.GetComponent<MeshRenderer>() || obj.GetComponent<SkinnedMeshRenderer>())
                {
                    FindMeshErrors(obj);
                }

                numCompleted++;
            }

            ProgressBarEnd();
        }

        void FindMeshErrors(GameObject _target)
        {
            if (_target == null)
            {
                //Debug.Log("No target selected.");
                return;
            }

            var meshFilters = _target.GetComponentsInChildren<MeshFilter>();

            if (meshFilters == null)
            {
                Debug.Log("Object \"" + _target.name + "\" - " + "meshFilters is null.");
                return;
            }

            foreach (var meshFilter in meshFilters)
            {
                //Debug.Log("Object \"" + _target.name + "\" - " + "MeshFilter \"" + meshFilter.name + "\"");
                
                var mesh = meshFilter.sharedMesh;
                
                if (mesh == null)
                {
                    Debug.Log("Object \"" + _target.name + "\" - " + "MeshFilter \"" + meshFilter.name + "\" has no mesh.");
                    continue;
                }
                
                if (meshFilter.GetComponent<Renderer>() == null)
                {
                    Debug.Log("Object \"" + _target.name + "\" - " + "MeshFilter \"" + meshFilter.name + "\" has no renderer.");
                    continue;
                }
                
                var materials = meshFilter.GetComponent<Renderer>().sharedMaterials;
                
                if (materials == null)
                {
                    Debug.Log("Object \"" + _target.name + "\" - " + "MeshFilter \"" + meshFilter.name + "\" has no materials.");
                    continue;
                }
            }
        }
    }
}