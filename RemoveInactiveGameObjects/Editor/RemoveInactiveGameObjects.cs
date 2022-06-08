﻿// Disable 'obsolete' warnings
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

namespace RemoveInactiveGameObjects
{
    [ExecuteInEditMode]
    public class RemoveInactiveGameObjects : EditorWindow
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

        [MenuItem("Window/Unique Tools/Remove Inactive Gameobjects")]
        static void Open()
        {
            GetWindow<RemoveInactiveGameObjects>("RemoveInactiveGameObjects").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Remove Inactive Gameobjects"))
            {
                RemoveInactives();
            }
        }

        public static List<GameObject> FindAllObjectsInScene()
        {
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            List<GameObject> objectsInScene = new List<GameObject>();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                objectsInScene.Add(rootObjects[i]);
            }

            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i].transform.root)
                {
                    for (int i2 = 0; i2 < rootObjects.Length; i2++)
                    {
                        if (allObjects[i].transform.root == rootObjects[i2].transform && allObjects[i] != rootObjects[i2])
                        {
                            objectsInScene.Add(allObjects[i]);
                            break;
                        }
                    }
                }
            }
            return objectsInScene;
        }

        public void RemoveInactives()
        {
            //var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            //var objects = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            var objects = FindAllObjectsInScene();
            int totalObjects = objects.Count;
            int numCompleted = 0;

            ProgressBarInit("Removing Inactive Gameobjects...");

            int removedCount = 0;

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Removing Inactive Gameobjects...", (float)numCompleted / (float)totalObjects);
                removedCount += ClearInactives(obj);
                numCompleted++;
            }

            ProgressBarEnd();

            Debug.Log("Destroyed " + removedCount + " inactive game objects.");
        }
    
        public int ClearInactives(GameObject obj)
        {
            int removedCount = 0;
            
            if (obj != null && !obj.activeInHierarchy)
            {
                Debug.Log("Removing inactive game object: \"" + obj.name + "\".");
                DestroyImmediate(obj);
                removedCount++;
            }

            return removedCount;
        }
    }
}