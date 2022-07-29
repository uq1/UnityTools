// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using static ProgressBarAPI.API;

namespace CleanupMissingScripts
{
    [ExecuteInEditMode]
    public class CleanupMissingScripts : EditorWindow
    {
        [MenuItem("Window/Unique Tools/Cleanup Missing Scripts")]
        static void Open()
        {
            GetWindow<CleanupMissingScripts>("CleanupMissingScripts").Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Cleanup ALL Scripts"))
            {
                CleanupAllMissingScripts();
            }

            if (GUILayout.Button("Cleanup Selected Scripts"))
            {
                CleanupSelectedMissingScripts();
            }

            if (GUILayout.Button("Cleanup Selected Scripts (Recursive)"))
            {
                RecursiveCleanupMissingScripts();
            }
        }

        public void CleanupAllMissingScripts()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Cleaning Scripts...");

            foreach (GameObject gameObject in objects)
            {
                ProgressBarShow("Cleaning scripts for \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                // We must use the GetComponents array to actually detect missing components
                var components = gameObject.GetComponents<Component>();

                // Create a serialized object so that we can edit the component list
                var serializedObject = new SerializedObject(gameObject);
                // Find the component list property
                var prop = serializedObject.FindProperty("m_Component");

                // Track how many components we've removed
                int r = 0;

                // Iterate over all components
                for (int j = 0; j < components.Length; j++)
                {
                    // Check if the ref is null
                    if (components[j] == null)
                    {
                        // If so, remove from the serialized component array
                        prop.DeleteArrayElementAtIndex(j - r);
                        // Increment removed count
                        r++;
                    }
                }

                // Apply our changes to the game object
                serializedObject.ApplyModifiedProperties();

                numCompleted++;
            }

            ProgressBarEnd();
        }

        public void CleanupSelectedMissingScripts()
        {
            ProgressBarInit("Cleaning Scripts...");

            int totalObjects = Selection.gameObjects.Length;
            int numCompleted = 0;

            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                var gameObject = Selection.gameObjects[i];

                ProgressBarShow("Cleaning scripts for \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                // We must use the GetComponents array to actually detect missing components
                var components = gameObject.GetComponents<Component>();

                // Create a serialized object so that we can edit the component list
                var serializedObject = new SerializedObject(gameObject);
                // Find the component list property
                var prop = serializedObject.FindProperty("m_Component");

                // Track how many components we've removed
                int r = 0;

                // Iterate over all components
                for (int j = 0; j < components.Length; j++)
                {
                    // Check if the ref is null
                    if (components[j] == null)
                    {
                        // If so, remove from the serialized component array
                        prop.DeleteArrayElementAtIndex(j - r);
                        // Increment removed count
                        r++;
                    }
                }

                // Apply our changes to the game object
                serializedObject.ApplyModifiedProperties();

                numCompleted++;
            }

            ProgressBarEnd();
        }


        public void RecursiveCleanupMissingScripts()
        {
            ProgressBarInit("Cleaning Scripts...");

            Transform[] allTransforms = Selection.gameObjects[0].GetComponentsInChildren<Transform>(true);

            int totalObjects = allTransforms.Length;
            int numCompleted = 0;

            for (int i = 0; i < allTransforms.Length; i++)
            {
                var gameObject = allTransforms[i].gameObject;

                ProgressBarShow("Cleaning scripts for \"" + gameObject.name + "\".", (float)numCompleted / (float)totalObjects);

                // We must use the GetComponents array to actually detect missing components
                var components = gameObject.GetComponents<Component>();

                // Create a serialized object so that we can edit the component list
                var serializedObject = new SerializedObject(gameObject);
                // Find the component list property
                var prop = serializedObject.FindProperty("m_Component");

                // Track how many components we've removed
                int r = 0;

                // Iterate over all components
                for (int j = 0; j < components.Length; j++)
                {
                    // Check if the ref is null
                    if (components[j] == null)
                    {
                        // If so, remove from the serialized component array
                        prop.DeleteArrayElementAtIndex(j - r);
                        // Increment removed count
                        r++;
                    }
                }


                // Apply our changes to the game object
                serializedObject.ApplyModifiedProperties();

                numCompleted++;
            }

            ProgressBarEnd();
        }
    }
}
