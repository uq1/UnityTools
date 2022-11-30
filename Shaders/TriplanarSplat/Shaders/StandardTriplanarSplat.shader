// Standard shader with triplanar mapping
// https://github.com/keijiro/StandardTriplanar

Shader "Standard Triplanar (Splat)"
{
    Properties
    {
        _Color("", Color) = (1, 1, 1, 1)
        _MainTex("", 2D) = "white" {}

        _Glossiness("", Range(0, 1)) = 0.5
        [Gamma] _Metallic("", Range(0, 1)) = 0

        _BumpScale("", Float) = 1
        _BumpMap("", 2D) = "bump" {}

        _OcclusionStrength("", Range(0, 1)) = 1
        _OcclusionMap("", 2D) = "white" {}
        
        _Color1("", Color) = (0, 0, 0, 0)
        _Splat1("", 2D) = "white" {}
        
        _Color2("", Color) = (0, 0, 0, 0)
        _Splat2("", 2D) = "white" {}
        
        _Color3("", Color) = (0, 0, 0, 0)
        _Splat3("", 2D) = "white" {}
        
        _Color4("", Color) = (0, 0, 0, 0)
        _Splat4("", 2D) = "white" {}
        
        _SplatControl("", 2D) = "black" {}
        _SplatControlScale("", Range(0, 1000)) = 2

        _MapScale("", Range(0, 10)) = 0.25
        _SplatScale("", Range(0, 10)) = 0.4
        _SplatMix("", Range(0, 10)) = 5
        _SplatContrast("", Range(0, 10)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM

        #pragma surface surf Standard vertex:vert fullforwardshadows addshadow

        #pragma shader_feature _NORMALMAP
        #pragma shader_feature _OCCLUSIONMAP
        #pragma shader_feature _SPLAT1
        #pragma shader_feature _SPLAT2
        #pragma shader_feature _SPLAT3
        #pragma shader_feature _SPLAT4

        #pragma target 3.0

        half4 _Color;
        sampler2D _MainTex;

        half _Glossiness;
        half _Metallic;

        half _BumpScale;
        sampler2D _BumpMap;

        half _OcclusionStrength;
        sampler2D _OcclusionMap;
        
        half4 _Color1;
        sampler2D _Splat1;
        
        half4 _Color2;
        sampler2D _Splat2;
        
        half4 _Color3;
        sampler2D _Splat3;
        
        half4 _Color4;
        sampler2D _Splat4;
        
        sampler2D _SplatControl;
        half _SplatControlScale;

        half _MapScale;
        half _SplatScale;
        half _SplatMix;
        half _SplatContrast;

        struct Input
        {
            float3 localCoord;
            float3 localNormal;
        };

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            data.localCoord = v.vertex.xyz;
            data.localNormal = v.normal.xyz;
        }
        
        half4 AdjustContrastCurve(half4 color, half contrast)
        {
            return saturate(lerp(half4(0.5, 0.5, 0.5, 0.5), color, contrast));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Blending factor of triplanar mapping
            float3 bf = normalize(abs(IN.localNormal));
            bf /= dot(bf, (float3)1);

            // Triplanar mapping
            float2 tx = IN.localCoord.yz * _MapScale;
            float2 ty = IN.localCoord.zx * _MapScale;
            float2 tz = IN.localCoord.xy * _MapScale;

            // Base color
            half4 cx = tex2D(_MainTex, tx) * bf.x;
            half4 cy = tex2D(_MainTex, ty) * bf.y;
            half4 cz = tex2D(_MainTex, tz) * bf.z;
            half4 color = (cx + cy + cz) * _Color;
            o.Albedo = color.rgb;
            o.Alpha = color.a;

        #ifdef _NORMALMAP
            // Normal map
            half4 nx = tex2D(_BumpMap, tx) * bf.x;
            half4 ny = tex2D(_BumpMap, ty) * bf.y;
            half4 nz = tex2D(_BumpMap, tz) * bf.z;
            o.Normal = UnpackScaleNormal(nx + ny + nz, _BumpScale);
        #endif

        #ifdef _OCCLUSIONMAP
            // Occlusion map
            half ox = tex2D(_OcclusionMap, tx).g * bf.x;
            half oy = tex2D(_OcclusionMap, ty).g * bf.y;
            half oz = tex2D(_OcclusionMap, tz).g * bf.z;
            o.Occlusion = lerp((half4)1, ox + oy + oz, _OcclusionStrength);
        #endif
            
            tx = IN.localCoord.yz * 0.001 * _SplatControlScale;
            ty = IN.localCoord.zx * 0.001 * _SplatControlScale;
            tz = IN.localCoord.xy * 0.001 * _SplatControlScale;
            
            cx = tex2D(_SplatControl, tx) * bf.x;
            cy = tex2D(_SplatControl, ty) * bf.y;
            cz = tex2D(_SplatControl, tz) * bf.z;
            half4 splats = cx + cy + cz;
            
            tx = IN.localCoord.yz * _SplatScale;
            ty = IN.localCoord.zx * _SplatScale;
            tz = IN.localCoord.xy * _SplatScale;
            
            tx = tx.yx;
            ty = ty.yx;
            tz = tz.yx;
            
        #ifdef _SPLAT1
            if (splats.r * _Color1.a > 0.0)
            {
                half4 cx = tex2D(_Splat1, tx) * bf.x;
                half4 cy = tex2D(_Splat1, ty) * bf.y;
                half4 cz = tex2D(_Splat1, tz) * bf.z;
                half4 smap = (cx + cy + cz);
                smap = AdjustContrastCurve(smap, _SplatContrast);
                o.Albedo = lerp(o.Albedo.rgb, smap.rgb, clamp(splats.r * _Color1.a * smap.a * _SplatMix, 0.0, 1.0));
            }
        #endif
        #ifdef _SPLAT2
            if (splats.g * _Color2.a > 0.0)
            {
                half4 cx = tex2D(_Splat2, tx) * bf.x;
                half4 cy = tex2D(_Splat2, ty) * bf.y;
                half4 cz = tex2D(_Splat2, tz) * bf.z;
                half4 smap = (cx + cy + cz) * _Color2;
                smap = AdjustContrastCurve(smap, _SplatContrast);
                o.Albedo = lerp(o.Albedo.rgb, smap.rgb, clamp(splats.g * _Color2.a * smap.a * _SplatMix, 0.0, 1.0));
            }
        #endif
        #ifdef _SPLAT3
            if (splats.b * _Color3.a > 0.0)
            {
                half4 cx = tex2D(_Splat3, tx) * bf.x;
                half4 cy = tex2D(_Splat3, ty) * bf.y;
                half4 cz = tex2D(_Splat3, tz) * bf.z;
                half4 smap = (cx + cy + cz) * _Color3;
                smap = AdjustContrastCurve(smap, _SplatContrast);
                o.Albedo = lerp(o.Albedo.rgb, smap.rgb, clamp(splats.b * _Color3.a * smap.a * _SplatMix, 0.0, 1.0));
            }
        #endif
        #ifdef _SPLAT4
            if (splats.a * _Color4.a > 0.0)
            {
                half4 cx = tex2D(_Splat4, tx) * bf.x;
                half4 cy = tex2D(_Splat4, ty) * bf.y;
                half4 cz = tex2D(_Splat4, tz) * bf.z;
                half4 smap = (cx + cy + cz) * _Color4;
                smap = AdjustContrastCurve(smap, _SplatContrast);
                o.Albedo = lerp(o.Albedo.rgb, smap.rgb, clamp(splats.a * _Color4.a * smap.a * _SplatMix, 0.0, 1.0));
            }
        #endif
        
            // Debug...
            //o.Albedo.rgb = splats.rgb;
            //o.Albedo.rgb = lerp(o.Albedo.rgb, splats.rgb, splats.a * _SplatMix);

            // Misc parameters
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
    CustomEditor "StandardTriplanarSplatInspector"
}
