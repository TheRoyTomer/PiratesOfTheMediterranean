Shader "Hidden/KriptoFX/KWS/Underwater"
{
    Properties
    {
        [HideInInspector]KWS_StencilMaskValue ("KWS_StencilMaskValue", Int) = 32
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
		
        ZWrite Off
        Cull Off

        Stencil
        {
            Ref [KWS_StencilMaskValue]
            ReadMask [KWS_StencilMaskValue]
            Comp Greater
            Pass keep
        }


        Pass
        {
            HLSLPROGRAM
            #pragma vertex vertUnderwater
            #pragma fragment fragUnderwater
            #pragma target 4.6
			#pragma enable_d3d11_debug_symbols
          
            #pragma shader_feature_fragment _ KWS_USE_VOLUMETRIC_LIGHT
            #pragma shader_feature_fragment _ USE_PHYSICAL_APPROXIMATION_COLOR USE_PHYSICAL_APPROXIMATION_SSR
            #pragma shader_feature_fragment _ KWS_USE_HALF_LINE_TENSION
			#pragma shader_feature_fragment _ KWS_USE_CAUSTIC
		
            
            #pragma multi_compile_fragment _ KWS_USE_OCEAN_RENDERING
			#pragma multi_compile_fragment _ KWS_USE_DYNAMIC_WAVES KWS_USE_COLORED_DYNAMIC_WAVES
            #pragma multi_compile_fragment _ KWS_USE_LOCAL_WATER_ZONES
			#pragma multi_compile_fragment _ KWS_IS_CAMERA_PARTIAL_UNDERWATER
            #pragma multi_compile_fragment _ KWS_USE_CLIP_MASKING
            
            #include "../../Common/KWS_WaterHelpers.cginc"
			
            DECLARE_TEXTURE(_SourceRT);
            float4 KWS_Underwater_RTHandleScale;
            float4 _SourceRTHandleScale;
            float4 _SourceRT_TexelSize;
			float KWS_UnderwaterNoiseBlurRadius;


            
            float MaskToAlpha(float mask)
            {
                return saturate(mask * mask * mask * 20);
            }

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


            vertexOutput vertUnderwater(vertexInput v)
            {
                vertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = GetTriangleVertexPosition(v.vertexID);
                o.uv = GetTriangleUVScaled(v.vertexID);
                return o;
            }

            half4 fragUnderwater(vertexOutput i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 uv = i.uv;
            	
            	//float3 foamColor = GetVolumetricSurfaceLight(uv).xyz;
            	//return float4(foamColor, 1);
            	//float3 foamColor = GetScreenSpaceFoamBuffer(uv).xyz;
            	//foamColor = lerp(foamColor, saturate(foamColor - Test4.y), Test4.z);
            	//return float4(GetWaterMask(uv).x, 0,0 , 0.95);
            	
            	#ifdef KWS_USE_CLIP_MASKING
            	
            		float2 clipMaskThickness = GetWaterMaskThickness(uv);
            		float clipMask = GetWaterClipMask(i.uv, float2(0, 1));
            		bool insideAir = (clipMaskThickness.x == 0.0 && clipMaskThickness.y > 0);
            		
            	#endif 
            	
				
                #if KWS_USE_HALF_LINE_TENSION
            	
            		float waterHalflineMask  = 0;
            	
            		#ifdef KWS_IS_CAMERA_PARTIAL_UNDERWATER
						waterHalflineMask = GetWaterHalfLineTensionMask(uv - float2(0, _SourceRT_TexelSize.y * 5));
            		#endif
            	
            		#ifdef KWS_USE_CLIP_MASKING
            			waterHalflineMask *= 1-(float)insideAir;
            		#endif
            	
					float halfLineUvOffset = -waterHalflineMask * 0.25 + waterHalflineMask * waterHalflineMask * 0.25;
					uv.y -= halfLineUvOffset;
            	
                #endif
            	
            	float waterMask = GetWaterMask(uv, float2(0, 1));
            	
            	
            	float noise = KWS_InterleavedGradientNoise((float2)i.vertex.xy, KWS_Time + 9.123);
            	if (KWS_UnderwaterNoiseBlurRadius > 0)
            	{
            		float2 blurRadius = KWS_UnderwaterNoiseBlurRadius * _SourceRT_TexelSize.xy;  
            		float2 disk = KWS_PointOnUnitCircle(noise * 6.283);
            		float2 blurOffset = disk * blurRadius;
            		uv = clamp(uv + blurOffset, 0, 1);
            	}
            	
            	
            	float sceneZ = GetSceneDepth(uv);
				float waterDepth = GetWaterDepth(uv);
            	
            	
            	
            	#ifdef KWS_USE_CLIP_MASKING
					float2 volumeDepth = GetWaterVolumeDepth(uv, sceneZ, waterMask, waterDepth, clipMaskThickness, clipMask);
            	#else
            		float2 volumeDepth = GetWaterVolumeDepth(uv, sceneZ, waterMask, waterDepth);
				#endif
            	
            	#ifdef KWS_USE_CLIP_MASKING
            		
            		bool underwaterMaskHoleFix = (!GetUnderwaterMask(waterMask) && GetUnderwaterMask(clipMask) && !insideAir);
            		if (underwaterMaskHoleFix) waterMask = clipMask;
            	
            	#endif 
            	
            	bool surfaceMask = (abs(waterMask - 0.75) < 0.1) && (volumeDepth.y > sceneZ); //bool zero VGPR cost
				float alpha = volumeDepth.x > 0 && waterMask > 0.5 ?   1 : 0;
            	
            	//VolumetricLightAdditionalData data = GetVolumetricLightAdditionalData(uv);
                //return float4(data.SurfaceDirShadow.xxx, 1);
				

                #if KWS_USE_HALF_LINE_TENSION
					alpha = saturate(alpha + waterHalflineMask * 10);
                #endif

            	#ifdef KWS_USE_CLIP_MASKING
            		            	
            		bool surfaceToMaskIntersection = clipMaskThickness.y < waterDepth &&  (insideAir || clipMaskThickness.x > waterDepth);
            		bool surfacefringe = clipMaskThickness.y > sceneZ;
            		bool shouldClip = surfaceMask && insideAir && surfaceToMaskIntersection;
	     
            		float underwaterWindowMask = (clipMaskThickness.x > 0.0 && clipMaskThickness.x > sceneZ 
            			&& clipMaskThickness.x > waterDepth && clipMask > 0.5)? 0 : 1;
            	
	                alpha = surfaceToMaskIntersection && surfacefringe ? 1 : alpha;
            		alpha = shouldClip ? 0 : alpha;
            		
            	#endif 
            	
                if (alpha == 0) discard;


				float localZoneOffsetBlending = 0;
				float localZoneMaxHeightOffset = 0;
            	
                float waterLevel = KWS_WaterLevel;
                float transparent = KWS_Transparent;
                float3 turbidityColor = KWS_TurbidityColor;
                float3 waterColor = KWS_WaterColor;

                transparent = clamp(transparent + KWS_UnderwaterTransparentOffset, 1, KWS_MAX_TRANSPARENT * 2);
            	
                float3 camPos = GetCameraAbsolutePosition();
                
            	float3 worldPos = GetWorldSpacePositionFromDepth(uv, volumeDepth.y); //todo add real water wave offset

                #if defined(KWS_USE_LOCAL_WATER_ZONES)

	                LocalZoneData blendedZone = (LocalZoneData)0;
	                blendedZone.transparent = transparent;
	                blendedZone.turbidityColor.xyz = turbidityColor.xyz;
	                blendedZone.waterColor.xyz = waterColor.xyz;

            		#if defined(KWS_USE_OCEAN_CAUSTIC)
            			EvaluateBlendedZoneDataWithHeight(blendedZone, camPos, normalize(worldPos - camPos), GetWorldToCameraDistance(worldPos), waterLevel + 20, localZoneMaxHeightOffset, localZoneOffsetBlending);
            		#else
            			EvaluateBlendedZoneData(blendedZone, camPos, normalize(worldPos - camPos), GetWorldToCameraDistance(worldPos), waterLevel + 20, noise * 2 - 1);
            		#endif
	               
            		//return float4(saturate(blendedZone.transparent), 0, 0, 1);
	                transparent = blendedZone.transparent;
	                turbidityColor.xyz = blendedZone.turbidityColor.xyz;
	                waterColor.xyz = blendedZone.waterColor.xyz;

            		if (transparent < 0.001) discard;

                #endif

                half3 normal = GetWaterNormals(uv.xy) * surfaceMask;
				#if KWS_USE_CLIP_MASKING
            		normal *= underwaterWindowMask;
            	#endif
            	
            	
            	
            	float2 refractionUV = uv.xy + normal.xz;
                half3 refraction = GetSceneColor(refractionUV);
            	
            	float3 oceanCaustic = 0;
				float3 zoneCaustic = 0;
            	float zoneCausticFadeFactor = 0;
            	waterLevel = lerp(waterLevel, localZoneMaxHeightOffset + KWS_WaterLevel, localZoneOffsetBlending);
            	float waterHeightDepth =  waterLevel - worldPos.y;

			
            	#if defined(KWS_USE_CAUSTIC)
            	
            	
            		#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES)
            	
						float2 flowDirection = 0;
            	
            			bool isDynamicWavesZone = false;
						float borderFade;
            			float velocityLength = 0;

            			float distanceToCamera = GetWorldToCameraDistanceXZ(worldPos);
            			float isOutDistance = distanceToCamera > KW_WaterFarDistance * 0.5;
            	
						float3 dynamicWavesMapUV = GetDynamicWavesMapUV(worldPos, distanceToCamera);
            			
            	
						isDynamicWavesZone = !IsOutsideUvBorders(dynamicWavesMapUV);
						if (surfaceMask < 0.5 && isDynamicWavesZone && !isOutDistance)
						{
							float4 dynamicWaves = GetDynamicWavesMapBicubic(dynamicWavesMapUV);
							DynamicWavesAdditionalData additionalData = GetDynamicWavesAdditionalMapBicubic(dynamicWavesMapUV);
							waterLevel += (dynamicWaves.z + dynamicWaves.w) * additionalData.zoneFade;

							borderFade = additionalData.zoneFade;
							velocityLength = length(dynamicWaves.xy);
							flowDirection = (flowDirection + dynamicWaves.xy);
							ZoneData zone = KWS_ZoneData[additionalData.zoneID];

							float2 flowDirectionNormalized = NormalizeDynamicWavesVelocity(flowDirection);
							
            				zoneCaustic = GetDynamicWavesZoneCaustic(worldPos.xz, waterHeightDepth, flowDirectionNormalized, velocityLength, zone.flowSpeedMultiplier * zone.timeScale, 1);
            				//caustic.xyz = lerp(caustic.xyz, zoneCaustic.xyz, KWS_Pow2(borderFade));
							zoneCausticFadeFactor = KWS_Pow2(borderFade);

                           
/*
                            #ifdef KWS_USE_COLORED_DYNAMIC_WAVES
								float4 zoneColorData = GetDynamicWavesColorMap(dynamicWavesMapUV);
								zoneColorData.rgb = lerp(zoneColorData.rgb, zoneColorData.rgb * 0.35, saturate(zoneColorData.a * zoneColorData.a + zoneColorData.a * 2));
								zoneColorData.a = saturate(zoneColorData.a * 2);
								zoneColorData.a *= 1 - saturate((KWS_WaterLevel - 1 - worldPos.y) / (DYNAMIC_WAVE_COLOR_MAX_TRANSPARENT * 2));

								turbidityColor = lerp(turbidityColor, zoneColorData.rgb, zoneColorData.a);
								waterColor = lerp(waterColor, zoneColorData.rgb, zoneColorData.a);
								//transparent = lerp(transparent, DYNAMIC_WAVE_COLOR_MAX_TRANSPARENT, zoneColorData.a);

							#endif
			*/
						}
            	
					#endif
			
					#ifdef KWS_USE_OCEAN_RENDERING
            			if (surfaceMask < 0.5 && zoneCausticFadeFactor < 0.95)
            			{
            				float depthAttenuation = saturate(exp2(- waterHeightDepth / max(2, transparent)));
							oceanCaustic = GetOceanCaustic(i.vertex.xy, worldPos.xz, waterHeightDepth) * depthAttenuation; 
            				
            			}
					#endif
				
            	
            		float3 finalCaustic = lerp(oceanCaustic.xyz, zoneCaustic.xyz, zoneCausticFadeFactor);
            	
            		#ifdef KWS_USE_VOLUMETRIC_LIGHT
						VolumetricLightAdditionalData volumeData = GetVolumetricLightAdditionalData(uv);
            			finalCaustic *= lerp(1, KWS_Pow10(volumeData.SceneDirShadow), KWS_DisableCausticInShadow);
            		#endif
            	
            	
            		#ifdef KWS_USE_CLIP_MASKING
            			finalCaustic *= underwaterWindowMask; 
            		#endif
            			
            	
					refraction.rgb *= 1.0 + finalCaustic;

				#endif
			
            
			
				
                #if defined(USE_PHYSICAL_APPROXIMATION_COLOR) || defined(USE_PHYSICAL_APPROXIMATION_SSR)
					float3 worldViewDir = GetWorldSpaceViewDirNorm(worldPos);
					float distanceToSurface = saturate((worldPos.y - GetCameraAbsolutePosition().y) * 0.3);
					//float3 refractedRay = refract(-worldViewDir, normal, lerp(0.95, KWS_WATER_IOR, distanceToSurface));
					float3 refractedRay = refract(-worldViewDir, normal, KWS_WATER_IOR);

					float refractedMask = 1 - clamp(-refractedRay.y * 100, 0, 1);
					refractedMask *= surfaceMask;
            	
            	#if KWS_USE_CLIP_MASKING
            		refractedMask *= underwaterWindowMask;
            	#endif
					
					float3 reflection = turbidityColor * 0.05;
                #endif

                #ifdef USE_PHYSICAL_APPROXIMATION_SSR
					float3 reflDir = reflect(-worldViewDir, normal);
					float2 refl_uv = GetScreenSpaceReflectionUV(reflDir, uv + normal.xz * 0.5);
					float4 ssrReflection = GetScreenSpaceReflection(refl_uv, worldPos);
					
					reflection = lerp(refraction.xyz, ssrReflection.xyz, ssrReflection.a);
					reflection = lerp(reflection, 0, distanceToSurface);
					reflection = lerp(reflection.xyz, ssrReflection.xyz, ssrReflection.a);

                #endif
				
                #if defined(USE_PHYSICAL_APPROXIMATION_COLOR) || defined(USE_PHYSICAL_APPROXIMATION_SSR)
					refraction = lerp(refraction, reflection, refractedMask);
                #endif
				
				

                float3 volLight = GetVolumetricLightWithAbsorbtion(uv, uv, transparent, turbidityColor, waterColor, refraction, volumeDepth, GetExposure(), 0).xyz;

                #if KWS_USE_HALF_LINE_TENSION
					volLight = lerp(volLight, refraction * volLight * 2 * waterHalflineMask * waterHalflineMask + volLight * 0.5, waterHalflineMask);
                #endif


                return float4(volLight, alpha);
            }
            ENDHLSL
        }
    }
}