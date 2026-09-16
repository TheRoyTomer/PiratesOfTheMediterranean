#ifndef KWS_SHARED_API_INCLUDED
#define KWS_SHARED_API_INCLUDED

#define KWS_SHARED_API_IGNORE_INCLUDES

#ifdef KWS_SHARED_API_CUSTOM
	#undef KWS_SHARED_API_IGNORE_INCLUDES
#endif

#ifndef SHADERGRAPH_PREVIEW

	#ifndef KWS_WATER_VERT_FRAG
		#include "../PlatformSpecific/Includes/KWS_VertFragIncludes.cginc"

	#endif

#endif



inline float3 GetWaterSurfaceCollisionForQuadParticlesAquarium(float3 vertex, float3 center, float levelOffset)
{
	#ifdef SHADERGRAPH_PREVIEW
		return vertex;
	#else
		
		float waterLevel = KWS_ParticlesPos.y + levelOffset;
		float3 waterDisplacement = GetFftWavesDisplacement(vertex);

		float3 quadOffset = vertex.xyz - center.xyz;
		float quadOffsetLength = length(quadOffset);

		if (center.y > waterLevel - quadOffsetLength)
		{
			center.y = waterLevel + waterDisplacement.y - quadOffsetLength;
			vertex = center.xyz + quadOffset;
		}
		
		return vertex;
	#endif
}

inline float3 GetWaterSurfaceCollisionForQuadParticles(float3 vertex, float3 center)
{
	#ifdef SHADERGRAPH_PREVIEW
		return vertex;
	#else

		float3 quadOffset = vertex.xyz - center.xyz;
		float quadOffsetLength = length(quadOffset);

		WaterOffsetData waterData = ComputeWaterOffset(vertex);
		float surfaceLevel = KWS_WaterLevel + waterData.offset.y;
		
		if (center.y > surfaceLevel - quadOffsetLength)
		{
			center.y = surfaceLevel - quadOffsetLength;
			vertex = center.xyz + quadOffset;
		}

	
		return vertex;
	#endif
}


inline void GetWetnessData(float2 screenUV, float2 uv, float sceneDepth, out float3 diffuseColor, out float wetMap, out float occlusion, out float smoothness, out float metallic)
{
	diffuseColor = float3(0, 0, 0);
	wetMap = 0;
	occlusion = 0;
	smoothness = 0;
	metallic = 0;

	#ifdef SHADERGRAPH_PREVIEW
		
	#else

		float depth = sceneDepth;
		float3 worldPos = GetWorldSpacePositionFromDepth(screenUV, depth);
		float waterLevel =  KWS_WaterLevel;
		float waterLevelMaxLevelFade = KWS_Pow2(KWS_WindSpeed / KWS_MAX_WIND_SPEED) * 20 + 1;
	
		float borderFade = 0;
		float colorOverrideStrength = 0;
		float reflectionStrength = 1.0;
	    float underwaterFade = 1.0;
	
		#ifdef KWS_USE_CLIP_MASKING
	
			if (ShouldClipWaterSurface(screenUV, depth)) return;
			
		#endif


		#if defined(KWS_USE_LOCAL_WATER_ZONES)
		
			uint zoneIndexOffset_local = 0;
			uint zoneIndexCount_local = 0;
			bool isLocalWaterZone = GetTileRange_LocalZone(worldPos, zoneIndexOffset_local, zoneIndexCount_local);

			if (isLocalWaterZone)
			{
				float offsetBlending = 0;
				float maxHeightOffset = -100000;
				for (uint zoneIndex = zoneIndexOffset_local; zoneIndex < zoneIndexCount_local; zoneIndex++)
				{
					LocalZoneData zone = (LocalZoneData)0;
					if (GetWaterZone_LocalZone(worldPos, zoneIndex, zone))
					{
						if (zone.overrideHeight > 0.5 && zone.clipWaterBelowZone)
						{
							float2 distanceToBox = abs(mul((worldPos.xz - zone.center.xz), zone.rotationMatrix)) / zone.halfSize.xz;
							float distanceToBorder = max(distanceToBox.x, distanceToBox.y);
							float zoneMinHeight = zone.center.y - zone.halfSize.y;
							if (distanceToBorder < 1.1 && worldPos.y < zoneMinHeight && worldPos.y > KWS_WaterLevel) return;
						}
						
						if (zone.overrideHeight > 0.5)
						{
							float currentHeightOffset = zone.center.y + zone.halfSize.y - KWS_WaterLevel;
							float heightFade = GetLocalWaterZoneSphereBlendFactor(zone.uv, zone.heightEdgeBlending);
							maxHeightOffset = max(maxHeightOffset, currentHeightOffset);
							offsetBlending = lerp(heightFade, 1, KWS_Pow20(zone.heightEdgeBlending));
								
						}
							

					}
				}
				waterLevel += lerp(0, maxHeightOffset, offsetBlending);
					
			}
		#endif

		
		if (worldPos.y < waterLevel + waterLevelMaxLevelFade) wetMap = clamp(waterLevel - worldPos.y + waterLevelMaxLevelFade, 0, 1) * 1;
		
	

		#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES)

		float distanceToCamera = GetWorldToCameraDistanceXZ(worldPos);
		float3 dynamicWavesMapUV = GetDynamicWavesMapUV(worldPos, distanceToCamera);
		if (!IsOutsideUvBorders(dynamicWavesMapUV.xy))
		{
			float4 dynamicWaves = GetDynamicWavesMapBicubic(dynamicWavesMapUV);
			DynamicWavesAdditionalData additionalData = GetDynamicWavesAdditionalMapBicubic(dynamicWavesMapUV);
			DynamicWavesWetData wetData = GetDynamicWavesWetMapBicubic(dynamicWavesMapUV);
	
			ZoneData zone = KWS_ZoneData[additionalData.zoneID];
			float worldHeight = DecodeDynamicWavesHeight(wetData.wetMapDepth, zone.center, zone.halfSize * 2);
			float maxWetLevel = KWS_WetLevel;
			float currentHeight = dynamicWaves.z + worldHeight;
			float wetFadeByHeight = 1 - saturate(worldPos.y - currentHeight - maxWetLevel);

			currentHeight = dynamicWaves.z + dynamicWaves.w + KWS_WaterLevel;
			underwaterFade *= saturate(worldPos.y - currentHeight + 1);
						
			borderFade = saturate(borderFade + additionalData.zoneFade);
			wetMap = max(wetMap, wetData.wetMask * wetFadeByHeight);
			
			#if defined(KWS_USE_COLORED_DYNAMIC_WAVES)
				float4 zoneColorData = GetDynamicWavesColorMap(dynamicWavesMapUV);
				float colorTransparencyFactor = (dot(zoneColorData.rgb, 0.33));
				zoneColorData.a = lerp(zoneColorData.a, 0, colorTransparencyFactor);
			
				zoneColorData.rgb = lerp(zoneColorData.rgb, zoneColorData.rgb * 0.35, saturate(zoneColorData.a * zoneColorData.a + zoneColorData.a * 2));
				zoneColorData.a = saturate(zoneColorData.a * 2);
				zoneColorData.a *= 1 - saturate((KWS_WaterLevel - 1 - worldPos.y) / (DYNAMIC_WAVE_COLOR_MAX_TRANSPARENT * 2));

				colorOverrideStrength = max(colorOverrideStrength, zoneColorData.a * 3);
				diffuseColor.rgb = lerp(diffuseColor.rgb, zoneColorData.rgb, zoneColorData.a);
			
			#endif
			
		}

		#endif


		#ifdef KWS_USE_ZONE_INSTANCE
			float4 dynamicWaves = GetDynamicWavesZone(uv);
			float4 dynamicWavesAdditionalData = GetDynamicWavesZoneAdditionalDataBicubic(uv); //(wetmap, shoreline mask, foam mask, wetDepth)
			float zoneFade = GetDynamicWavesBorderFading(uv);
								
			float worldHeight = DecodeDynamicWavesHeight(dynamicWavesAdditionalData.w);
			float maxWetLevel = KWS_WetLevel;
			float currentHeight = dynamicWaves.z + worldHeight;
			float wetFadeByHeight = 1 - saturate(saturate(worldPos.y - currentHeight) * 5);

			currentHeight = dynamicWaves.z + dynamicWaves.w + KWS_WaterLevel;
			underwaterFade *= saturate(worldPos.y - currentHeight + 1);
								
			borderFade = saturate(borderFade + zoneFade);
			wetMap = max(wetMap, dynamicWavesAdditionalData.x * wetFadeByHeight);

	
			//float waterZEye = LinearEyeDepthUniversal(GetWaterDepth(screenUV));
			//float waterFade = 1-saturate((waterZEye - LinearEyeDepthUniversal(sceneDepth)) * 2);
			//wetMap *= waterFade;
								
			#if defined(KWS_USE_COLORED_DYNAMIC_WAVES)
				float4 zoneColorData = GetDynamicWavesZoneColorData(uv);
				float colorTransparencyFactor = (dot(zoneColorData.rgb, 0.33));
				zoneColorData.a = lerp(zoneColorData.a, 0, colorTransparencyFactor);
	
				zoneColorData.rgb = lerp(zoneColorData.rgb, zoneColorData.rgb * 0.35, saturate(zoneColorData.a * zoneColorData.a + zoneColorData.a * 2));
				zoneColorData.a = saturate(zoneColorData.a * 2);
				zoneColorData.a *= 1 - saturate((KWS_WaterLevel - 1 - worldPos.y) / (DYNAMIC_WAVE_COLOR_MAX_TRANSPARENT * 2));

				colorOverrideStrength = max(colorOverrideStrength, zoneColorData.a * 3);

				diffuseColor.rgb = lerp(diffuseColor.rgb, zoneColorData.rgb, zoneColorData.a);
			
			#endif
		#endif
		

		#if defined(KWS_URP) || defined(KWS_HDRP)
			reflectionStrength *= lerp(0.4, 0.85, underwaterFade);
		
		#endif

	
	
		wetMap = wetMap * lerp((1-KWS_Pow5(1-borderFade)), 1, KWS_RenderOcean);
		if (wetMap < 0.001) return;
	
		occlusion = saturate(wetMap * 0.7 * KWS_WetStrength);
		smoothness = saturate(wetMap * reflectionStrength * (1 - KWS_Pow2(1 - KWS_WetStrength)));
		metallic = saturate(wetMap * 0.65) + colorOverrideStrength;

	#endif
	
}

inline float2 ComputeUvDistortion(float3 worldPos, float2 uv, float strength)
{
	#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES)
	
		float3 dynamicWavesMapUV = GetDynamicWavesMapUV(worldPos, GetWorldToCameraDistanceXZ(worldPos));
		if (!IsOutsideUvBorders(dynamicWavesMapUV.xy))
		{
			float4 dynamicWaves = GetDynamicWavesMap(dynamicWavesMapUV);
			float2 flowDirectionNormalized = NormalizeDynamicWavesVelocity(dynamicWaves.xy);
			uv += flowDirectionNormalized * strength;
		}
	
	#endif
	return uv;
}



////////////////////////////// shadergraph support /////////////////////////////////////////////////////////////////////

inline void GetSurfaceLight_float(float2 screenUV, out float3 result)
{
	#ifdef SHADERGRAPH_PREVIEW
	result = 0;
	#else
	
	result = GetVolumetricSurfaceLight(screenUV);
		
	#endif
}

inline void ComputeUvDistortion_float(float3 worldPos, float2 uv, float strength, out float2 result)
{
	#ifdef SHADERGRAPH_PREVIEW
		result = 0;
	#else
	
	result = ComputeUvDistortion(worldPos, uv, strength);
		
	#endif
}


inline void GetDecalVertexOffset_float(float3 worldPos, float displacement, out float3 result)
{
	#ifdef SHADERGRAPH_PREVIEW
		result = 0;
	#else
	
		worldPos.y = KWS_WaterLevel;
		WaterOffsetData waterData = ComputeWaterOffset(worldPos);
		result = worldPos + waterData.offset;
		
	#endif
}



inline void GetDecalDepthTest_float(float4 screenPos, out float result)
{
	#ifdef SHADERGRAPH_PREVIEW
		result = 1;
	#else
		float sceneDepth = GetSceneDepth(screenPos.xy / screenPos.w);
		result = LinearEyeDepthUniversal(sceneDepth) > LinearEyeDepthUniversal(screenPos.z / screenPos.w);
	#endif
}


inline void TileWarpParticlesOffsetXZ_float(float3 vertex, float3 center, out float3 result)
{
	#ifdef SHADERGRAPH_PREVIEW
	result = 1;
	#else
	result = TileWarpParticlesOffsetXZ(vertex, center);
	#endif
	
}


inline void GetWaterSurfaceCollisionForQuadParticles_float(float3 vertex, float3 center, out float3 result)
{
	result = GetWaterSurfaceCollisionForQuadParticles(vertex, center);
}

inline void GetWaterSurfaceCollisionForQuadParticlesAquarium_float(float3 vertex, float3 center, float levelOffset, out float3 result)
{
	result = GetWaterSurfaceCollisionForQuadParticlesAquarium(vertex, center, levelOffset);
}

void GetDynamicWavesFoamParticlesVertexPosition_float(uint instanceID, uint vertexID, out float3 vertex, out float2 uv) //shadergraph function

{
	#ifdef SHADERGRAPH_PREVIEW
		vertex = 1;
		uv = 1;
	#else

		vertex = 1;
		uv = 1;
	#endif
}



void GetWetnessData_float(float2 screenUV, float2 uv, float sceneDepth, out float3 diffuseColor, out float wetMap, out float occlusion, out float smoothness, out float metallic) //shadergraph function

{
	#ifdef SHADERGRAPH_PREVIEW
		diffuseColor = float3(0, 0, 0);
		wetMap = 1;
		occlusion = 0;
		smoothness = 0;
		metallic = 0;
	#else
		GetWetnessData(screenUV, uv, sceneDepth, diffuseColor, wetMap, occlusion, smoothness, metallic);
	#endif
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




#endif