Shader "Hidden/Ballistics/Line" 
{
	SubShader 
    {
        Tags { "RenderType"="Opaque" }
		Pass 
        {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float3 data : POSITION;
				half4 color : COLOR;
			};

			struct v2f {
				float4 position : SV_POSITION;
				float4 color : COLOR;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.position = UnityObjectToClipPos(float4(v.data, 1));
				o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return i.color;
			}
			ENDCG 
		}
	}	
}
