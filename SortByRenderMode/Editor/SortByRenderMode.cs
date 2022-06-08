using Sirenix.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
 
public class SortByRenderMode : ScriptableObject
{
    [MenuItem("GameObject/Sorting/Sort By Render Mode")]
    /*public static void SortActiveTransformChildren()
    {
        if (Selection.activeTransform)
        {
            Sort(Selection.activeTransform);
        }
        else
        {
            Debug.LogErrorFormat("No game object selected in Hierarchy");
        }
    }
 
    private static void Sort(Transform current)
    {
        IOrderedEnumerable<Transform> orderedChildren = current.Cast<Transform>().OrderBy(tr => tr.GetComponent<MeshRenderer>().sharedMaterials[0].renderQueue);
 
        foreach (Transform child in orderedChildren)
        {
            Undo.SetTransformParent(child, null, "Reorder children");
            Undo.SetTransformParent(child, current, "Reorder children");
        }
    }*/

    public static void MenuAddChild()
    {
        RecursiveSort(Selection.activeTransform);
    }

    public enum BlendMode
    {
        Opaque,
        Cutout,
        Fade,       // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
    }

    /*private static int SortByNameFunc(Transform o1, Transform o2)
    {
        return o1.name.CompareTo(o2.name);
    }*/
    
    private static int SortByRenderModeFunc(Transform o1, Transform o2)
    {
        // mesh1
        MeshRenderer render1 = o1.GetComponent<MeshRenderer>();

        if (render1 == null)
        {
            return 0;
        }

        if (render1.sharedMaterials == null || render1.sharedMaterials.Length <= 0)
        {
            return 0;
        }

        Material material1 = render1.sharedMaterials[0];
        BlendMode blend1 = (BlendMode)material1.GetFloat("_Mode");


        // mesh2
        MeshRenderer render2 = o2.GetComponent<MeshRenderer>();

        if (render2 == null)
        {
            return 0;
        }

        if (render2.sharedMaterials == null || render2.sharedMaterials.Length <= 0)
        {
            return 0;
        }

        Material material2 = render2.sharedMaterials[0];
        BlendMode blend2 = (BlendMode)material2.GetFloat("_Mode");

        // Sort...

        /* Opaque, Cutout, Fade, Transparent - This order is optimal for sorting anyway... */
        if (blend1 < blend2)
            return -1;

        if (blend1 > blend2)
            return 1;

        return 0;
}

    static void RecursiveSort(Transform current)
    {
        List<Transform> children = new List<Transform>();

        foreach (Transform child in current)
        {
            children.Add(child);
        }

        children.Sort(SortByRenderModeFunc); // sorted by name now

        /*foreach (Transform child in children)
        {
            child.parent = null;
        }

        foreach (Transform child in children)
        {
            child.parent = current;
        }*/

        foreach (Transform child in children)
        {
            Undo.SetTransformParent(child, null, "Reorder children");
            Undo.SetTransformParent(child, current, "Reorder children");
        }
    }
}
