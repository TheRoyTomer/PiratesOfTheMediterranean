Shader "Hidden/KriptoFX/KWS/WaterPrePassZoneInstance"
{
	Properties
	{
		srpBatcherFix ("srpBatcherFix", Float) = 0
		//[HideInInspector]KWS_StencilMaskValue ("KWS_StencilMaskValue", Int) = 32

	}

	SubShader
	{
		Tags { "Queue" = "Transparent-1" "IgnoreProjector" = "True" "RenderType" = "Transparent" "DisableBatching" = "true" }
		
		//Stencil
		//{
		//	Ref 1
		//	Comp Always
		//	Pass replace
		//}

		//0 non-tesselated
		Pass
		{
			Blend Off
			ZWrite On
			Cull Off
			//BlendOp 0 Max

			HLSLPROGRAM

			#pragma multi_compile _ KWS_USE_OCEAN_RENDERING
			#pragma multi_compile _ KWS_USE_LOCAL_WATER_ZONES 
			#pragma multi_compile _ KWS_USE_DYNAMIC_WAVES KWS_USE_COLORED_DYNAMIC_WAVES
	
			
			#pragma shader_feature_fragment _ KWS_USE_HALF_LINE_TENSION
		
			#define KWS_USE_ZONE_INSTANCE
			#define KWS_PRE_PASS

			#include "../../PlatformSpecific/Includes/KWS_VertFragIncludes.cginc"

			#pragma target 4.6
			#pragma vertex vertDepth
			#pragma fragment fragDepth
			
			#pragma editor_sync_compilation

			ENDHLSL
		}


		//2 -> 1 backface non-tesselated
		Pass
		{
			ZWrite On
			Cull Front

			//ColorMask R 1 //write only id channel and save others

			HLSLPROGRAM
			
			#pragma multi_compile _ KWS_USE_OCEAN_RENDERING
			#pragma multi_compile _ KWS_USE_LOCAL_WATER_ZONES 
			#pragma multi_compile _ KWS_USE_DYNAMIC_WAVES KWS_USE_COLORED_DYNAMIC_WAVES
	
			
			#pragma shader_feature_fragment _ KWS_USE_HALF_LINE_TENSION
		
			#define KWS_USE_ZONE_INSTANCE
			#define KWS_PRE_PASS_BACK_FACE

			#include "../../PlatformSpecific/Includes/KWS_VertFragIncludes.cginc"

			#pragma target 4.6
			#pragma vertex vertDepth
			#pragma fragment fragDepth
			
			#pragma editor_sync_compilation

			ENDHLSL
		}


		//4 -> 2 ocean underwater mask
		Pass
		{
			Cull Off
			
			HLSLPROGRAM

			#pragma vertex vertHorizon
			#pragma fragment fragHorizon
			#pragma target 4.6
			#pragma editor_sync_compilation

			//#include "../../Common/KWS_WaterHelpers.cginc"
			#include "../../PlatformSpecific/Includes/KWS_VertFragIncludes.cginc"


			struct vertexInputHorizon
			{
				uint vertexID : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct vertexOutputHorizon
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			vertexOutputHorizon vertHorizon(vertexInputHorizon v)
			{
				vertexOutputHorizon o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = GetTriangleVertexPosition(v.vertexID);
				o.uv = GetTriangleUVScaled(v.vertexID);
				return o;
			}

			WaterFragmentOutput fragHorizon(vertexOutputHorizon i) 
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				WaterFragmentOutput o = (WaterFragmentOutput)0;
				
				float3 worldPos = GetWorldSpacePositionFromDepth(i.uv + KWS_WaterPrePassRT0_TexelSize.xy * float2(0, 0), 0.0001); //todo check how to fix it with TAA
				float oceanUnderwaterMask = worldPos.y <= KWS_OceanLevel ?   1 : 0;
				o.pass1 = half4(oceanUnderwaterMask * KWS_WATER_MASK_DECODING_VALUE, oceanUnderwaterMask, 0, 0);
				
				return o;
			}
			

			ENDHLSL
		}

		//5 -> 3 tension mask
		Pass
		{
			Cull Off
			
			HLSLPROGRAM

			#pragma vertex vertHorizon
			#pragma fragment fragHorizon
			#pragma target 4.6
			#pragma editor_sync_compilation

			#include "../../Common/KWS_WaterHelpers.cginc"
			
			DECLARE_TEXTURE(_SourceRT);
			float4 _SourceRT_TexelSize;
			float4 _SourceRTHandleScale;

			struct vertexInput
			{
				uint vertexID : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct vertexOutput
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			vertexOutput vertHorizon(vertexInput v)
			{
				vertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = GetTriangleVertexPosition(v.vertexID);
				o.uv = GetTriangleUVScaled(v.vertexID);
				return o;
			}

			float4 fragHorizon(vertexOutput i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float2 offsetUV = float2(0, _SourceRT_TexelSize.y);
				//offsetUV.y *= lerp(-5, 10, KWS_UnderwaterHalfLineTensionScale); //0.5 rtScale
				offsetUV.y *= lerp(-2, 13, KWS_UnderwaterHalfLineTensionScale); //0.35 rtScale
				//offsetUV.y *= 1-unity_OrthoParams.w; //disable offset for ortho view

				float2 uv = GetRTHandleUV(i.uv, _SourceRT_TexelSize.xy, 10.0, _SourceRTHandleScale.xy);
				float mask = SAMPLE_TEXTURE_LOD(_SourceRT, sampler_point_clamp, uv + offsetUV, 0).y;
				if (mask > 0.51) return 1;
				else return 0;
			}
			

			ENDHLSL
		}
	}
}