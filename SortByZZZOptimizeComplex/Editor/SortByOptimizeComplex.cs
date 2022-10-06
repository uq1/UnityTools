using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
 
public class SortByOptimizeComplex : ScriptableObject
{
    [MenuItem("GameObject/Sorting/Optimize (Complex)")]
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

    private static double GetMeshSize(Mesh mesh)
    {
        var oldVerts = new List<Vector3>();
        mesh.GetVertices(oldVerts);

        int subCount = mesh.subMeshCount;

        double size = 0;

        for (int s = 0; s < subCount; s++)
        {
            var oldTris = mesh.GetTriangles(s);

            for (int j = 0; j < oldTris.Length; j += 3)
            {
                var oldTri1 = oldTris[j];
                var oldVert1 = oldVerts[oldTri1];

                var oldTri2 = oldTris[j + 1];
                var oldVert2 = oldVerts[oldTri2];

                var oldTri3 = oldTris[j + 2];
                var oldVert3 = oldVerts[oldTri3];

                /*Bounds b = new Bounds(Vector3.zero, Vector3.zero);
                b.Encapsulate(oldVert1);
                b.Encapsulate(oldVert2);
                b.Encapsulate(oldVert3);
                size += b.size.magnitude;
                */

                double tsize = (Vector3.Distance(oldVert1, oldVert2) * Vector3.Distance(oldVert1, oldVert3)) / 2.0;

                size += tsize;
            }
        }

        return size;
    }

    private static int SortByBoundsFunc(Transform o1, Transform o2)
    {
        MeshFilter f1 = o1.GetComponent<MeshFilter>();

        if (f1 == null)
        {
            return 0;
        }

        MeshFilter f2 = o2.GetComponent<MeshFilter>();

        if (f2 == null)
        {
            return 0;
        }

        // Sort...
        /*if (render1.bounds.size.magnitude > render2.bounds.size.magnitude)
            return -1;

        if (render1.bounds.size.magnitude < render2.bounds.size.magnitude)
            return 1;*/

        Mesh m1 = f1.sharedMesh;
        Mesh m2 = f2.sharedMesh;

        double weight1 = GetMeshSize(m1) / (double)Mathf.Max((float)(m1.vertexCount / 100.0), 1.0f);
        double weight2 = GetMeshSize(m2) / (double)Mathf.Max((float)(m2.vertexCount / 100.0), 1.0f);

        if (weight1 > weight2)
            return -1;

        if (weight1 < weight2)
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
