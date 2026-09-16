Shader "Hidden/KriptoFX/KWS/Water"
{
	Properties
	{
		srpBatcherFix ("srpBatcherFix", Float) = 0
		[HideInInspector]KWS_StencilMaskValue("KWS_StencilMaskValue", Int) = 32
		[Int] _ZWriteOverride("ZWrite Override", Int) = 1
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
			ZWrite [_ZWriteOverride]

			Cull Back
			HLSLPROGRAM
			

			#pragma multi_compile _ KWS_USE_OCEAN_RENDERING
			#pragma multi_compile _ KWS_USE_LOCAL_WATER_ZONES
			#pragma multi_compile _ KWS_USE_DYNAMIC_WAVES KWS_USE_COLORED_DYNAMIC_WAVES
			#pragma multi_compile_fragment _ KWS_USE_CLIP_MASKING
			
 			#pragma shader_feature_fragment _ KWS_REFLECT_SUN
			#pragma shader_feature_fragment _ KWS_USE_VOLUMETRIC_LIGHT
			#pragma shader_feature_fragment _ KWS_SSR_REFLECTION
			#pragma shader_feature_fragment _ KWS_USE_PLANAR_REFLECTION
			#pragma shader_feature_fragment _ KWS_USE_REFRACTION_IOR
			#pragma shader_feature_fragment _ KWS_USE_REFRACTION_DISPERSION
			#pragma shader_feature_fragment _ KWS_USE_CAUSTIC

			#pragma shader_feature_fragment _ KWS_USE_BAKED_OCEAN_WAVES

			#define KWS_USE_WATER_INSTANCING
			
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