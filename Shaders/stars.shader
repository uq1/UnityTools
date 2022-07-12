// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ShaderToy/stars"{
	Properties{
	}
	SubShader{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				half3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				half4 posWorld : TEXCOORD2;
				half3 normalDir : TEXCOORD3;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				//o.normalDir = mul(unity_ObjectToWorld, half4(v.normal, 0)).xyz;
				o.normalDir = v.normal;
				return o;
			}
			
			const float tau = 6.28318530717958647692;

			float hash( const in float n ) {
				return frac(sin(n)*43758.5453123);
			}

			float noise( const in  float3 x ) {
				float3 p = floor(x);
				float3 f = frac(x);
				f = f*f*(3.0-2.0*f);
				float n = p.x + p.y*157.0 + 113.0*p.z;
				return lerp(lerp(lerp( hash(n+  0.0), hash(n+  1.0),f.x),
							   lerp( hash(n+157.0), hash(n+158.0),f.x),f.y),
						   lerp(lerp( hash(n+113.0), hash(n+114.0),f.x),
							   lerp( hash(n+270.0), hash(n+271.0),f.x),f.y),f.z);
			}

			float4 Rand( in int x )
			{
				float2 uv;
				uv.x = (float(x)+0.5)/256.0;
				uv.y = (floor(uv.x)+0.5)/256.0;
				//return texture( iChannel0, uv, -100.0 );
				return float4(hash(uv.x), hash(uv.y), hash(uv.x), hash(uv.y));
			}
			
			fixed4 frag(v2f i) : SV_Target
			{
				/*float3 ray;
				ray.xy = 2.0*(i.screenCoord.xy * _ScreenParams.xy-_ScreenParams.xy*.5)/_ScreenParams.x;
				ray.z = 1.0;*/

				//float3 ray = normalize(i.posWorld.xyz);
				float3 normals = i.normalDir.xyz;
				float3 ray = normalize(normals);//normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
				//ray.z = 0.5;
				ray.z = ray.z * 0.5 + 0.4;

				float offset = _Time.y * .1;
				float speed2 = 1.0;
				float speed = speed2;
				offset *= 2.0;


				float3 col = float3(0.0, 0.0, 0.0);

				float3 stp = ray / max(abs(ray.x),abs(ray.y));

				float3 pos = 4.0*stp + .5;

				for (int i = 0; i < 20; i++)
				{
					float z = noise(float3(pos.xy, pos.x + pos.y) * 2.0);
					z = frac(z - offset);
					float d = 50.0*z - pos.z;
					float w = pow(max(0.0,1.0 - 8.0*length(frac(pos.xy) - .5)),2.0);
					float3 c = max(float3(0.0, 0.0, 0.0), float3(1.0 - abs(d + speed2 * .5) / speed,1.0 - abs(d) / speed,1.0 - abs(d - speed2 * .5) / speed));
					col += 1.5*(1.0 - z)*c*w;
					pos += stp;
				}

				return fixed4(col, max(col.r, max(col.g, col.b)));
			}
			ENDCG
		}
	}
}
