// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using static ProgressBarAPI.API;

namespace ResetAllLayers
{
    [ExecuteInEditMode]
    public class ResetAllLayers : EditorWindow
    {
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