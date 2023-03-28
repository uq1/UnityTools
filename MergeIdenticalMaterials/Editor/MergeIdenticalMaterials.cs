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
using System.Reflection;

using System.Threading;
using System.Threading.Tasks;

namespace MergeIdenticalMaterials
{
    [ExecuteInEditMode]
    public class MergeIdenticalMaterials : EditorWindow
    {
        public bool texturedColorCheck = true;
        public bool showChanges = false;

        private int numReplaced = 0;
        private int numDestroyed = 0;

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

        [MenuItem("Window/Unique Tools/Merge Identical Materials")]
        static void Open()
        {
            GetWindow<MergeIdenticalMaterials>("MergeIdenticalMaterials").Show();
        }

        Material[] allMaterials;
        List<Material> uniqueMaterials = new List<Material>();

        void OnGUI()
        {
            texturedColorCheck = EditorGUILayout.Toggle("Check colors are identical when textured?", texturedColorCheck);
            showChanges = EditorGUILayout.Toggle("List changes in console?", showChanges);
            EditorGUILayout.Space();

            if (GUILayout.Button("Merge Identical Materials"))
            {
                numReplaced = 0;
                numDestroyed = 0;
                MergeMaterials();

                if (numReplaced == 0 && numDestroyed == 0)
                {
                    Debug.Log("Nothing changed, all materials instances are unique.");
                }
                else
                {
                    Debug.Log(numReplaced + " references to old materials were replaced. " + numDestroyed + " original materials were deleted from project.");
                }
            }
        }

        public bool CompareMaterials(Material origMat, Material uniqueMat)
        {
            if (origMat.shader != uniqueMat.shader)
            {
                return false;
            }

            if (origMat.GetType() != uniqueMat.GetType())
            {
                return false;
            }

            if (origMat.mainTexture != uniqueMat.mainTexture)
            {
                if (origMat.mainTexture == null || uniqueMat.mainTexture == null)
                {
                    return false;
                }

                if (origMat.mainTexture.imageContentsHash != uniqueMat.mainTexture.imageContentsHash)
                {
                    return false;
                }
            }

            if (origMat.color != uniqueMat.color && !(!texturedColorCheck && (origMat.mainTexture != null || uniqueMat.mainTexture != null)))
            {
                return false;
            }

            if (origMat.HasProperty("_MainTex") != uniqueMat.HasProperty("_MainTex"))
            {
                return false;
            }

            if (origMat.HasProperty("_BaseMap") != uniqueMat.HasProperty("_BaseMap"))
            {
                return false;
            }

            if (origMat.HasProperty("_BaseColorMap") != uniqueMat.HasProperty("_BaseColorMap"))
            {
                return false;
            }

            if (origMat.HasProperty("_EmissionMap") != uniqueMat.HasProperty("_EmissionMap"))
            {
                return false;
            }

            if (origMat.HasProperty("_MainTex") && uniqueMat.HasProperty("_MainTex"))
            {
                if (origMat.GetTexture("_MainTex") != uniqueMat.GetTexture("_MainTex"))
                {
                    if (origMat.GetTexture("_MainTex").imageContentsHash != uniqueMat.GetTexture("_MainTex").imageContentsHash)
                    {
                        return false;
                    }
                }
            }

            if (origMat.HasProperty("_BaseMap") && uniqueMat.HasProperty("_BaseMap"))
            {
                if (origMat.GetTexture("_BaseMap") != uniqueMat.GetTexture("_BaseMap"))
                {
                    if (origMat.GetTexture("_BaseMap").imageContentsHash != uniqueMat.GetTexture("_BaseMap").imageContentsHash)
                    {
                        return false;
                    }
                }
            }

            if (origMat.HasProperty("_BaseColorMap") && uniqueMat.HasProperty("_BaseColorMap"))
            {
                if (origMat.GetTexture("_BaseColorMap") != uniqueMat.GetTexture("_BaseColorMap"))
                {
                    if (origMat.GetTexture("_BaseColorMap").imageContentsHash != uniqueMat.GetTexture("_BaseColorMap").imageContentsHash)
                    {
                        return false;
                    }
                }
            }

            if (origMat.HasProperty("_EmissionMap") && uniqueMat.HasProperty("_EmissionMap"))
            {
                if (origMat.GetTexture("_EmissionMap") != uniqueMat.GetTexture("_EmissionMap"))
                {
                    if (origMat.GetTexture("_EmissionMap").imageContentsHash != uniqueMat.GetTexture("_EmissionMap").imageContentsHash)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public Material GetReplacementMaterial(Material origMat)
        {
            foreach (Material uniqueMat in uniqueMaterials)
            {
                if (origMat.Equals(uniqueMat))
                {
                    return uniqueMat;
                }

                if (CompareMaterials(origMat, uniqueMat))
                {
                    return uniqueMat;
                }
            }

            // This should never happen...
            return origMat;
        }

        public void MergeMaterials()
        {
            allMaterials = Resources.FindObjectsOfTypeAll<Material>();

            // Create a list of all unique materials...
            uniqueMaterials.Clear();

            Debug.Log("Calculating all material choices...");

            foreach (Material origMat in allMaterials)
            {
                bool found = false;

                foreach (Material uniqueMat in uniqueMaterials)
                {
                    if (origMat.Equals(uniqueMat))
                    {
                        found = true;
                        break;
                    }

                    if (CompareMaterials(origMat, uniqueMat))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    uniqueMaterials.Add(origMat);
                }
            }

            Debug.Log("Orig mats: " + allMaterials.Length);
            Debug.Log("Unique mats: " + uniqueMaterials.Count);


            ProgressBarInit("Merging Materials...");

            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

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

            // Delete old materials...
            foreach (Material origMat in allMaterials)
            {
                bool found = false;

                foreach (Material uniqueMat in uniqueMaterials)
                {
                    if (origMat.Equals(uniqueMat))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    uniqueMaterials.Remove(origMat);
                    DestroyImmediate(origMat, true);
                }
            }
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

        private void ReplaceMaterialInObject(GameObject obj, int instance, Material newMaterial)
        {// This is so dumb, WTB, pointer to original structure, not a fkin copy...
            //
            // YAY! 3 versions of the exact same code, unity + c# ftw!!!
            //
            if (obj.GetComponent<Renderer>())
            {// Because unity can't have a generic render method like sane engines would...
                if (showChanges)
                {
                    Debug.Log("Object " + obj.name + " contains a reference to " + obj.GetComponent<Renderer>().sharedMaterials[instance].name + ", which was replaced with " + newMaterial.name + ".");
                }

                numReplaced++;

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
                if (showChanges)
                {
                    Debug.Log("Object " + obj.name + " contains a reference to " + obj.GetComponent<MeshRenderer>().sharedMaterials[instance].name + ", which was replaced with " + newMaterial.name + ".");
                }

                numReplaced++;

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
                if (showChanges)
                {
                    Debug.Log("Object " + obj.name + " contains a reference to " + obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials[instance].name + ", which was replaced with " + newMaterial.name + ".");
                }

                numReplaced++;

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
                    Material oldMat = obj.GetComponent<Renderer>().sharedMaterials[i];

                    if (oldMat == null)
                    {
                        continue;
                    }

                    Material newMat = GetReplacementMaterial(oldMat);

                    if (newMat != null && oldMat != newMat)
                    {
                        ReplaceMaterialInObject(obj, i, newMat);
                    }
                }
            }

            if (obj.GetComponent<MeshRenderer>())
            {
                for (int i = 0; i < obj.GetComponent<MeshRenderer>().sharedMaterials.Length; i++)
                {
                    Material oldMat = obj.GetComponent<MeshRenderer>().sharedMaterials[i];

                    if (oldMat == null)
                    {
                        continue;
                    }

                    Material newMat = GetReplacementMaterial(oldMat);

                    if (newMat != null && oldMat != newMat)
                    {
                        ReplaceMaterialInObject(obj, i, newMat);
                    }
                }
            }

            if (obj.GetComponent<SkinnedMeshRenderer>())
            {
                for (int i = 0; i < obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length; i++)
                {
                    Material oldMat = obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials[i];

                    if (oldMat == null)
                    {
                        continue;
                    }

                    Material newMat = GetReplacementMaterial(oldMat);

                    if (newMat != null && oldMat != newMat)
                    {
                        ReplaceMaterialInObject(obj, i, newMat);
                    }
                }
            }
        }
    }
}
