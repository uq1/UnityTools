// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using static ProgressBarAPI.API;

namespace RemoveInactiveMeshRenderers
{
    [ExecuteInEditMode]
    public class RemoveInactiveMeshRenderers : EditorWindow
    {
        public bool removeFilters = true;
        
        [MenuItem("Window/Unique Tools/Remove Inactive Mesh Renderers")]
        static void Open()
        {
            GetWindow<RemoveInactiveMeshRenderers>("RemoveInactiveMeshRenderers").Show();
        }

        void OnGUI()
        {
#if INWINDOW_PROGRESS
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 15), progress, progressText);
#endif //INWINDOW_PROGRESS

            removeFilters = EditorGUILayout.Toggle("Remove Mesh Filters", removeFilters);

            if (GUILayout.Button("Remove Inactive Mesh Renderers"))
            {
                RemoveInactiveRendererObjects();
            }
        }

        public void RemoveInactiveRendererObjects()
        {
            var objects = UnityEngine.GameObject.FindObjectsOfType<GameObject>();
            int totalObjects = objects.Length;
            int numCompleted = 0;

            ProgressBarInit("Removing Inactive Mesh Renderers");

            foreach (GameObject obj in objects)
            {
                ProgressBarShow("Removing Inactive Mesh Renderers", (float)numCompleted / (float)totalObjects);
                InactiveRenderers(obj);
                numCompleted++;
            }

            ProgressBarShow("Removing Inactive Mesh Renderers", (float)totalObjects / (float)totalObjects);
            ProgressBarEnd();
        }
    
        public void InactiveRenderers(GameObject obj)
        {
            if (obj.GetComponents<MeshRenderer>().Length > 0)
            {
                foreach (var c in obj.GetComponents<MeshRenderer>())
                {
                    if (c != null && !c.enabled)
                    {
                        Debug.Log("Object " + c.name + " removing inactive Mesh Renderer.");
                        DestroyImmediate(c);
                        
                        if (removeFilters)
                        {
                            if (obj.GetComponents<MeshFilter>().Length > 0)
                            {
                                foreach (var f in obj.GetComponents<MeshFilter>())
                                {
                                    if (f != null)
                                    {
                                        Debug.Log("Object " + f.name + " removing inactive Mesh Filter.");
                                        DestroyImmediate(f);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            if (obj.GetComponents<SkinnedMeshRenderer>().Length > 0)
            {
                foreach (var c in obj.GetComponents<SkinnedMeshRenderer>())
                {
                    if (c != null && !c.enabled)
                    {
                        Debug.Log("Object " + c.name + " removing inactive Mesh Renderer.");
                        DestroyImmediate(c);
                    }
                }
            }
        }
    }
}