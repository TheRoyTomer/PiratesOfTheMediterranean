Shader "Hidden/KriptoFX/KWS/WaterCustomMesh"
{
	Properties
	{
		srpBatcherFix ("srpBatcherFix", Float) = 0
		[HideInInspector]KWS_StencilMaskValue("KWS_StencilMaskValue", Int) = 32
	}

	SubShader
	{
		Tags { "Queue" = "Transparent-1" "IgnoreProjector" = "True" "RenderType" = "Transparent" "DisableBatching" = "true"}
		
		
		Stencil
		{
			Ref [KWS_StencilMaskValue]
            ReadMask [KWS_StencilMaskValue]
			Comp Greater
			Pass keep
		}

		
		
		Pass
		{
			
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On

			Cull Back
			HLSLPROGRAM

			#pragma multi_compile _ KWS_USE_OCEAN_RENDERING
			#pragma multi_compile _ KWS_USE_LOCAL_WATER_ZONES
			#pragma multi_compile_fragment _ KWS_USE_CLIP_MASKING
			
			
 			#pragma shader_feature_fragment _ KWS_REFLECT_SUN
			#pragma shader_feature_fragment _ KWS_USE_VOLUMETRIC_LIGHT
			#pragma shader_feature_fragment _ KWS_SSR_REFLECTION
			#pragma shader_feature_fragment _ KWS_USE_PLANAR_REFLECTION
			#pragma shader_feature_fragment _ KWS_USE_REFRACTION_IOR
			#pragma shader_feature_fragment _ KWS_USE_REFRACTION_DISPERSION
			#pragma shader_feature_fragment _ KWS_USE_CAUSTIC

			#define KWS_USE_CUSTOM_MESH
		
			#include "../PlatformSpecific/Includes/KWS_VertFragIncludes.cginc"
	
			#if defined(ENVIRO_3_FOG)
            	#pragma multi_compile_fragment _ ENVIRO_VOLUMELIGHT
				#pragma multi_compile_fragment _ ENVIRO_SIMPLEFOG
				#pragma multi_compile_fragment _ ENVIRO_SIMPLESKY
			#endif
				
			#pragma vertex vertWater
			#pragma fragment fragWater
			#pragma target 4.6
			#pragma editor_sync_compilation
			#pragma enable_d3d11_debug_symbols
			
			ENDHLSL
		}
	}
}