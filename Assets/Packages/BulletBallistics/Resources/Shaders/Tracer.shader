Shader "Hidden/Ballistics/Tracer" 
{
	SubShader 
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }

		Cull Off
		Lighting Off
		ZWrite Off
		Blend SrcAlpha One 

		Pass 
        {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata 
			{
				float4 vertex : POSITION;
				float4 direction : NORMAL;
			};

			struct v2f 
			{
				float4 vertex : SV_POSITION;
				float2 fade : TEXCOORD0;
			};

			half _TracerWidth;
			half3 _TracerColor;

			v2f vert (appdata v)
			{
				v2f o;

				float3 pos = v.vertex.xyz;
				float side = v.vertex.w;
				float3 next = pos + v.direction.xyz;

				float4 viewPos =  mul(UNITY_MATRIX_V, float4(pos, 1));
				float4 viewNext =  mul(UNITY_MATRIX_V, float4(next, 1));

				float3 right = normalize(cross(viewNext.xyz - viewPos.xyz, viewPos.xyz));

				o.vertex = mul(UNITY_MATRIX_P, viewPos + float4(right * side * _TracerWidth, 0));
				o.fade = float2(side, v.direction.w);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float f = i.fade.x * i.fade.x;
				float v = i.fade.y * 2 - 1;
				return float4(_TracerColor, (1.0f - min(1, v * v)) * (1.0f - f * f));
			}
			ENDCG 
		}
	}	
}
