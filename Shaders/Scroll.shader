Shader "Custom/Scroll"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Speed("Speed", Range(0,10)) = 1.0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType"="Fade" }
		LOD 100
		Cull Off
		ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Speed;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				half2 uv = i.uv;
				half t = (_Time.x - floor(_Time.x)) * _Speed;
				uv.y =  i.uv.y + t;
				
				half2 uv2 = i.uv;
				uv2.y =  i.uv.y + 0.5 + t;
				
				return max(tex2D(_MainTex, uv), tex2D(_MainTex, uv2));
			}
			ENDCG
		}
	}
}
