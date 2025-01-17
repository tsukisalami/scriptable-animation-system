// Made with Amplify Shader Editor v1.9.4.4
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Ultimate Scope Shaders/ASE/HolographicASE"
{
	Properties
	{
		[Toggle]_USE_TEXTURE_COLOR("Use Texture Color", Float) = 0
		[NoScaleOffset]_Reticle("Reticle", 2D) = "white" {}
		_Reticle_Color("Reticle Color", Color) = (1,1,1,1)
		_Reticle_Brightness("Reticle Brightness", Float) = 1
		_ReticleSize("Reticle Size", Float) = 1
		_ReticleTiling("Reticle Tiling", Vector) = (1,1,0,0)
		_ReticleOffset("Reticle Offset", Vector) = (0,0,0,0)
		_Glass_Tint("Glass Tint", Color) = (0,0.454902,1,0.3333333)
		[Toggle]_USE_OFFSET_NOISE("Use Offset Noise", Float) = 0
		_Offset_Noise_Distance("Offset Noise Distance", Float) = 0.3
		_Offset_Noise_Scroll_Speed("Offset Noise Scroll Speed", Float) = 0
		[Toggle]_BLURRETICLE("BlurReticle", Float) = 0
		_Blur_Distance("Blur Distance", Range( 0 , 3)) = 1
		_Blur_Range("Blur Range", Range( 0 , 3)) = 1
		_Blur_Samples("Blur Samples", Float) = 5
		[Toggle]_USERADIALNOISE("UseRadialNoise", Float) = 1
		_NumRadialSections("NumRadialSections", Float) = 75
		_RadialNoiseStrength("RadialNoiseStrength", Range( 0 , 1)) = 0.55
		_Radial_Noise_UV_Offset_Speed("Radial Noise UV Offset Speed", Float) = 0.7
		_Radial_Noise_UV_Offset_Distance("Radial Noise UV Offset Distance", Float) = 0.3
		_RadialNoiseRotationSpeed("RadialNoiseRotationSpeed", Float) = 0.1
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 viewDir;
			INTERNAL_DATA
		};

		uniform float4 _Glass_Tint;
		uniform float _USERADIALNOISE;
		uniform float _BLURRETICLE;
		uniform sampler2D _Reticle;
		uniform float _USE_OFFSET_NOISE;
		uniform float _ReticleSize;
		uniform float2 _ReticleTiling;
		uniform float2 _ReticleOffset;
		uniform float _Offset_Noise_Scroll_Speed;
		uniform float _Offset_Noise_Distance;
		SamplerState sampler_Reticle;
		uniform float _Blur_Samples;
		uniform float _Blur_Distance;
		uniform float _Blur_Range;
		uniform float _NumRadialSections;
		uniform float _Radial_Noise_UV_Offset_Speed;
		uniform float _Radial_Noise_UV_Offset_Distance;
		uniform float _RadialNoiseRotationSpeed;
		uniform float _RadialNoiseStrength;
		uniform float _USE_TEXTURE_COLOR;
		uniform float4 _Reticle_Color;
		uniform float _Reticle_Brightness;


		float2 UnityGradientNoiseDir( float2 p )
		{
			p = fmod(p , 289);
			float x = fmod((34 * p.x + 1) * p.x , 289) + p.y;
			x = fmod( (34 * x + 1) * x , 289);
			x = frac( x / 41 ) * 2 - 1;
			return normalize( float2(x - floor(x + 0.5 ), abs( x ) - 0.5 ) );
		}
		
		float UnityGradientNoise( float2 UV, float Scale )
		{
			float2 p = UV * Scale;
			float2 ip = floor( p );
			float2 fp = frac( p );
			float d00 = dot( UnityGradientNoiseDir( ip ), fp );
			float d01 = dot( UnityGradientNoiseDir( ip + float2( 0, 1 ) ), fp - float2( 0, 1 ) );
			float d10 = dot( UnityGradientNoiseDir( ip + float2( 1, 0 ) ), fp - float2( 1, 0 ) );
			float d11 = dot( UnityGradientNoiseDir( ip + float2( 1, 1 ) ), fp - float2( 1, 1 ) );
			fp = fp * fp * fp * ( fp * ( fp * 6 - 15 ) + 10 );
			return lerp( lerp( d00, d01, fp.y ), lerp( d10, d11, fp.y ), fp.x ) + 0.5;
		}


		inline float noise_randomValue (float2 uv) { return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453); }

		inline float noise_interpolate (float a, float b, float t) { return (1.0-t)*a + (t*b); }

		inline float valueNoise (float2 uv)
		{
			float2 i = floor(uv);
			float2 f = frac( uv );
			f = f* f * (3.0 - 2.0 * f);
			uv = abs( frac(uv) - 0.5);
			float2 c0 = i + float2( 0.0, 0.0 );
			float2 c1 = i + float2( 1.0, 0.0 );
			float2 c2 = i + float2( 0.0, 1.0 );
			float2 c3 = i + float2( 1.0, 1.0 );
			float r0 = noise_randomValue( c0 );
			float r1 = noise_randomValue( c1 );
			float r2 = noise_randomValue( c2 );
			float r3 = noise_randomValue( c3 );
			float bottomOfGrid = noise_interpolate( r0, r1, f.x );
			float topOfGrid = noise_interpolate( r2, r3, f.x );
			float t = noise_interpolate( bottomOfGrid, topOfGrid, f.y );
			return t;
		}


		float SimpleNoise(float2 UV)
		{
			float t = 0.0;
			float freq = pow( 2.0, float( 0 ) );
			float amp = pow( 0.5, float( 3 - 0 ) );
			t += valueNoise( UV/freq )*amp;
			freq = pow(2.0, float(1));
			amp = pow(0.5, float(3-1));
			t += valueNoise( UV/freq )*amp;
			freq = pow(2.0, float(2));
			amp = pow(0.5, float(3-2));
			t += valueNoise( UV/freq )*amp;
			return t;
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			float3 normalizeResult3 = normalize( i.viewDir );
			float retSize23 = _ReticleSize;
			#if ( SHADER_TARGET >= 50 )
			float recip8 = rcp( ( retSize23 * 0.25 ) );
			#else
			float recip8 = 1.0 / ( retSize23 * 0.25 );
			#endif
			#if ( SHADER_TARGET >= 50 )
			float2 recip13 = rcp( _ReticleTiling );
			#else
			float2 recip13 = 1.0 / _ReticleTiling;
			#endif
			float3 temp_output_11_0 = (( ( -normalizeResult3 * recip8 ) + float3( float2( 0.5,0.5 ) ,  0.0 ) )*float3( recip13 ,  0.0 ) + float3( ( _ReticleOffset + ( ( recip13 * float2( -0.5,-0.5 ) ) + float2( 0.5,0.5 ) ) ) ,  0.0 ));
			float3 temp_output_131_0 = ( ( i.viewDir * float3( 5,5,5 ) ) + ( _Time.y * _Offset_Noise_Scroll_Speed ) );
			float gradientNoise138 = UnityGradientNoise(( float3( 5,5,5 ) + temp_output_131_0 ).xy,25.0);
			gradientNoise138 = gradientNoise138*0.5 + 0.5;
			float gradientNoise136 = UnityGradientNoise(temp_output_131_0.xy,25.0);
			gradientNoise136 = gradientNoise136*0.5 + 0.5;
			float2 appendResult140 = (float2(gradientNoise138 , gradientNoise136));
			#if ( SHADER_TARGET >= 50 )
			float recip146 = rcp( retSize23 );
			#else
			float recip146 = 1.0 / retSize23;
			#endif
			float3 TexUV32 = (( _USE_OFFSET_NOISE )?( ( float3( ( (float2( -1,-1 ) + (appendResult140 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) * ( ( _Offset_Noise_Distance * recip146 ) * 0.01 ) ) ,  0.0 ) + temp_output_11_0 ) ):( temp_output_11_0 ));
			float localSampleBlur28 = ( 0.0 );
			sampler2D Texture28 = _Reticle;
			SamplerState Sampler28 = sampler_Reticle;
			float2 UV28 = TexUV32.xy;
			float Samples28 = max( _Blur_Samples , 1.0 );
			float temp_output_26_0 = ( retSize23 * 100.0 );
			#if ( SHADER_TARGET >= 50 )
			float recip27 = rcp( temp_output_26_0 );
			#else
			float recip27 = 1.0 / temp_output_26_0;
			#endif
			float BlurDist28 = ( _Blur_Distance * recip27 );
			float BlurRange28 = ( recip27 * _Blur_Range );
			float4 RGBA28 = float4( 0,0,0,0 );
			{
			Samples28 = round(Samples28);
			float2 uv = UV28;
			float samplePeriod = (2 * 3.14159) / Samples28;
			for (int i = 0; i < Samples28; i++) {
				sincos(samplePeriod * i, uv.x, uv.y);
				float randomno =  frac(sin(i * 735.234));
				float blurAdd = randomno * BlurRange28;
				RGBA28 +=tex2D(Texture28, UV28 + uv * (BlurDist28 + blurAdd));
			}
			RGBA28 /= Samples28;
			}
			float3 temp_output_74_0 = ( i.viewDir * float3( -1,-1,0 ) );
			float3 temp_output_111_0 = ( ( _Time.y * ( 0.1 * _Radial_Noise_UV_Offset_Speed ) ) + temp_output_74_0 );
			float simpleNoise120 = SimpleNoise( ( ( -1.0 * 0.3 ) + temp_output_111_0 ).xy*750.0 );
			float simpleNoise118 = SimpleNoise( ( 0.3 + temp_output_111_0 ).xy*750.0 );
			float2 appendResult123 = (float2(simpleNoise120 , simpleNoise118));
			float cos78 = cos( ( _Time.y * _RadialNoiseRotationSpeed ) );
			float sin78 = sin( ( _Time.y * _RadialNoiseRotationSpeed ) );
			float2 rotator78 = mul( ( ( float3( ( (float2( -1,-1 ) + (appendResult123 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) * ( _Radial_Noise_UV_Offset_Distance * 0.01 ) ) ,  0.0 ) + temp_output_74_0 ) * float3( -1,1,0 ) ).xy - float2( 0,0 ) , float2x2( cos78 , -sin78 , sin78 , cos78 )) + float2( 0,0 );
			float cos83 = cos( radians( ( ( 360.0 / ceil( _NumRadialSections ) ) / -2.0 ) ) );
			float sin83 = sin( radians( ( ( 360.0 / ceil( _NumRadialSections ) ) / -2.0 ) ) );
			float2 rotator83 = mul( rotator78 - float2( 0,0 ) , float2x2( cos83 , -sin83 , sin83 , cos83 )) + float2( 0,0 );
			float2 break91 = rotator83;
			float2 temp_cast_15 = (round( ( 1.5 + ( _NumRadialSections * (0.0 + (atan2( break91.x , break91.y ) - -UNITY_PI) * (1.0 - 0.0) / (UNITY_PI - -UNITY_PI)) ) ) )).xx;
			float dotResult4_g2 = dot( temp_cast_15 , float2( 12.9898,78.233 ) );
			float lerpResult10_g2 = lerp( 0.0 , 1.0 , frac( ( sin( dotResult4_g2 ) * 43758.55 ) ));
			float2 break82 = rotator78;
			float temp_output_97_0 = ( ( (0.0 + (atan2( break82.x , break82.y ) - -UNITY_PI) * (1.0 - 0.0) / (UNITY_PI - -UNITY_PI)) * _NumRadialSections ) + 1.5 );
			float2 temp_cast_16 = (round( temp_output_97_0 )).xx;
			float dotResult4_g1 = dot( temp_cast_16 , float2( 12.9898,78.233 ) );
			float lerpResult10_g1 = lerp( 0.0 , 1.0 , frac( ( sin( dotResult4_g1 ) * 43758.55 ) ));
			float lerpResult67 = lerp( lerpResult10_g2 , lerpResult10_g1 , (0.0 + (cos( ( 6.28318548202515 * temp_output_97_0 ) ) - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)));
			float4 temp_output_45_0 = ( _Glass_Tint * ( 1.0 - (( _USERADIALNOISE )?( ( (( _BLURRETICLE )?( RGBA28 ):( tex2D( _Reticle, TexUV32.xy ) )) * (( 1.0 - _RadialNoiseStrength ) + (lerpResult67 - 0.0) * (1.0 - ( 1.0 - _RadialNoiseStrength )) / (1.0 - 0.0)) ) ):( (( _BLURRETICLE )?( RGBA28 ):( tex2D( _Reticle, TexUV32.xy ) )) )).w ) );
			float4 lerpResult47 = lerp( float4( 0,0,0,0 ) , temp_output_45_0 , temp_output_45_0.a);
			float retBrightness63 = _Reticle_Brightness;
			float4 temp_output_55_0 = ( _Reticle_Color + ( float4(0.2,0.2,0.2,0) * saturate( ( retBrightness63 - 1.0 ) ) ) );
			float4 temp_output_48_0 = ( lerpResult47 + (( _USE_TEXTURE_COLOR )?( ( (( _USERADIALNOISE )?( ( (( _BLURRETICLE )?( RGBA28 ):( tex2D( _Reticle, TexUV32.xy ) )) * (( 1.0 - _RadialNoiseStrength ) + (lerpResult67 - 0.0) * (1.0 - ( 1.0 - _RadialNoiseStrength )) / (1.0 - 0.0)) ) ):( (( _BLURRETICLE )?( RGBA28 ):( tex2D( _Reticle, TexUV32.xy ) )) )) * temp_output_55_0 ) ):( ( (( _USERADIALNOISE )?( ( (( _BLURRETICLE )?( RGBA28 ):( tex2D( _Reticle, TexUV32.xy ) )) * (( 1.0 - _RadialNoiseStrength ) + (lerpResult67 - 0.0) * (1.0 - ( 1.0 - _RadialNoiseStrength )) / (1.0 - 0.0)) ) ):( (( _BLURRETICLE )?( RGBA28 ):( tex2D( _Reticle, TexUV32.xy ) )) )).w * temp_output_55_0 ) )) );
			o.Albedo = temp_output_48_0.rgb;
			o.Emission = ( (( _USE_TEXTURE_COLOR )?( ( (( _USERADIALNOISE )?( ( (( _BLURRETICLE )?( RGBA28 ):( tex2D( _Reticle, TexUV32.xy ) )) * (( 1.0 - _RadialNoiseStrength ) + (lerpResult67 - 0.0) * (1.0 - ( 1.0 - _RadialNoiseStrength )) / (1.0 - 0.0)) ) ):( (( _BLURRETICLE )?( RGBA28 ):( tex2D( _Reticle, TexUV32.xy ) )) )) * temp_output_55_0 ) ):( ( (( _USERADIALNOISE )?( ( (( _BLURRETICLE )?( RGBA28 ):( tex2D( _Reticle, TexUV32.xy ) )) * (( 1.0 - _RadialNoiseStrength ) + (lerpResult67 - 0.0) * (1.0 - ( 1.0 - _RadialNoiseStrength )) / (1.0 - 0.0)) ) ):( (( _BLURRETICLE )?( RGBA28 ):( tex2D( _Reticle, TexUV32.xy ) )) )).w * temp_output_55_0 ) )) * retBrightness63 ).xyz;
			float temp_output_40_0 = 0.0;
			o.Metallic = temp_output_40_0;
			o.Smoothness = temp_output_40_0;
			o.Occlusion = 1.0;
			o.Alpha = temp_output_48_0.a;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard alpha:fade keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float4 tSpace0 : TEXCOORD1;
				float4 tSpace1 : TEXCOORD2;
				float4 tSpace2 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = IN.tSpace0.xyz * worldViewDir.x + IN.tSpace1.xyz * worldViewDir.y + IN.tSpace2.xyz * worldViewDir.z;
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19404
Node;AmplifyShaderEditor.RangedFloatNode;115;-7904,1024;Inherit;False;Property;_Radial_Noise_UV_Offset_Speed;Radial Noise UV Offset Speed;18;0;Create;False;0;0;0;False;0;False;0.7;0.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;112;-7648,832;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;114;-7600,976;Inherit;False;2;2;0;FLOAT;0.1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;73;-7840,1408;Inherit;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;74;-7584,1408;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;-1,-1,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;113;-7392,912;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;121;-7344,720;Inherit;False;Constant;_Float4;Float 4;17;0;Create;True;0;0;0;False;0;False;0.3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;111;-7216,1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;122;-7120,688;Inherit;False;2;2;0;FLOAT;-1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;117;-6944,880;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;116;-6944,1056;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;119;-6928,976;Inherit;False;Constant;_Float3;Float 3;17;0;Create;True;0;0;0;False;0;False;750;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;120;-6736,880;Inherit;False;Simple;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;118;-6736,1056;Inherit;False;Simple;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;123;-6464,928;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;127;-6576,1232;Inherit;False;Property;_Radial_Noise_UV_Offset_Distance;Radial Noise UV Offset Distance;19;0;Create;False;0;0;0;False;0;False;0.3;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;84;-6016,2144;Inherit;False;Property;_NumRadialSections;NumRadialSections;16;0;Create;True;0;0;0;False;0;False;75;75;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;124;-6256,976;Inherit;False;5;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;1,1;False;3;FLOAT2;-1,-1;False;4;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;126;-6208,1248;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.CeilOpNode;85;-5664,2304;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;125;-6064,1200;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;6;0,-96;Inherit;False;Property;_ReticleSize;Reticle Size;4;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;2;-3168,-1168;Inherit;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleTimeNode;132;-2944,-1504;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;134;-2976,-1424;Inherit;False;Property;_Offset_Noise_Scroll_Speed;Offset Noise Scroll Speed;10;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;75;-5872,1424;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleTimeNode;79;-5856,1664;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;86;-5440,2256;Inherit;False;2;0;FLOAT;360;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;81;-6048,1792;Inherit;False;Property;_RadialNoiseRotationSpeed;RadialNoiseRotationSpeed;20;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;23;176,-96;Inherit;False;retSize;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;130;-2928,-1648;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;5,5,5;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;133;-2736,-1504;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;76;-5744,1488;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;-1,1,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;80;-5616,1728;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;87;-5328,2480;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;-2;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;12;-3344,-752;Inherit;False;Property;_ReticleTiling;Reticle Tiling;5;0;Create;True;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;24;-3232,-944;Inherit;False;23;retSize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;131;-2576,-1648;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RotatorNode;78;-5456,1536;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RadiansOpNode;110;-5264,2336;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;3;-2976,-1168;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-3008,-944;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.25;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;15;-3104,-544;Inherit;False;Constant;_Vector1;Vector 0;2;0;Create;True;0;0;0;False;0;False;-0.5,-0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ReciprocalOpNode;13;-3104,-688;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;135;-2432,-1712;Inherit;False;2;2;0;FLOAT3;5,5,5;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;137;-2368,-1600;Inherit;False;Constant;_Float5;Float 5;20;0;Create;True;0;0;0;False;0;False;25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;145;-2496,-1248;Inherit;False;23;retSize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;82;-4992,1536;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.PiNode;99;-5184,1760;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;83;-5200,2176;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NegateNode;4;-2800,-1168;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-2848,-544;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;17;-2976,-384;Inherit;False;Constant;_Vector2;Vector 0;2;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ReciprocalOpNode;8;-2864,-944;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;136;-2208,-1488;Inherit;False;Gradient;True;True;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;138;-2208,-1712;Inherit;False;Gradient;True;True;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;142;-2432,-1344;Inherit;False;Property;_Offset_Noise_Distance;Offset Noise Distance;9;0;Create;False;0;0;0;False;0;False;0.3;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ReciprocalOpNode;146;-2288,-1248;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;91;-4992,2176;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.ATan2OpNode;92;-4864,1536;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;100;-4944,1760;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;-2624,-1136;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector2Node;10;-2608,-944;Inherit;False;Constant;_Vector0;Vector 0;2;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;16;-2672,-464;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;20;-2576,-624;Inherit;False;Property;_ReticleOffset;Reticle Offset;6;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DynamicAppendNode;140;-1968,-1584;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;143;-2144,-1344;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ATan2OpNode;93;-4864,2176;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;94;-4688,1536;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;9;-2400,-1008;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;21;-2368,-528;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;139;-1744,-1664;Inherit;False;5;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;1,1;False;3;FLOAT2;-1,-1;False;4;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;144;-2000,-1296;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;95;-4656,2176;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;98;-4400,1872;Inherit;False;Constant;_Float2;Float 2;16;0;Create;True;0;0;0;False;0;False;1.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;89;-4432,1680;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;11;-2224,-816;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;1,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;141;-1696,-1344;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;25;-4144,240;Inherit;False;23;retSize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;90;-4416,2032;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;97;-4176,1744;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TauNode;101;-4032,1408;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;128;-1568,-960;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-3952,240;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;96;-4192,2016;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;102;-3888,1488;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;129;-1376,-752;Inherit;False;Property;_USE_OFFSET_NOISE;Use Offset Noise;8;0;Create;False;0;0;0;False;0;False;0;True;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;30;-3600,32;Inherit;False;Property;_Blur_Samples;Blur Samples;14;0;Create;False;0;0;0;False;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;36;-3712,128;Inherit;False;Property;_Blur_Distance;Blur Distance;12;0;Create;False;0;0;0;False;0;False;1;5;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-3728,352;Inherit;False;Property;_Blur_Range;Blur Range;13;0;Create;False;0;0;0;False;0;False;1;3;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.RoundOpNode;105;-3984,1744;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RoundOpNode;106;-4000,2016;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;103;-3664,1552;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ReciprocalOpNode;27;-3600,208;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;32;-1120,-752;Inherit;False;TexUV;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;33;-3424,400;Inherit;False;32;TexUV;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-3440,208;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;35;-3440,304;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;31;-3408,48;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;29;-3440,544;Inherit;True;Property;_Reticle;Reticle;1;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.FunctionNode;108;-3744,2016;Inherit;False;Random Range;-1;;2;7b754edb8aebbfb4a9ace907af661cfc;0;3;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;104;-3472,1552;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;68;-3312,2160;Inherit;False;Property;_RadialNoiseStrength;RadialNoiseStrength;17;0;Create;True;0;0;0;False;0;False;0.55;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;107;-3744,1744;Inherit;False;Random Range;-1;;1;7b754edb8aebbfb4a9ace907af661cfc;0;3;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;59;0,-192;Inherit;False;Property;_Reticle_Brightness;Reticle Brightness;3;0;Create;False;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-3136,480;Inherit;True;Property;_NonBlur;NonBlur;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CustomExpressionNode;28;-3152,192;Inherit;False;Samples = round(Samples)@$float2 uv = UV@$float samplePeriod = (2 * 3.14159) / Samples@$for (int i = 0@ i < Samples@ i++) {$	sincos(samplePeriod * i, uv.x, uv.y)@$	float randomno =  frac(sin(i * 735.234))@$	float blurAdd = randomno * BlurRange@$	RGBA +=tex2D(Texture, UV + uv * (BlurDist + blurAdd))@$}$RGBA /= Samples@;7;Create;7;True;Texture;SAMPLER2D;;In;;Inherit;False;True;Sampler;SAMPLERSTATE;;In;;Inherit;False;False;UV;FLOAT2;0,0;In;;Inherit;False;False;Samples;FLOAT;0;In;;Inherit;False;False;BlurDist;FLOAT;0;In;;Inherit;False;False;BlurRange;FLOAT;0;In;;Inherit;False;True;RGBA;FLOAT4;0,0,0,0;Out;;Inherit;False;SampleBlur;True;False;0;;False;8;0;FLOAT;0;False;1;SAMPLER2D;;False;2;SAMPLERSTATE;;False;3;FLOAT2;0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT4;0,0,0,0;False;2;FLOAT;0;FLOAT4;8
Node;AmplifyShaderEditor.LerpOp;67;-3072,1920;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;69;-3040,2160;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;63;208,-192;Inherit;False;retBrightness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;39;-2672,496;Inherit;False;Property;_BLURRETICLE;BlurReticle;11;0;Create;False;0;0;0;False;0;False;0;True;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TFHCRemapNode;66;-2832,1952;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;72;-2400,688;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;64;-2224,1104;Inherit;False;63;retBrightness;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;60;-1952,1072;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;71;-2096,560;Inherit;False;Property;_USERADIALNOISE;UseRadialNoise;15;0;Create;False;0;0;0;False;0;False;1;True;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.BreakToComponentsNode;42;-1648,288;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.Vector4Node;58;-1984,848;Inherit;False;Constant;_Vector3;Vector 3;12;0;Create;True;0;0;0;False;0;False;0.2,0.2,0.2,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;61;-1808,1072;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;43;-1488,144;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;44;-1552,-32;Inherit;False;Property;_Glass_Tint;Glass Tint;7;0;Create;False;0;0;0;False;0;False;0,0.454902,1,0.3333333;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;56;-1776,672;Inherit;False;Property;_Reticle_Color;Reticle Color;2;0;Create;False;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;57;-1680,864;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;-1248,48;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;55;-1504,704;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;46;-1072,112;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;53;-1337.343,515.3156;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;-1328,624;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;47;-912,16;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ToggleSwitchNode;51;-1024,496;Inherit;False;Property;_USE_TEXTURE_COLOR;Use Texture Color;0;0;Create;False;0;0;0;False;0;False;0;True;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;65;-688,720;Inherit;False;63;retBrightness;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;48;-674.7009,281.4955;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;22;-3104,-784;Inherit;False;2;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;38;-3680,256;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-176,112;Inherit;False;Constant;_Float0;Float 0;8;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-176,192;Inherit;False;Constant;_Float1;Float 0;8;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;-474.8788,635.0451;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.BreakToComponentsNode;52;-496,352;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Ultimate Scope Shaders/ASE/HolographicASE;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;114;1;115;0
WireConnection;74;0;73;0
WireConnection;113;0;112;0
WireConnection;113;1;114;0
WireConnection;111;0;113;0
WireConnection;111;1;74;0
WireConnection;122;1;121;0
WireConnection;117;0;122;0
WireConnection;117;1;111;0
WireConnection;116;0;121;0
WireConnection;116;1;111;0
WireConnection;120;0;117;0
WireConnection;120;1;119;0
WireConnection;118;0;116;0
WireConnection;118;1;119;0
WireConnection;123;0;120;0
WireConnection;123;1;118;0
WireConnection;124;0;123;0
WireConnection;126;0;127;0
WireConnection;85;0;84;0
WireConnection;125;0;124;0
WireConnection;125;1;126;0
WireConnection;75;0;125;0
WireConnection;75;1;74;0
WireConnection;86;1;85;0
WireConnection;23;0;6;0
WireConnection;130;0;2;0
WireConnection;133;0;132;0
WireConnection;133;1;134;0
WireConnection;76;0;75;0
WireConnection;80;0;79;0
WireConnection;80;1;81;0
WireConnection;87;0;86;0
WireConnection;131;0;130;0
WireConnection;131;1;133;0
WireConnection;78;0;76;0
WireConnection;78;2;80;0
WireConnection;110;0;87;0
WireConnection;3;0;2;0
WireConnection;7;0;24;0
WireConnection;13;0;12;0
WireConnection;135;1;131;0
WireConnection;82;0;78;0
WireConnection;83;0;78;0
WireConnection;83;2;110;0
WireConnection;4;0;3;0
WireConnection;14;0;13;0
WireConnection;14;1;15;0
WireConnection;8;0;7;0
WireConnection;136;0;131;0
WireConnection;136;1;137;0
WireConnection;138;0;135;0
WireConnection;138;1;137;0
WireConnection;146;0;145;0
WireConnection;91;0;83;0
WireConnection;92;0;82;0
WireConnection;92;1;82;1
WireConnection;100;0;99;0
WireConnection;5;0;4;0
WireConnection;5;1;8;0
WireConnection;16;0;14;0
WireConnection;16;1;17;0
WireConnection;140;0;138;0
WireConnection;140;1;136;0
WireConnection;143;0;142;0
WireConnection;143;1;146;0
WireConnection;93;0;91;0
WireConnection;93;1;91;1
WireConnection;94;0;92;0
WireConnection;94;1;100;0
WireConnection;94;2;99;0
WireConnection;9;0;5;0
WireConnection;9;1;10;0
WireConnection;21;0;20;0
WireConnection;21;1;16;0
WireConnection;139;0;140;0
WireConnection;144;0;143;0
WireConnection;95;0;93;0
WireConnection;95;1;100;0
WireConnection;95;2;99;0
WireConnection;89;0;94;0
WireConnection;89;1;84;0
WireConnection;11;0;9;0
WireConnection;11;1;13;0
WireConnection;11;2;21;0
WireConnection;141;0;139;0
WireConnection;141;1;144;0
WireConnection;90;0;84;0
WireConnection;90;1;95;0
WireConnection;97;0;89;0
WireConnection;97;1;98;0
WireConnection;128;0;141;0
WireConnection;128;1;11;0
WireConnection;26;0;25;0
WireConnection;96;0;98;0
WireConnection;96;1;90;0
WireConnection;102;0;101;0
WireConnection;102;1;97;0
WireConnection;129;0;11;0
WireConnection;129;1;128;0
WireConnection;105;0;97;0
WireConnection;106;0;96;0
WireConnection;103;0;102;0
WireConnection;27;0;26;0
WireConnection;32;0;129;0
WireConnection;34;0;36;0
WireConnection;34;1;27;0
WireConnection;35;0;27;0
WireConnection;35;1;37;0
WireConnection;31;0;30;0
WireConnection;108;1;106;0
WireConnection;104;0;103;0
WireConnection;107;1;105;0
WireConnection;1;0;29;0
WireConnection;1;1;33;0
WireConnection;1;7;29;1
WireConnection;28;1;29;0
WireConnection;28;2;29;1
WireConnection;28;3;33;0
WireConnection;28;4;31;0
WireConnection;28;5;34;0
WireConnection;28;6;35;0
WireConnection;67;0;108;0
WireConnection;67;1;107;0
WireConnection;67;2;104;0
WireConnection;69;0;68;0
WireConnection;63;0;59;0
WireConnection;39;0;1;0
WireConnection;39;1;28;8
WireConnection;66;0;67;0
WireConnection;66;3;69;0
WireConnection;72;0;39;0
WireConnection;72;1;66;0
WireConnection;60;0;64;0
WireConnection;71;0;39;0
WireConnection;71;1;72;0
WireConnection;42;0;71;0
WireConnection;61;0;60;0
WireConnection;43;0;42;3
WireConnection;57;0;58;0
WireConnection;57;1;61;0
WireConnection;45;0;44;0
WireConnection;45;1;43;0
WireConnection;55;0;56;0
WireConnection;55;1;57;0
WireConnection;46;0;45;0
WireConnection;53;0;71;0
WireConnection;53;1;55;0
WireConnection;54;0;42;3
WireConnection;54;1;55;0
WireConnection;47;1;45;0
WireConnection;47;2;46;3
WireConnection;51;0;54;0
WireConnection;51;1;53;0
WireConnection;48;0;47;0
WireConnection;48;1;51;0
WireConnection;22;1;12;0
WireConnection;38;1;26;0
WireConnection;62;0;51;0
WireConnection;62;1;65;0
WireConnection;52;0;48;0
WireConnection;0;0;48;0
WireConnection;0;2;62;0
WireConnection;0;3;40;0
WireConnection;0;4;40;0
WireConnection;0;5;41;0
WireConnection;0;9;52;3
ASEEND*/
//CHKSM=0DDDDB2E98085D8AA3B592B7D5B968E2FD050547