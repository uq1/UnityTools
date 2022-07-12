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

namespace ReplaceMaterial
{
    [ExecuteInEditMode]
    public class ReplaceMaterial : EditorWindow
    {
        public Material newMaterial = null;
        public Material oldMaterial1 = null;
        public Material oldMaterial2 = null;
        public Material oldMaterial3 = null;
        public Material oldMaterial4 = null;
        public bool replaceMissing = false;
        public bool showChanges = false;

        private int numReplaced = 0;

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

        [MenuItem("Window/Unique Tools/Replace Materials")]
        static void Open()
        {
            GetWindow<ReplaceMaterial>("ReplaceMaterial").Show();
        }

        void OnGUI()
        {
            newMaterial = (Material)EditorGUILayout.ObjectField("New Material", newMaterial, typeof(Material));
            EditorGUILayout.Space();

            oldMaterial1 = (Material)EditorGUILayout.ObjectField("Replace Old Material 1", oldMaterial1, typeof(Material));
            oldMaterial2 = (Material)EditorGUILayout.ObjectField("Replace Old Material 2", oldMaterial2, typeof(Material));
            oldMaterial3 = (Material)EditorGUILayout.ObjectField("Replace Old Material 3", oldMaterial3, typeof(Material));
            oldMaterial4 = (Material)EditorGUILayout.ObjectField("Replace Old Material 4", oldMaterial4, typeof(Material));

            EditorGUILayout.Space();

            replaceMissing = EditorGUILayout.Toggle("Replace missing?", replaceMissing);
            EditorGUILayout.Space();

            showChanges = EditorGUILayout.Toggle("List changes in console?", showChanges);
            EditorGUILayout.Space();

            if (GUILayout.Button("Replace Materials"))
            {
                ReplaceMaterials();
                Debug.Log(numReplaced + " references to old materials were replaced.");
                numReplaced = 0;
            }
        }

        public void ReplaceMaterials()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Replacing Materials...");

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Replacing Materials in \"" + obj.name + "\".", (float)numCompleted / (float)totalObjects);

                if (obj.GetComponent<Renderer>() || obj.GetComponent<MeshRenderer>() || obj.GetComponent<SkinnedMeshRenderer>())
                {
                    ReplaceMaterialsInObject(obj);
                }

                numCompleted++;
            }

            ProgressBarEnd();
        }

        private void PrintList(Material[] list)
        {
            string output = "";

            foreach (var x in list)
            {
                if (x == null)
                {
                    output += "null" + ", ";
                }
                else
                {
                    output += x.name + ", ";
                }
            }
            output = output.Remove(output.Length - 2);
            Debug.Log(output);
        }

        private void ReplaceMaterialInObject(GameObject obj, int instance)
        {// This is so dumb, WTB, pointer to original structure, not a fkin copy...
            //
            // YAY! 3 versions of the exact same code, unity + c# ftw!!!
            //
            if (obj.GetComponent<Renderer>())
            {// Because unity can't have a generic render method like sane engines would...
                if (obj.GetComponent<Renderer>().sharedMaterials[instance] == null && replaceMissing)
                {
                    Debug.Log("Object " + obj.name + " contains a null reference, which was replaced with " + newMaterial.name + ".");
                    numReplaced++;
                }
                else
                {
                    Debug.Log("Object " + obj.name + " contains a reference to " + obj.GetComponent<Renderer>().sharedMaterials[instance].name + ", which was replaced with " + newMaterial.name + ".");
                    numReplaced++;
                }

                // Build a completely new list of materials, and copy the original list to it, because, we have no direct access...
                Material[] newMats = new Material[obj.GetComponent<Renderer>().sharedMaterials.Length];

                for (int j = 0; j < obj.GetComponent<Renderer>().sharedMaterials.Length; j++)
                {
                    newMats[j] = obj.GetComponent<Renderer>().sharedMaterials[j];
                }

                // Replace the selected material instance in the new list...
                newMats[instance] = newMaterial;

                /*
                Debug.Log("Old List:");
                PrintList(obj.GetComponent<Renderer>().sharedMaterials);

                Debug.Log("New List:");
                PrintList(newMats);
                */

                // Set unity to use the new list of materials, not the original one...
                obj.GetComponent<Renderer>().sharedMaterials = newMats;
            }

            if (obj.GetComponent<MeshRenderer>())
            {// Because unity can't have a generic render method like sane engines would...
                if (obj.GetComponent<MeshRenderer>().sharedMaterials[instance] == null && replaceMissing)
                {
                    Debug.Log("Object " + obj.name + " contains a null reference, which was replaced with " + newMaterial.name + ".");
                    numReplaced++;
                }
                else
                {
                    Debug.Log("Object " + obj.name + " contains a reference to " + obj.GetComponent<MeshRenderer>().sharedMaterials[instance].name + ", which was replaced with " + newMaterial.name + ".");
                    numReplaced++;
                }

                // Build a completely new list of materials, and copy the original list to it, because, we have no direct access...
                Material[] newMats = new Material[obj.GetComponent<MeshRenderer>().sharedMaterials.Length];

                for (int j = 0; j < obj.GetComponent<MeshRenderer>().sharedMaterials.Length; j++)
                {
                    newMats[j] = obj.GetComponent<MeshRenderer>().sharedMaterials[j];
                }

                // Replace the selected material instance in the new list...
                newMats[instance] = newMaterial;

                /*
                Debug.Log("Old List:");
                PrintList(obj.GetComponent<MeshRenderer>().sharedMaterials);

                Debug.Log("New List:");
                PrintList(newMats);
                */

                // Set unity to use the new list of materials, not the original one...
                obj.GetComponent<MeshRenderer>().sharedMaterials = newMats;
            }

            if (obj.GetComponent<SkinnedMeshRenderer>())
            {// Because unity can't have a generic render method like sane engines would...
                if (obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials[instance] == null && replaceMissing)
                {
                    Debug.Log("Object " + obj.name + " contains a null reference, which was replaced with " + newMaterial.name + ".");
                    numReplaced++;
                }
                else
                {
                    Debug.Log("Object " + obj.name + " contains a reference to " + obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials[instance].name + ", which was replaced with " + newMaterial.name + ".");
                    numReplaced++;
                }

                // Build a completely new list of materials, and copy the original list to it, because, we have no direct access...
                Material[] newMats = new Material[obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length];

                for (int j = 0; j < obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length; j++)
                {
                    newMats[j] = obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials[j];
                }

                // Replace the selected material instance in the new list...
                newMats[instance] = newMaterial;

                /*
                Debug.Log("Old List:");
                PrintList(obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials);

                Debug.Log("New List:");
                PrintList(newMats);
                */

                // Set unity to use the new list of materials, not the original one...
                obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials = newMats;
            }
        }

        private void ReplaceMaterialsInObject(GameObject obj)
        {
            if (obj.GetComponent<Renderer>())
            {
                for (int i = 0; i < obj.GetComponent<Renderer>().sharedMaterials.Length; i++)
                {
                    if (obj.GetComponent<Renderer>().sharedMaterials[i] == null)
                    {
                        if (replaceMissing)
                        {
                            Debug.Log("Object " + obj.name + " contains a null reference, which was replaced with " + newMaterial.name + ".");
                            obj.GetComponent<Renderer>().sharedMaterials[i] = newMaterial;
                            numReplaced++;
                        }

                        continue;
                    }

                    if (oldMaterial1 != null && obj.GetComponent<Renderer>().sharedMaterials[i] == oldMaterial1)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                    else if (oldMaterial2 != null && obj.GetComponent<Renderer>().sharedMaterials[i] == oldMaterial2)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                    else if (oldMaterial3 != null && obj.GetComponent<Renderer>().sharedMaterials[i] == oldMaterial3)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                    else if (oldMaterial4 != null && obj.GetComponent<Renderer>().sharedMaterials[i] == oldMaterial4)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                }
            }

            if (obj.GetComponent<MeshRenderer>())
            {
                for (int i = 0; i < obj.GetComponent<MeshRenderer>().sharedMaterials.Length; i++)
                {
                    if (obj.GetComponent<MeshRenderer>().sharedMaterials[i] == null)
                    {
                        if (replaceMissing)
                        {
                            ReplaceMaterialInObject(obj, i);
                        }

                        continue;
                    }

                    if (oldMaterial1 != null && obj.GetComponent<MeshRenderer>().sharedMaterials[i] == oldMaterial1)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                    else if (oldMaterial2 != null && obj.GetComponent<MeshRenderer>().sharedMaterials[i] == oldMaterial2)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                    else if (oldMaterial3 != null && obj.GetComponent<MeshRenderer>().sharedMaterials[i] == oldMaterial3)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                    else if (oldMaterial4 != null && obj.GetComponent<MeshRenderer>().sharedMaterials[i] == oldMaterial4)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                }
            }

            if (obj.GetComponent<SkinnedMeshRenderer>())
            {
                for (int i = 0; i < obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length; i++)
                {
                    if (obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials[i] == null)
                    {
                        if (replaceMissing)
                        {
                            ReplaceMaterialInObject(obj, i);
                        }

                        continue;
                    }

                    if (oldMaterial1 != null && obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials[i] == oldMaterial1)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                    else if (oldMaterial2 != null && obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials[i] == oldMaterial2)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                    else if (oldMaterial3 != null && obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials[i] == oldMaterial3)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                    else if (oldMaterial4 != null && obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials[i] == oldMaterial4)
                    {
                        ReplaceMaterialInObject(obj, i);
                    }
                }
            }
        }
    }
}
