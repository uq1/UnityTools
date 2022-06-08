using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
 
public class SortByOptimize : ScriptableObject
{
    [MenuItem("GameObject/Sorting/Optimize")]
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

    private static int SortByNameFunc(Transform o1, Transform o2)
    {
        return o1.name.CompareTo(o2.name);
    }

    private static int SortByTransparancyFunc(Transform o1, Transform o2)
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
        BlendMode blend1;


        try
        {
            blend1 = (BlendMode)material1.GetFloat("_Mode");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return 0;
        }

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
        BlendMode blend2;

        try
        {
            blend2 = (BlendMode)material2.GetFloat("_Mode");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return 0;
        }

        // Sort...

        /* Opaque, Cutout, Fade, Transparent - This order is optimal for sorting anyway... */
        if (blend1 < blend2)
            return -1;

        if (blend1 > blend2)
            return 1;

        return 0;
    }

    private static int SortByEmissionFunc(Transform o1, Transform o2)
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
        var realtimeEmission1 = (material1.globalIlluminationFlags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive | MaterialGlobalIlluminationFlags.AnyEmissive)) > 0 ? 1 : 0;

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
        var realtimeEmission2 = (material2.globalIlluminationFlags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive | MaterialGlobalIlluminationFlags.AnyEmissive)) > 0 ? 1 : 0;

        // Sort...

        /* Opaque, Cutout, Fade, Transparent - This order is optimal for sorting anyway... */
        if (realtimeEmission1 < realtimeEmission2)
            return -1;

        if (realtimeEmission1 > realtimeEmission2)
            return 1;

        return 0;
    }

    private static int SortByBoundsFunc(Transform o1, Transform o2)
    {
        MeshRenderer render1 = o1.GetComponent<MeshRenderer>();

        if (render1 == null)
        {
            return 0;
        }

        MeshRenderer render2 = o2.GetComponent<MeshRenderer>();

        if (render2 == null)
        {
            return 0;
        }

        // Sort...
        if (render1.bounds.size.magnitude > render2.bounds.size.magnitude)
            return -1;

        if (render1.bounds.size.magnitude < render2.bounds.size.magnitude)
            return 1;

        return 0;
    }

    private static int SortByAllFunc(Transform o1, Transform o2)
    {
        int sort1 = SortByNameFunc(o1, o2);
        int sort2 = SortByBoundsFunc(o1, o2);
        int sort3 = SortByEmissionFunc(o1, o2);
        int sort4 = SortByTransparancyFunc(o1, o2);

        int s1 = sort1 + (sort2 * 2) + (sort3 * 4) + (sort4 * 8);
        int s2 = -sort1 + (-sort2 * 2) + (-sort3 * 4) + (-sort4 * 8);

        // Sort...
        if (s1 < s2)
            return -1;

        if (s1 > s2)
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

        children.Sort(SortByAllFunc);

        foreach (Transform child in children)
        {
            Undo.SetTransformParent(child, null, "Reorder children");
            Undo.SetTransformParent(child, current, "Reorder children");
        }
    }
}
