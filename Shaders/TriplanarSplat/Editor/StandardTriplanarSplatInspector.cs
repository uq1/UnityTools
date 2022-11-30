// Standard shader with triplanar mapping
// https://github.com/keijiro/StandardTriplanar

using UnityEngine;
using UnityEditor;

public class StandardTriplanarSplatInspector : ShaderGUI
{
    static class Styles
    {
        static public readonly GUIContent albedo = new GUIContent("Albedo", "Albedo (RGB)");
        static public readonly GUIContent normalMap = new GUIContent("Normal Map", "Normal Map");
        static public readonly GUIContent occlusion = new GUIContent("Occlusion", "Occlusion (G)");
        static public readonly GUIContent splat1 = new GUIContent("Splat 1", "Albedo (RGB)");
        static public readonly GUIContent splat2 = new GUIContent("Splat 2", "Albedo (RGB)");
        static public readonly GUIContent splat3 = new GUIContent("Splat 3", "Albedo (RGB)");
        static public readonly GUIContent splat4 = new GUIContent("Splat 4", "Albedo (RGB)");
        static public readonly GUIContent splatControl = new GUIContent("Splat Control Map", "Control Scale");
    }

    bool _initialized;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        EditorGUI.BeginChangeCheck();

        editor.TexturePropertySingleLine(
            Styles.albedo, FindProperty("_MainTex", props), FindProperty("_Color", props)
        );

        editor.ShaderProperty(FindProperty("_Metallic", props), "Metallic");
        editor.ShaderProperty(FindProperty("_Glossiness", props), "Smoothness");

        var normal = FindProperty("_BumpMap", props);
        editor.TexturePropertySingleLine(
            Styles.normalMap, normal,
            normal.textureValue ? FindProperty("_BumpScale", props) : null
        );

        var occ = FindProperty("_OcclusionMap", props);
        editor.TexturePropertySingleLine(
            Styles.occlusion, occ,
            occ.textureValue ? FindProperty("_OcclusionStrength", props) : null
        );
        
        editor.TexturePropertySingleLine(
            Styles.splat1, FindProperty("_Splat1", props), FindProperty("_Color1", props)
        );
        
        editor.TexturePropertySingleLine(
            Styles.splat2, FindProperty("_Splat2", props), FindProperty("_Color2", props)
        );
        
        editor.TexturePropertySingleLine(
            Styles.splat3, FindProperty("_Splat3", props), FindProperty("_Color3", props)
        );
        
        editor.TexturePropertySingleLine(
            Styles.splat4, FindProperty("_Splat4", props), FindProperty("_Color4", props)
        );
        
        editor.TexturePropertySingleLine(
            Styles.splatControl, FindProperty("_SplatControl", props), FindProperty("_SplatControlScale", props)
        );
        
        //editor.ShaderProperty(FindProperty("_SplatControl", props), "Splat Control Map");
        
        

        editor.ShaderProperty(FindProperty("_MapScale", props), "Texture Scale");
        
        editor.ShaderProperty(FindProperty("_SplatScale", props), "Splat Scale");
        editor.ShaderProperty(FindProperty("_SplatMix", props), "Splat Mix");
        editor.ShaderProperty(FindProperty("_SplatContrast", props), "Splat Contrast");

        if (EditorGUI.EndChangeCheck() || !_initialized)
            foreach (Material m in editor.targets)
                SetMaterialKeywords(m);

        _initialized = true;
    }

    static void SetMaterialKeywords(Material material)
    {
        SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
        SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
        SetKeyword(material, "_SPLAT1", material.GetTexture("_Splat1"));
        SetKeyword(material, "_SPLAT2", material.GetTexture("_Splat2"));
        SetKeyword(material, "_SPLAT3", material.GetTexture("_Splat3"));
        SetKeyword(material, "_SPLAT4", material.GetTexture("_Splat4"));
    }

    static void SetKeyword(Material m, string keyword, bool state)
    {
        if (state)
            m.EnableKeyword(keyword);
        else
            m.DisableKeyword(keyword);
    }
}
