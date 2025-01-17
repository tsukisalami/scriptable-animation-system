// Made with Amplify Shader Editor v1.9.4.4
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Ultimate Scope Shaders/ASE/ScopeASE"
{
	Properties
	{
		[NoScaleOffset]_PIPInput("PIPInput", 2D) = "white" {}
		_DistortStrength("Distortion Strength", Float) = 0
		_ShaderZoom("ShaderZoom", Range( 0 , 10)) = 0
		_Scope_Depth("Inner Tube Depth", Range( 0 , 3)) = 0.3
		_FOV_Size("FOV Size", Range( 0 , 1)) = 0.75
		_FOVFadeDistance("FOVFadeDistance", Range( 0 , 0.05)) = 0.01
		[NoScaleOffset]_Reticle("Reticle", 2D) = "white" {}
		[Toggle]_USE_TEXTURE_COLOR("Use Texture Color", Float) = 1
		_CrosshairColor("Reticle Color", Color) = (1,1,1,1)
		_Reticle_Size("Reticle Size", Float) = 1
		_Reticle_Tiling("Reticle Tiling", Vector) = (1,1,0,0)
		_Reticle_Offset("Reticle Offset", Vector) = (0,0,0,0)
		_Reticle_Brightness("Reticle Brightness", Float) = 0
		[Toggle]_USESCOPESHADOW("UseScopeShadow", Float) = 1
		_EyeRelief("EyeRelief", Float) = 0.1
		_EyeReliefFalloff("EyeReliefFalloff", Float) = 4
		_ShadowSize("ShadowSize", Range( 0 , 1)) = 0.85
		_ShadowFadeDistance("ShadowFadeDistance", Range( 0 , 0.2)) = 0.1
		_EyeboxAccuracy("RequiredEyeboxAccuracy", Range( 0 , 3)) = 2
		_Reticle_Zoom("Reticle Zoom", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
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
			float2 uv_texcoord;
			float3 worldNormal;
		};

		uniform float _USESCOPESHADOW;
		uniform sampler2D _PIPInput;
		uniform float _Scope_Depth;
		uniform float _ShaderZoom;
		uniform float _DistortStrength;
		uniform float _FOVFadeDistance;
		uniform float _FOV_Size;
		uniform float _Reticle_Brightness;
		uniform float _USE_TEXTURE_COLOR;
		uniform float2 _Reticle_Tiling;
		uniform float _Reticle_Size;
		uniform float _Reticle_Zoom;
		uniform float2 _Reticle_Offset;
		uniform sampler2D _Reticle;
		uniform float4 _CrosshairColor;
		uniform float _ShadowFadeDistance;
		uniform float _EyeboxAccuracy;
		uniform float _ShadowSize;
		uniform float _EyeReliefFalloff;
		uniform float _EyeRelief;


		float3 ASESafeNormalize(float3 inVec)
		{
			float dp3 = max(1.175494351e-38, dot(inVec, inVec));
			return inVec* rsqrt(dp3);
		}


		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			o.Normal = float3(0,0,1);
			float2 projection60 = (((i.viewDir*float3( float2( -1,-1 ) ,  0.0 ) + float3( float2( 0.5,0.5 ) ,  0.0 ))*float3( float2( 1,1 ) ,  0.0 ) + ( -i.viewDir * _Scope_Depth ))).xy;
			float shaderZoom123 = _ShaderZoom;
			#if ( SHADER_TARGET >= 50 )
			float recip139 = rcp( shaderZoom123 );
			#else
			float recip139 = 1.0 / shaderZoom123;
			#endif
			float2 temp_output_2_0_g2 = i.uv_texcoord;
			float2 temp_output_11_0_g2 = ( temp_output_2_0_g2 - float2( 0.5,0.5 ) );
			float dotResult12_g2 = dot( temp_output_11_0_g2 , temp_output_11_0_g2 );
			float temp_output_109_0 = ( ( _DistortStrength / shaderZoom123 ) * -1.0 );
			float2 appendResult111 = (float2(temp_output_109_0 , ( temp_output_109_0 / ( _ScreenParams.x / _ScreenParams.y ) )));
			float temp_output_82_0 = ( 1.0 - ( ( 1.0 + cos( ( saturate( ( saturate( ( _FOVFadeDistance - ( ( distance( projection60 , float2( 0.5,0.5 ) ) * 2.0 ) - _FOV_Size ) ) ) / _FOVFadeDistance ) ) * UNITY_PI ) ) ) / 2.0 ) );
			float3 objToWorld2 = mul( unity_ObjectToWorld, float4( float3( 0,0,0 ), 1 ) ).xyz;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_worldTangent = WorldNormalVector( i, float3( 1, 0, 0 ) );
			float3 ase_worldBitangent = WorldNormalVector( i, float3( 0, 1, 0 ) );
			float3x3 ase_worldToTangent = float3x3( ase_worldTangent, ase_worldBitangent, ase_worldNormal );
			float3 worldToTangentDir4 = ASESafeNormalize( mul( ase_worldToTangent, ( _WorldSpaceCameraPos - objToWorld2 )) );
			#if ( SHADER_TARGET >= 50 )
			float2 recip43 = rcp( ( worldToTangentDir4.z * ( _Reticle_Tiling * ( _Reticle_Size * _Reticle_Zoom ) ) ) );
			#else
			float2 recip43 = 1.0 / ( worldToTangentDir4.z * ( _Reticle_Tiling * ( _Reticle_Size * _Reticle_Zoom ) ) );
			#endif
			float2 temp_output_50_0 = (projection60*recip43 + ( ( ( float2( 1,1 ) - recip43 ) / float2( 2,2 ) ) + _Reticle_Offset ));
			float4 temp_output_68_0 = ( temp_output_82_0 * ( ( 1.0 - step( 0.5 , distance( temp_output_50_0 , float2( 0.5,0.5 ) ) ) ) * tex2D( _Reticle, temp_output_50_0 ) ) );
			float4 temp_cast_3 = (temp_output_68_0.a).xxxx;
			float4 lerpResult70 = lerp( ( tex2D( _PIPInput, ( (projection60*recip139 + ( ( 1.0 - recip139 ) / 2.0 )) + ( i.uv_texcoord - ( temp_output_2_0_g2 + ( temp_output_11_0_g2 * ( dotResult12_g2 * dotResult12_g2 * appendResult111 ) ) + float2( 0,0 ) ) ) ) ) * temp_output_82_0 ) , ( _Reticle_Brightness * ( (( _USE_TEXTURE_COLOR )?( temp_output_68_0 ):( temp_cast_3 )) * _CrosshairColor ) ) , temp_output_68_0.a);
			float temp_output_7_0 = ( _EyeboxAccuracy + 1.0 );
			float3 appendResult9 = (float3(temp_output_7_0 , temp_output_7_0 , 1.0));
			float3 objToWorld16 = mul( unity_ObjectToWorld, float4( float3( 0,0,0 ), 1 ) ).xyz;
			float3 worldToView17 = mul( UNITY_MATRIX_V, float4( objToWorld16, 1 ) ).xyz;
			float4 lerpResult40 = lerp( lerpResult70 , float4( 0,0,0,0 ) , ( ( 1.0 + cos( ( saturate( ( saturate( ( _ShadowFadeDistance - ( ( distance( (( ( worldToTangentDir4 * appendResult9 ) + float3( i.uv_texcoord ,  0.0 ) )).xy , float2( 0.5,0.5 ) ) * 2.0 ) - ( _ShadowSize * ( 1.0 - saturate( ( _EyeReliefFalloff * distance( _EyeRelief , -worldToView17.z ) ) ) ) ) ) ) ) / _ShadowFadeDistance ) ) * UNITY_PI ) ) ) / 2.0 ));
			clip( step( 0.5 , ( 1.0 - distance( i.uv_texcoord , float2( 0.5,0.5 ) ) ) ) - 0.5);
			o.Emission = (( _USESCOPESHADOW )?( lerpResult40 ):( lerpResult70 )).rgb;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Unlit keepalpha fullforwardshadows 

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
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
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
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
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
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = IN.tSpace0.xyz * worldViewDir.x + IN.tSpace1.xyz * worldViewDir.y + IN.tSpace2.xyz * worldViewDir.z;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutput o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutput, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
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
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;51;1632,-816;Inherit;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector2Node;54;2016,-608;Inherit;False;Constant;_Vector4;Vector 3;6;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;53;2016,-736;Inherit;False;Constant;_Vector3;Vector 3;6;0;Create;True;0;0;0;False;0;False;-1,-1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.NegateNode;57;1888,-496;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;59;1984,-320;Inherit;False;Property;_Scope_Depth;Inner Tube Depth;3;0;Create;False;0;0;0;False;0;False;0.3;0.653;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;52;2192,-800;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;1,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector2Node;56;2288,-640;Inherit;False;Constant;_Vector5;Vector 3;6;0;Create;True;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;58;2272,-384;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;55;2496,-656;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;1,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SwizzleNode;65;2736,-656;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;60;2928,-656;Inherit;False;projection;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;1;-4528,768;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TransformPositionNode;2;-4496,912;Inherit;False;Object;World;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector2Node;97;-3888,-144;Inherit;False;Constant;_Vector6;Vector 6;15;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;98;-3856,-272;Inherit;False;60;projection;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;3;-4256,816;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;101;-3472,704;Inherit;False;Property;_Reticle_Size;Reticle Size;9;0;Create;False;0;0;0;False;0;False;1;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;102;-3472,784;Inherit;False;Property;_Reticle_Zoom;Reticle Zoom;19;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;96;-3632,-256;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TransformDirectionNode;4;-4064,816;Inherit;False;World;Tangent;True;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;103;-3280,720;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;104;-3296,576;Inherit;False;Property;_Reticle_Tiling;Reticle Tiling;10;0;Create;False;0;0;0;False;0;False;1,1;0.75,0.75;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TransformPositionNode;16;-3248,2112;Inherit;False;Object;World;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;95;-3344,-240;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;94;-3232,-80;Inherit;False;Property;_FOV_Size;FOV Size;4;0;Create;False;0;0;0;False;0;False;0.75;0.98;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-3344,1584;Inherit;False;Property;_EyeboxAccuracy;RequiredEyeboxAccuracy;18;0;Create;False;0;0;0;False;0;False;2;0.65;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;41;-3120,400;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-3072,656;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TransformPositionNode;17;-2992,2112;Inherit;False;World;View;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleSubtractOpNode;91;-2896,-176;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;90;-3040,-336;Inherit;False;Property;_FOVFadeDistance;FOVFadeDistance;5;0;Create;True;0;0;0;False;0;False;0.01;0.0096;0;0.05;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;7;-3008,1584;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;18;-2736,2112;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;42;-2944,544;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;89;-2656,-368;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;9;-2816,1520;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ReciprocalOpNode;43;-2784,432;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;45;-2720,592;Inherit;False;Constant;_Vector1;Vector 1;5;0;Create;True;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.BreakToComponentsNode;19;-2528,2128;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;20;-2560,2016;Inherit;False;Property;_EyeRelief;EyeRelief;14;0;Create;True;0;0;0;False;0;False;0.1;0.07;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;88;-2464,-448;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;-2528,1376;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;12;-2464,1536;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;44;-2496,592;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;47;-2496,704;Inherit;False;Constant;_Vector2;Vector 1;5;0;Create;True;0;0;0;False;0;False;2,2;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DistanceOpNode;21;-2352,2064;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-2400,1936;Inherit;False;Property;_EyeReliefFalloff;EyeReliefFalloff;15;0;Create;True;0;0;0;False;0;False;4;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;92;-2352,-288;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;10;-2224,1424;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;46;-2320,608;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;49;-2288,752;Inherit;False;Property;_Reticle_Offset;Reticle Offset;11;0;Create;False;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-2192,1968;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;107;1872,80;Inherit;False;Property;_ShaderZoom;ShaderZoom;2;0;Create;False;0;0;0;False;0;False;0;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;83;-2304,-176;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;93;-2240,-352;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;14;-2144,1648;Inherit;False;Constant;_Vector0;Vector 0;1;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;48;-2096,656;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;61;-2352,416;Inherit;False;60;projection;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;24;-2032,1968;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;128;-2088.339,1544.207;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;123;2176,80;Inherit;False;shaderZoom;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;84;-2096,-272;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;13;-1952,1552;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;25;-1856,1968;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;26;-1856,1824;Inherit;False;Property;_ShadowSize;ShadowSize;16;0;Create;True;0;0;0;False;0;False;0.85;0.847;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-2464,-1072;Inherit;False;Property;_DistortStrength;Distortion Strength;1;0;Create;False;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;124;-2416,-976;Inherit;False;123;shaderZoom;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;50;-1952,496;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;1,0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CosOpNode;85;-1952,-160;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-1552,1888;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;-1728,1568;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;108;-2240,-1056;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenParams;112;-2272,-896;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DistanceOpNode;64;-1680,352;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;86;-1856,-272;Inherit;False;2;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;29;-1344,1696;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;28;-1424,1472;Inherit;False;Property;_ShadowFadeDistance;ShadowFadeDistance;17;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0.2;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;125;-2160,-1504;Inherit;False;123;shaderZoom;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;109;-2000,-1040;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;113;-2064,-896;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;66;-1504,352;Inherit;False;2;0;FLOAT;0.5;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;87;-1728,-224;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;30;-1136,1600;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;62;-1648,480;Inherit;True;Property;_Reticle;Reticle;6;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;-1;None;c615be08ce27b3f46afe8cfd6304adc5;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;110;-1808,-960;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;67;-1392,352;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ReciprocalOpNode;139;-1872,-1600;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;31;-944,1600;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;82;-1568,-208;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;121;-1712,-1520;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;111;-1664,-1008;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;115;-1696,-1136;Inherit;False;Constant;_Vector7;Vector 7;20;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TexCoordVertexDataNode;116;-1728,-1264;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;63;-1248,448;Inherit;False;2;2;0;FLOAT;1;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;32;-768,1696;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;68;-1088,336;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;119;-1792,-1696;Inherit;False;60;projection;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;120;-1536,-1504;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;114;-1488,-1152;Inherit;False;Spherize;-1;;2;1488bb72d8899174ba0601b595d32b07;0;4;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;33;-656,1696;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;69;-848,368;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.PiNode;34;-704,1856;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;118;-1312,-1600;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;117;-1216,-1200;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ToggleSwitchNode;71;-688,608;Inherit;False;Property;_USE_TEXTURE_COLOR;Use Texture Color;7;0;Create;False;0;0;0;False;0;False;1;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;73;-784,784;Inherit;False;Property;_CrosshairColor;Reticle Color;8;0;Create;False;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;35;-496,1760;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;99;-848,-1232;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;72;-448,704;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;76;-432,464;Inherit;False;Property;_Reticle_Brightness;Reticle Brightness;12;0;Create;False;0;0;0;False;0;False;0;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;81;-592,-432;Inherit;True;Property;_PIPInput;PIPInput;0;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;-1;None;3544176f08cefa3439e544134e0729be;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CosOpNode;36;-352,1872;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;74;-208,544;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;79;-230.6224,-142.6374;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;37;-256,1760;Inherit;False;2;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;130;1088,-208;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;132;1120,-80;Inherit;False;Constant;_Vector8;Vector 8;20;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleDivideOpNode;38;-128,1808;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;131;1344,-144;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;70;464,160;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;133;1536,-144;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;40;736,448;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StepOpNode;134;1760,-160;Inherit;False;2;0;FLOAT;0.5;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;77;928,192;Inherit;False;Property;_USESCOPESHADOW;UseScopeShadow;13;0;Create;False;0;0;0;False;0;False;1;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StickyNoteNode;39;-3824,1264;Inherit;False;3880.81;100;Scope Shadow;;1,1,1,1;;0;0
Node;AmplifyShaderEditor.StickyNoteNode;78;-3328,240;Inherit;False;2911.985;100;Reticle;;1,1,1,1;;0;0
Node;AmplifyShaderEditor.StickyNoteNode;100;-4032,-560;Inherit;False;2911.985;100;Objective Size;;1,1,1,1;;0;0
Node;AmplifyShaderEditor.ClipNode;135;1440,240;Inherit;False;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0.5;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;136;1536,368;Inherit;False;Constant;_Float0;Float 0;20;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;137;1520,448;Inherit;False;Constant;_Float1;Float 0;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;138;1200,384;Inherit;False;Constant;_Color0;Color 0;20;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;122;-1952,-1648;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1728,240;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Ultimate Scope Shaders/ASE/ScopeASE;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;57;0;51;0
WireConnection;52;0;51;0
WireConnection;52;1;53;0
WireConnection;52;2;54;0
WireConnection;58;0;57;0
WireConnection;58;1;59;0
WireConnection;55;0;52;0
WireConnection;55;1;56;0
WireConnection;55;2;58;0
WireConnection;65;0;55;0
WireConnection;60;0;65;0
WireConnection;3;0;1;0
WireConnection;3;1;2;0
WireConnection;96;0;98;0
WireConnection;96;1;97;0
WireConnection;4;0;3;0
WireConnection;103;0;101;0
WireConnection;103;1;102;0
WireConnection;95;0;96;0
WireConnection;41;0;4;0
WireConnection;105;0;104;0
WireConnection;105;1;103;0
WireConnection;17;0;16;0
WireConnection;91;0;95;0
WireConnection;91;1;94;0
WireConnection;7;0;6;0
WireConnection;18;0;17;0
WireConnection;42;0;41;2
WireConnection;42;1;105;0
WireConnection;89;0;90;0
WireConnection;89;1;91;0
WireConnection;9;0;7;0
WireConnection;9;1;7;0
WireConnection;43;0;42;0
WireConnection;19;0;18;0
WireConnection;88;0;89;0
WireConnection;5;0;4;0
WireConnection;5;1;9;0
WireConnection;44;0;45;0
WireConnection;44;1;43;0
WireConnection;21;0;20;0
WireConnection;21;1;19;2
WireConnection;92;0;88;0
WireConnection;92;1;90;0
WireConnection;10;0;5;0
WireConnection;10;1;12;0
WireConnection;46;0;44;0
WireConnection;46;1;47;0
WireConnection;23;0;22;0
WireConnection;23;1;21;0
WireConnection;93;0;92;0
WireConnection;48;0;46;0
WireConnection;48;1;49;0
WireConnection;24;0;23;0
WireConnection;128;0;10;0
WireConnection;123;0;107;0
WireConnection;84;0;93;0
WireConnection;84;1;83;0
WireConnection;13;0;128;0
WireConnection;13;1;14;0
WireConnection;25;0;24;0
WireConnection;50;0;61;0
WireConnection;50;1;43;0
WireConnection;50;2;48;0
WireConnection;85;0;84;0
WireConnection;27;0;26;0
WireConnection;27;1;25;0
WireConnection;15;0;13;0
WireConnection;108;0;106;0
WireConnection;108;1;124;0
WireConnection;64;0;50;0
WireConnection;86;1;85;0
WireConnection;29;0;15;0
WireConnection;29;1;27;0
WireConnection;109;0;108;0
WireConnection;113;0;112;1
WireConnection;113;1;112;2
WireConnection;66;1;64;0
WireConnection;87;0;86;0
WireConnection;30;0;28;0
WireConnection;30;1;29;0
WireConnection;62;1;50;0
WireConnection;110;0;109;0
WireConnection;110;1;113;0
WireConnection;67;0;66;0
WireConnection;139;0;125;0
WireConnection;31;0;30;0
WireConnection;82;0;87;0
WireConnection;121;0;139;0
WireConnection;111;0;109;0
WireConnection;111;1;110;0
WireConnection;63;0;67;0
WireConnection;63;1;62;0
WireConnection;32;0;31;0
WireConnection;32;1;28;0
WireConnection;68;0;82;0
WireConnection;68;1;63;0
WireConnection;120;0;121;0
WireConnection;114;2;116;0
WireConnection;114;3;115;0
WireConnection;114;4;111;0
WireConnection;33;0;32;0
WireConnection;69;0;68;0
WireConnection;118;0;119;0
WireConnection;118;1;139;0
WireConnection;118;2;120;0
WireConnection;117;0;116;0
WireConnection;117;1;114;0
WireConnection;71;0;69;3
WireConnection;71;1;68;0
WireConnection;35;0;33;0
WireConnection;35;1;34;0
WireConnection;99;0;118;0
WireConnection;99;1;117;0
WireConnection;72;0;71;0
WireConnection;72;1;73;0
WireConnection;81;1;99;0
WireConnection;36;0;35;0
WireConnection;74;0;76;0
WireConnection;74;1;72;0
WireConnection;79;0;81;0
WireConnection;79;1;82;0
WireConnection;37;1;36;0
WireConnection;38;0;37;0
WireConnection;131;0;130;0
WireConnection;131;1;132;0
WireConnection;70;0;79;0
WireConnection;70;1;74;0
WireConnection;70;2;69;3
WireConnection;133;0;131;0
WireConnection;40;0;70;0
WireConnection;40;2;38;0
WireConnection;134;1;133;0
WireConnection;77;0;70;0
WireConnection;77;1;40;0
WireConnection;135;0;77;0
WireConnection;135;1;134;0
WireConnection;122;1;125;0
WireConnection;0;2;135;0
ASEEND*/
//CHKSM=B717FBE433E4358A72EE849A125EC99629892959