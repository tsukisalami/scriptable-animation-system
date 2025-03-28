Shader "Custom/HologramPreview"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.5, 0.8, 1, 0.5)
        _RimColor ("Rim Color", Color) = (0.5, 0.8, 1, 0.0)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        
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
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _RimColor;
            float _RimPower;
            float _PulseSpeed;
            float _PulseAmount;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Base texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Grid pattern
                float2 grid = frac(i.uv * 10) - 0.5;
                float gridMask = 1.0 - saturate(abs(grid.x) * 20) * saturate(abs(grid.y) * 20);
                col.rgb += gridMask * 0.1;
                
                // Rim effect
                float rim = 1.0 - saturate(dot(i.viewDir, i.worldNormal));
                col.rgb += _RimColor.rgb * pow(rim, _RimPower);
                
                // Pulse effect
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmount + 1.0;
                col.a *= pulse;
                
                // Apply scan line
                float scanLine = frac(i.vertex.y * 0.1 + _Time.y * 0.5);
                col.rgb += step(0.98, scanLine) * 0.2;
                
                return col;
            }
            ENDCG
        }
    }
} 