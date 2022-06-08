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


namespace RevertMaterials
{
    [ExecuteInEditMode]
    public class RevertMaterials : EditorWindow
    {
        Renderer[] All_Renderer_Objects;

        [MenuItem("Window/Unique Tools/Revert Instanced Materials")]
        static void Open()
        {
            GetWindow<RevertMaterials>("RevertMaterials").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Revert Materials"))
            {
                All_Renderer_Objects = UnityEngine.GameObject.FindObjectsOfType<Renderer>();
                RevertAllRendererMaterials();
            }
        }

        void RevertAllRendererMaterials()
        {
            for (int i = 0; i < All_Renderer_Objects.Length; i++)
            {
                Material[] originalMats = Resources.FindObjectsOfTypeAll<Material>();
                Material[] mats = All_Renderer_Objects[i].sharedMaterials;
                Material[] tempMats = new Material[mats.Length];
                for (int j = 0; j < originalMats.Length; j++)
                {
                    for (int k = 0; k < mats.Length; k++)
                    {
                        if (originalMats[j].name + " (Instance) (Instance)" == mats[k].name)
                        {
                            tempMats[k] = originalMats[j];
                        }

                        else if (originalMats[j].name + " (Instance)" == mats[k].name)
                        {
                            tempMats[k] = originalMats[j];
                        }


                        else if (originalMats[j].name == mats[k].name)
                        {
                            tempMats[k] = originalMats[j];
                        }
                    }

                }

                All_Renderer_Objects[i].sharedMaterials = tempMats;
            }
        }
    }
}
 