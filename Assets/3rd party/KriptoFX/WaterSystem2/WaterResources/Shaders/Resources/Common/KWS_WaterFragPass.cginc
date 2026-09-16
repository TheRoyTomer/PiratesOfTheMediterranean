#ifndef KWS_WATER_FRAG_PASS
#define KWS_WATER_FRAG_PASS



half4 fragWater(v2fWater i) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	float2 screenUV = i.screenPos.xy / i.screenPos.w;
	float3 viewDir = GetWorldSpaceViewDirNorm(i.worldPosRefracted);
	float surfaceDepthZ = i.screenPos.z / i.screenPos.w;
	float surfaceDepthZEye = LinearEyeDepthUniversal(surfaceDepthZ);
	float sceneZ = GetSceneDepth(screenUV);
	float sceneZEye = LinearEyeDepthUniversal(sceneZ);
	float distanceToCamera = GetWorldToCameraDistance(i.worldPos);
	
	float3 sceneWorldPos =  GetWorldSpacePositionFromDepth(screenUV, sceneZ);
	float waterDepthRaw = i.worldPosRefracted.y - sceneWorldPos.y;
	float waterDepth = lerp(clamp(abs(surfaceDepthZEye - sceneZEye) * 0.1, 0, 10), waterDepthRaw, 1-KWS_Pow10(1-viewDir.y));
	
	//half surfaceMask = max(i.surfaceMask.x >= 0.999, i.surfaceMask.x <= 0.0001);
	half surfaceMask = (i.surfaceMask.x >= 0.999 || i.surfaceMask.x <= 0.0001) ? 1.0h : 0.0h;
	half exposure = GetExposure();
	half alpha = 1;
	
	#ifdef KWS_USE_CLIP_MASKING
		if (ShouldClipWaterSurface(screenUV, surfaceDepthZ)) discard;
	#endif
	
	float3 turbidityColor = KWS_TurbidityColor;
	float3 waterColor = KWS_WaterColor;
	float transparent = KWS_Transparent;
	float localZoneDensity = 0;

	bool isDynamicWavesZone = false;

	float4 foamData = float4(1.0, 2.25, 1.0, 1.0);

	float shorelineMask = 1;
	float riverMask = 0;
	float waterfallMask = 0;
	float foamMask = 0;

	float2 flowDirection = 0;
	float3 dynamicWavesNormal = float3(0, 1, 0);
	float borderFade = 0;
	float dynamicWavesNormalMask = 0;
	float flowSpeedMultiplier = 1;
	float zoneTimeScale = 1;
	float velocityLength = 0;
	float dynamicWavesHeight = 0;
	float dynamicWavesFoamAlpha = 1;
	float rainDropIntensity = 0;
	float rainMask = 0;
	
	float4 colorData = float4(0, 0, 0, 0);

	#if defined(KWS_USE_LOCAL_WATER_ZONES)
		
		
		UNITY_LOOP
		for (uint zoneIdx = 0; zoneIdx < KWS_WaterLocalZonesCount; zoneIdx++)
		{
			LocalZoneData zone = KWS_ZoneData_LocalZone[zoneIdx];
				
			// if (zone.cutoutMode > 0)
			// {
			// 	float3 distanceToBox = abs(i.worldPosRefracted.xyz - zone.center.xyz) / zone.halfSize.xyz;
			// 	float distanceToBorder = KWS_MAX(saturate(distanceToBox));
			// 	float cutoutFadeFactor = saturate(smoothstep(0, zone.cutoutFadeFactor * 0.5, 1 - distanceToBorder));
			// 	cutoutFadeFactor = 1 - KWS_Pow2(1 - cutoutFadeFactor);
			// 	alpha = lerp(1 - cutoutFadeFactor, cutoutFadeFactor, zone.cutoutMode == 2);
			// 	//return float4(distanceToBorder, 0, 0, 1);
			// }

			if (zone.overrideHeight > 0.5 && zone.clipWaterBelowZone)
			{
				float2 distanceToBox = abs(mul(i.worldPos.xz - zone.center.xz, zone.rotationMatrix)) / zone.halfSize.xz;
			
				float distanceToBorder = (float)max(distanceToBox.x, distanceToBox.y);
				float zoneMinHeight = zone.center.y - zone.halfSize.y;

				if (distanceToBorder < 1.1 && i.worldPosRefracted.y < zoneMinHeight) discard;

				localZoneDensity = 1;
			}
			
	        if (zone.overrideColorSettings > 0.5)
			{

				float tEntry=0;
			
				float3 sceneWorldPos = GetWorldSpacePositionFromDepth(screenUV, sceneZ);
				float3 surfaceOffset = float3(0, (float)max(0, zone.center.y + zone.halfSize.y - i.worldPosRefracted.y) * 0.5f, 0);
					
	        	float density = 0;
	        	if (zone.useSphereBlending > 0.5)
	        	{
	        		density = KWS_SDF_SphereDensity(i.worldPosRefracted,  normalize(sceneWorldPos - i.worldPosRefracted), zone.center.xyz, zone.halfSize.x,  length(i.worldPosRefracted - sceneWorldPos), tEntry);
	        	}
	        	else
	        	{
	        		float2 boxSDF = KWS_SDF_IntersectionBox(i.worldPosRefracted, normalize(sceneWorldPos - i.worldPosRefracted), zone.rotationMatrix, zone.center, zone.halfSize);
	        		density = boxSDF.x < boxSDF.y && boxSDF.y > 0 && boxSDF.x <  length(i.worldPosRefracted - sceneWorldPos);
	        		tEntry = boxSDF.x;
	        	}
	        	
				if (density > 0)
				{
					density = saturate(density * 2);
					density = lerp(0, density, saturate(transparent / (float)max(1, tEntry)));
		
					transparent = lerp(transparent, zone.transparent, density);
					turbidityColor = lerp(turbidityColor, zone.turbidityColor.xyz, density);
					waterColor = lerp(waterColor, zone.waterColor.xyz, density);
					localZoneDensity = lerp(localZoneDensity, 1, density);
				}
			}
		}
		
			
		
	#endif

	if (alpha < 0.001) return 0;
	
	float4 advectedUV = 0;
	bool useAdvectedUV = false;
	bool useDynamicWavesFoamTexture = false;

	#ifdef KWS_USE_ZONE_INSTANCE

			isDynamicWavesZone = true;
			float4 dynamicWaves = GetDynamicWavesZone(i.uv); 

			float4 dynamicWavesAdditionalData = GetDynamicWavesZoneAdditionalData(i.uv); //(wetmap, shoreline mask, foam mask, wetDepth)
			float3 zoneNormal = i.worldNormal;
			float zoneFade = GetDynamicWavesBorderFading(i.uv);

			zoneNormal = lerp(float3(0, 1, 0), zoneNormal, zoneFade);
			flowSpeedMultiplier *= KWS_DynamicWavesZoneFlowSpeedMultiplier;
			zoneTimeScale *= KWS_SimulationTimeScale;

			dynamicWavesFoamAlpha *= KWS_DynamicWavesZoneFoamData.w;
			foamData.xyz = KWS_DynamicWavesZoneFoamData.xyz;
			foamData.w = foamData.z *(KWS_DynamicWavesZoneSize.z / KWS_DynamicWavesZoneSize.x);
			useDynamicWavesFoamTexture = KWS_DynamicWavesUseFoamTexture;
			useAdvectedUV = false; //baked mode can't use advected textures
			rainDropIntensity = KWS_DynamicWavesRainDropsIntensity;
	
			float waterfallThreshold = GetDynamicWavesWaterfallTreshold(zoneNormal) * zoneFade;
			dynamicWaves.xy = lerp(dynamicWaves.xy, dynamicWaves.xy * 0.2, waterfallThreshold);
			
			flowDirection = (flowDirection + dynamicWaves.xy);
			shorelineMask = max(dynamicWavesAdditionalData.y, 1.0 - zoneFade);
			dynamicWavesNormal = KWS_BlendNormals(dynamicWavesNormal, zoneNormal);
			velocityLength += length(dynamicWaves.xy);
			dynamicWavesHeight += dynamicWaves.z;
			
			borderFade = saturate(borderFade + zoneFade);
			
			foamMask = max(foamMask, dynamicWavesAdditionalData.z * zoneFade);
			waterfallThreshold *= exp(-dynamicWaves.z * 0.35);
			float foamWaveThreshold = 1-saturate(lerp(0.5, saturate(waterfallThreshold * 5), shorelineMask));

			waterfallMask = saturate(waterfallMask + waterfallThreshold);
			
			#ifdef KWS_USE_COLORED_DYNAMIC_WAVES
				float4 zoneColorData = GetDynamicWavesZoneColorData(i.uv);
				float colorTransparencyFactor = (dot(zoneColorData.rgb, 0.33));
				zoneColorData.a = lerp(zoneColorData.a, 0, colorTransparencyFactor);
	
				zoneColorData.rgb = lerp(zoneColorData.rgb, zoneColorData.rgb * 0.35, saturate(zoneColorData.a * zoneColorData.a + zoneColorData.a * 2));
				zoneColorData.a = saturate(zoneColorData.a * 2);
	
				turbidityColor = lerp(turbidityColor, zoneColorData.rgb, zoneColorData.a);
				waterColor = lerp(waterColor, zoneColorData.rgb, zoneColorData.a);
				transparent = lerp(transparent, DYNAMIC_WAVE_COLOR_MAX_TRANSPARENT, zoneColorData.a);

				colorData = max(colorData, zoneColorData);
			#endif

			
			velocityLength *= KWS_Pow2(borderFade);
			dynamicWavesNormalMask = saturate(dynamicWavesHeight * 0.5 * velocityLength) * KWS_Pow3(borderFade);
			flowDirection *= KWS_Pow2(borderFade);
		
	#endif

		
	#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES)
	
		float3 dynamicWavesMapUV = GetDynamicWavesMapUV(i.worldPosRefracted, GetWorldToCameraDistanceXZ(i.worldPos));
		//return float4(dynamicWavesMapUV.z / 7.0, 0, 0, 1);
		isDynamicWavesZone = !IsOutsideUvBorders(dynamicWavesMapUV.xy);
		if (isDynamicWavesZone)
		{
			float4 dynamicWaves = GetDynamicWavesMap(dynamicWavesMapUV);
			DynamicWavesAdditionalData additionalData = GetDynamicWavesAdditionalMap(dynamicWavesMapUV);
			dynamicWavesNormal = GetDynamicWavesNormalMap(dynamicWavesMapUV);
			float waterfallThreshold = GetDynamicWavesWaterfallTreshold(dynamicWavesNormal);
			dynamicWaves.xy = lerp(dynamicWaves.xy, dynamicWaves.xy * 0.2, waterfallThreshold);
			
			//return float4(abs(dynamicWaves.xy) * 0.25, 0, 1);
			
			//return float4(additionalData.foamMask, 0.0, 0.0, 1);
			
			borderFade = additionalData.zoneFade;
			waterfallThreshold *= exp(-dynamicWaves.z * 0.35);
			waterfallMask = saturate(waterfallMask + waterfallThreshold);
			foamMask = additionalData.foamMask * borderFade;
			flowDirection = dynamicWaves.xy * borderFade;
			shorelineMask = max(additionalData.shorelineMask, 1.0-borderFade);
			velocityLength = length(dynamicWaves.xy) * borderFade;
			dynamicWavesHeight = dynamicWaves.z;

		
			
			ZoneData zone = KWS_ZoneData[additionalData.zoneID];
			useAdvectedUV = zone.useAdvectedUV;
			useDynamicWavesFoamTexture = zone.useFoamTexture;
			flowSpeedMultiplier *= zone.flowSpeedMultiplier;
			zoneTimeScale *= zone.timeScale;

			dynamicWavesFoamAlpha *= zone.foamData.w;
			foamData.xyz = zone.foamData.xyz;
			foamData.w = foamData.z * (zone.halfSize.z / zone.halfSize.x);
			foamMask *= useDynamicWavesFoamTexture;
			//foamMask *= 1-saturate(3 * waterfallThreshold);
			if (zone.useAdvectedUV) advectedUV = GetDynamicWavesAdvectedUVMap(dynamicWavesMapUV);
			rainDropIntensity = zone.rainDropsIntensity;

			#ifdef KWS_USE_COLORED_DYNAMIC_WAVES
				float4 zoneColorData = GetDynamicWavesColorMap(dynamicWavesMapUV);
				float colorTransparencyFactor = (dot(zoneColorData.rgb, 0.33));
				zoneColorData.a = lerp(zoneColorData.a, 0, colorTransparencyFactor);
			
				zoneColorData.rgb = lerp(zoneColorData.rgb, zoneColorData.rgb * 0.35, saturate(zoneColorData.a * zoneColorData.a + zoneColorData.a * 2));
				zoneColorData.a = saturate(zoneColorData.a * 2);
			
				turbidityColor = lerp(turbidityColor, zoneColorData.rgb, zoneColorData.a);
				waterColor = lerp(waterColor, zoneColorData.rgb, zoneColorData.a);
				//transparent = lerp(transparent, DYNAMIC_WAVE_COLOR_MAX_TRANSPARENT, zoneColorData.a);

				colorData = zoneColorData;
			
			#endif
			
		}
	
	#endif

	
	//alpha = lerp(1-KWS_Pow5(1-borderFade), alpha, KWS_RenderOcean);
	alpha = lerp(1-KWS_Pow5(1.0-borderFade), alpha, (half)KWS_RenderOcean);
	if (alpha < 0.001) return 0;
	
	
	float2 flowDirectionNormalized = NormalizeDynamicWavesVelocity(flowDirection);
	
	//return float4(saturate(flowDirection), 0, 1);
	
	float noiseMask = GetNoiseMask(i.worldPos.xz, 1, 1);
	/////////////////////////////////////////////////////////////  NORMAL  ////////////////////////////////////////////////////////////////////////////////////////////////////////
	float3 tangentNormal = float3(0, 1, 0);
	float oceanFoamMask = 0;
	
	
	#if defined(KWS_USE_OCEAN_RENDERING)
	
		float3 wavesNormalFoam = GetFftWavesNormalFoam(i.worldPos, i.windAttenuation);
		tangentNormal = float3(wavesNormalFoam.x, 1, wavesNormalFoam.z);
		tangentNormal = lerp(float3(0, 1, 0), tangentNormal, any(fwidth(i.worldPos.xz))); //fix horizontal flickering
		
		oceanFoamMask = wavesNormalFoam.y * noiseMask;

	#endif
	float3 oldTangent = tangentNormal;
	
	#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES) || defined(KWS_USE_ZONE_INSTANCE)
		if (isDynamicWavesZone && borderFade > 0.0)
		{
			riverMask = GetDynamicWavesRiverMask(i.worldPosRefracted.y, shorelineMask);
			KWS_BlendDynamicWavesNormals(riverMask,	i.worldPos.xz, shorelineMask, flowDirectionNormalized, velocityLength, zoneTimeScale, flowSpeedMultiplier, dynamicWavesNormal, tangentNormal);
		}
		
	#endif
	
	rainDropIntensity *= borderFade;
	
	#if defined(KWS_USE_OCEAN_RENDERING)
		if (KWS_UseOceanFoam)
		{
			//return float4(wavesNormalFoam.yyy, 1);
			foamMask = max(foamMask, oceanFoamMask * surfaceMask * shorelineMask * (1-borderFade) * KWS_Pow3(i.windAttenuation));
			
		}
		rainDropIntensity = lerp(KWS_OceanRainDropsIntensity, rainDropIntensity, borderFade);
	#endif
	
	if (rainDropIntensity > 0.0)
	{
		float3 rainNormal = GetRainMap(i.worldPos.xz * 0.1, KWS_Time, float4(0.75, 0.85, 1.0, 0.8), rainDropIntensity);
		tangentNormal = KWS_BlendNormals(tangentNormal, rainNormal);
		rainMask = saturate(abs(rainNormal.x) + abs(rainNormal.z));
		//waterDepth += rainMask * 0.5;
	}
	

	tangentNormal = lerp(float3(0, 1, 0), tangentNormal, surfaceMask * alpha);
	#ifdef KWS_USE_ZONE_INSTANCE
		float3 worldNormal = tangentNormal;
	#else
		float3 worldNormal = KWS_BlendNormals(tangentNormal, i.worldNormal);
	#endif

	
	//worldNormal = KWS_GetDerivativeNormal(i.worldPosRefracted, _ProjectionParams.x);
	//return float4(worldNormal, 1);

	/////////////////////////////////////////////////////////////  end normal  ////////////////////////////////////////////////////////////////////////////////////////////////////////



	/////////////////////////////////////////////////////////////////////  REFRACTION  ///////////////////////////////////////////////////////////////////
	
	half3 refraction;
	
	float2 refractionUV;
	//todo surfaceMask > 0.5
	#ifdef KWS_USE_REFRACTION_IOR
		float3 refractionPos = float3(i.worldPos.x, i.worldPosRefracted.y, i.worldPos.z);
		float riverRefractionMultiplier = lerp(1, 3, saturate(dynamicWavesHeight));
		refractionUV = GetRefractedUV_IOR(viewDir, worldNormal, refractionPos, sceneZEye, surfaceDepthZEye, transparent, clamp(waterDepth * riverRefractionMultiplier, 0.1, KWS_MAX_TRANSPARENT));

		/* //todo fix alpha
		float3 refractedWorldPosDepth = GetWorldSpacePositionFromDepth(refractionUV, GetSceneDepth(refractionUV));
		waterDepth = i.worldPosRefracted.y - refractedWorldPosDepth.y;
		waterDepth = lerp(clamp(abs(surfaceDepthZEye - sceneZEye) * 0.1, 0, 10), waterDepth, 1-KWS_Pow10(1-viewDir.y));
		refractionUV = GetRefractedUV_IOR(viewDir, worldNormal, refractionPos, sceneZEye, surfaceDepthZEye, transparent, clamp(waterDepth * riverRefractionMultiplier, 0.1, KWS_MAX_TRANSPARENT));
		*/
	
	#else
		refractionUV = GetRefractedUV_Simple(screenUV, worldNormal);
	#endif
	refractionUV = lerp(screenUV, refractionUV, surfaceMask);
	refractionUV += waterfallMask * clamp(flowDirectionNormalized * 0.5, -0.25, 0.25) * lerp(1.0, RIVER_FLOW_FRESNEL_MULTIPLIER * 0.5, riverMask) * (1.1-saturate(distanceToCamera * 0.01)) * saturate(waterDepth * 0.5);


	float refractedSceneZ = GetSceneDepth(refractionUV);
	float refractedSceneZEye = LinearEyeDepthUniversal(refractedSceneZ);
	
	FixRefractionSurfaceLeaking(surfaceDepthZEye, sceneZ, sceneZEye, screenUV, refractedSceneZ, refractedSceneZEye, refractionUV);
	
	//todo surfaceMask > 0.5
	#ifdef KWS_USE_REFRACTION_DISPERSION
		refraction = GetSceneColorWithDispersion(refractionUV, KWS_RefractionDispersionStrength);
	#else
		refraction = GetSceneColor(refractionUV);
	#endif
	
	float foamParticlesIntensity = 0;
	/*
	if (KWS_IsAnyZoneUseFoamParticles == 1)
	{
		float3 foamParticles = GetScreenSpaceFoam(refractionUV).xyz;
		foamParticlesIntensity = saturate(dot(foamParticles, 0.33) * 2);
		refraction = lerp(refraction, foamParticles * float3(0.75, 0.85, 1) * 0.75, foamParticlesIntensity);
	
		float particlesFakeDepth = EyeDepthToRawUniversal(surfaceDepthZEye + lerp(0.05, 0.5, saturate(transparent * 0.1)));
		refractedSceneZ = lerp(refractedSceneZ, particlesFakeDepth, foamParticlesIntensity);
	}
	*/
	//refraction *= float3(0.85, 0.87, 0.9);
	//return float4(refraction, 1);
	/////////////////////////////////////////////////////////////  end refraction  ////////////////////////////////////////////////////////////////////////////////////////////////////////


	
	/////////////////////////////////////////////////////////////////////  CAUSTIC  ///////////////////////////////////////////
	
	#if defined(KWS_USE_CAUSTIC)

		float3 refractedWorldPos = GetWorldSpacePositionFromDepth(refractionUV, refractedSceneZ);
		float2 causticUV = refractedWorldPos.xz;
		
		float3 oceanCaustic = 0;
		float3 zoneCaustic = 0;
		float zoneCausticFade = KWS_Pow2(borderFade);

	
		#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES) || defined(KWS_USE_ZONE_INSTANCE)
			if (isDynamicWavesZone && zoneCausticFade > 0.0)
			{
				float causticNoiseMask = GetNoiseMask(causticUV, 2.5, 7);
				causticNoiseMask = saturate(noiseMask * causticNoiseMask + 0.1 + velocityLength * 0.25);

				//todo add high quality option
				/*
				float3 dynamicWavesMapUVRefracted = GetDynamicWavesMapUV(refractedWorldPos, distanceToCamera);
				float4 dynamicWavesRefracted = GetDynamicWavesMap(dynamicWavesMapUVRefracted);
				
				float2 flowDirectionRefractedNormalized = NormalizeDynamicWavesVelocity(dynamicWavesRefracted.xy);
				float velocityLengthRefracted = length(dynamicWavesRefracted.xy);
				*/
				float2 flowDirectionRefractedNormalized = flowDirectionNormalized;
				float velocityLengthRefracted = velocityLength;
				zoneCaustic = GetDynamicWavesZoneCaustic(causticUV, waterDepth, flowDirectionRefractedNormalized, velocityLengthRefracted, flowSpeedMultiplier * zoneTimeScale, causticNoiseMask);
		
			}
		#endif

		#if defined(KWS_USE_OCEAN_RENDERING)
			if (zoneCausticFade < 0.95) oceanCaustic = GetOceanCaustic(i.pos.xy, causticUV, waterDepth);
		#endif

		float3 finalCaustic = lerp(oceanCaustic.xyz, zoneCaustic.xyz, zoneCausticFade);
		
		float depthFadingDistance = saturate(waterDepth * 0.1);
		finalCaustic *= lerp(saturate(1-saturate((distanceToCamera + 25) * 0.01) + 0.25), 1, saturate(depthFadingDistance));
	
		#ifdef KWS_USE_VOLUMETRIC_LIGHT
				VolumetricLightAdditionalData volumeData = GetVolumetricLightAdditionalData(screenUV);
				finalCaustic *= lerp(1, KWS_Pow10(volumeData.SceneDirShadow), KWS_DisableCausticInShadow);
		#endif
	
		finalCaustic = lerp(finalCaustic, 0, foamParticlesIntensity);

		refraction.rgb *= 1.0 + finalCaustic; // caustic ~ 0.0–1.0
	#endif
	/////////////////////////////////////////////////////////////  end caustic  ///////////////////////////////////////////////


	
	
	/////////////////////////////////////////////////////////////////////  UNDERWATER  ///////////////////////////////////////////////////////////////////

	float2 refractedVolumeLightUV = lerp(refractionUV, screenUV, waterfallMask);
	
	#if defined(KWS_USE_LOCAL_WATER_ZONES)
		refractedVolumeLightUV = lerp(refractedVolumeLightUV, screenUV, localZoneDensity);
	#endif

	
	#ifdef KWS_USE_CUSTOM_MESH
		refractedVolumeLightUV = screenUV;
	#endif
	
	float2 volumeDepth = GetWaterVolumeDepth(screenUV, refractedSceneZ, 0, surfaceDepthZ);
	half4 volumeLight = GetVolumetricLightWithAbsorbtion(screenUV, refractedVolumeLightUV, transparent, turbidityColor, waterColor, refraction, volumeDepth, exposure, 0);
	//return float4(volumeLight.xyz, 1);
	
	if (surfaceMask < 0.5) return float4(volumeLight.xyz, 1);
	
	float depthAngleFix = (surfaceMask < 0.5 || KWS_MeshType == KWS_MESH_TYPE_CUSTOM_MESH) ?                          0.25 : saturate(GetWorldSpaceViewDirNorm(i.worldPos - float3(0, KWS_WindSpeed * 0.5, 0)).y);
	float fade = GetWaterRawFade(i.worldPos, surfaceDepthZEye, refractedSceneZEye, surfaceMask, depthAngleFix);
	half3 underwaterColor = volumeLight.xyz;

			
	float oceanFoam = 0;
	float advectedFoam = 0;
	float flowmapFoam = 0;

	#if defined(KWS_USE_OCEAN_RENDERING)
		UNITY_BRANCH
		if(KWS_UseOceanFoam) //todo add KWS_UseOceanFoam keyword
		{
			float2 oceanUV = i.worldPos.xz * 0.05 * KWS_FoamTextureScaleMultiplier;
			float2 dx = ddx(oceanUV);
			float2 dy = ddy(oceanUV);
			float4 oceanFoamTex = Tex2DStochastic(KW_FluidsFoamTex, sampler_KW_FluidsFoamTex, oceanUV, dx, dy).xyzw * (1.0-borderFade);
			float4 foamChannelMask = saturate(1.0 - abs(KWS_FoamTextureType - float4(0, 1, 2, 3)));
			float selectedFoamChannel = dot(oceanFoamTex, foamChannelMask);
			oceanFoam = saturate(saturate(pow(selectedFoamChannel, KWS_FoamTextureContrast) - (1 - sqrt(foamMask))));
		}
	#endif
	
	#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES) || defined(KWS_USE_ZONE_INSTANCE)
	UNITY_BRANCH
	if (useDynamicWavesFoamTexture)
	{
		float foamTextureType = foamData.x;
		float foamTextureContrast = foamData.y;
		float2 foamTextureScale = foamData.zw;
		//return float4(saturate(foamMask), 0,0 , 1);
		foamTextureContrast = max(0, lerp(foamTextureContrast, foamTextureContrast * 0.35, foamMask));
		
		float4 foamChannelMask = saturate(1.0 - abs(foamTextureType - float4(0, 1, 2, 3)));

		UNITY_BRANCH
		if (useAdvectedUV)
		{
			float4 advectedFoam0 = KW_FluidsFoamTex.Sample(sampler_KW_FluidsFoamTex, advectedUV.xy * 20 * foamTextureScale);
			float4 advectedFoam1 = KW_FluidsFoamTex.Sample(sampler_KW_FluidsFoamTex,  advectedUV.zw * 20 * foamTextureScale);

			float selectedFoamChannel0 = dot(advectedFoam0, foamChannelMask);
			float selectedFoamChannel1 = dot(advectedFoam1, foamChannelMask);


			float weight0 = sin(frac((KWS_Time * zoneTimeScale) / KWS_ADVECTED_UV_REPEAT_TIME) * 3.1415);
			float weight1 = sin(frac(((KWS_Time * zoneTimeScale) + KWS_ADVECTED_UV_REPEAT_TIME * 0.5) / KWS_ADVECTED_UV_REPEAT_TIME) * 3.1415);

			weight0 = sqrt(weight0);
			weight1 = sqrt(weight1);
		
			advectedFoam = max(selectedFoamChannel0 * weight0, selectedFoamChannel1 * weight1) * borderFade;
			advectedFoam = saturate(saturate(pow(advectedFoam, foamTextureContrast) - (1 - sqrt(foamMask))));
		}
		else
		{
			float flowSpeed = lerp(0.25, 0.75, riverMask);
	
			float4 flowmapFoamTex = Texture2DSampleFlowmapJump(KW_FluidsFoamTex, sampler_KW_FluidsFoamTex, i.worldPos.xz * 0.05, flowDirectionNormalized * flowSpeed, (KWS_Time * zoneTimeScale) * flowSpeedMultiplier * 0.5, foamTextureScale.x).xyzw * borderFade;
			float selectedFoamChannel = dot(flowmapFoamTex, foamChannelMask);
			flowmapFoam = saturate(saturate(pow(selectedFoamChannel, foamTextureContrast) - (1 - sqrt(foamMask))));
			
		}
	
	}
	#endif

	#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES) || defined(KWS_USE_ZONE_INSTANCE) || defined(KWS_USE_OCEAN_RENDERING)

		UNITY_BRANCH
		if (useDynamicWavesFoamTexture || KWS_UseOceanFoam)
		{
			float3 surfaceColor = GetVolumetricSurfaceLight(screenUV);
			float3 foamColor = 0;
			
			foamColor += advectedFoam * dynamicWavesFoamAlpha;
			foamColor += flowmapFoam * dynamicWavesFoamAlpha;
			foamColor += oceanFoam;
			
			foamColor += foamMask * 0.25 * KWS_TurbidityColor * borderFade * saturate(KWS_MAX_TRANSPARENT_INVERSE * transparent * 3);
			foamColor = saturate(foamColor) * KWS_Pow3(alpha) * saturate(waterDepth * 10);
			
			foamColor *= clamp(surfaceColor, 0, 1.15);
			foamColor *= lerp(float3(1.0, 1.0, 1.0), colorData.rgb, saturate(colorData.a * 1.35));
			
			underwaterColor = lerp(underwaterColor + foamColor, foamColor, colorData.a);

		}
	
	#endif
	
	
	float3 subsurfaceScatteringColor = ComputeSSS(screenUV, GetWaterSSS(screenUV), underwaterColor, volumeLight.a, transparent);
	underwaterColor += subsurfaceScatteringColor * 5;

	//return float4(underwaterColor.xyz, 1);
	/////////////////////////////////////////////////////////////  end underwater  ////////////////////////////////////////////////////////////////////////////////////////////////////////


	/////////////////////////////////////////////////////////////  REFLECTION  ////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	float3 reflDir = reflect(-viewDir, worldNormal);
	reflDir.y *= sign(dot(reflDir, float3(0, 1, 0)));

	float3 reflection = 0;

	#if defined(KWS_SSR_REFLECTION) || defined(KWS_USE_PLANAR_REFLECTION)
		float2 refl_uv = GetScreenSpaceReflectionUV(reflDir, screenUV + tangentNormal.xz * 0.5);
	#endif

	
	#ifdef KWS_USE_PLANAR_REFLECTION
		reflection = GetPlanarReflectionWithClipOffset(refl_uv) * exposure;
	#else

		#ifdef KWS_USE_REFLECTION_PROBES
			reflection = KWS_GetReflectionProbeEnv(screenUV, surfaceDepthZEye, i.worldPosRefracted, reflDir, KWS_SkyLodRelativeToWind, exposure);
		#else
			reflection = KWS_GetSkyColor(reflDir, KWS_SkyLodRelativeToWind, exposure);
		#endif

	#endif

	#ifdef KWS_SSR_REFLECTION
		float4 ssrReflection = GetScreenSpaceReflectionWithStretchingMask(refl_uv, i.worldPosRefracted);

		float inverseReflectionFix = 1 - saturate(dot(reflDir, float3(0, 1, 0)));
		inverseReflectionFix = lerp(1, KWS_Pow5(inverseReflectionFix), saturate((KWS_WindSpeed - 2.5) * 0.25));
		inverseReflectionFix = lerp(inverseReflectionFix, 1, riverMask);
		ssrReflection.a = lerp(0, ssrReflection.a, inverseReflectionFix);
	
		reflection = lerp(reflection, ssrReflection.rgb, ssrReflection.a);

	#endif
	
	
	reflection *= surfaceMask * (1.0 - waterfallMask);
	//reflection = ApplyShorelineWavesReflectionFix(reflDir, reflection, underwaterColor);


	/////////////////////////////////////////////////////////////  end reflection  ////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	
	
	half waterFresnel = ComputeWaterFresnel(worldNormal, viewDir);
	waterFresnel *= lerp(1.0, RIVER_FLOW_FRESNEL_MULTIPLIER, riverMask);
	waterFresnel *= surfaceMask * (1.0 - waterfallMask);
	
	half3 finalColor = lerp(underwaterColor, reflection, waterFresnel);
	
	

	#ifdef KWS_REFLECT_SUN
		float3 sunReflection = ComputeSunlight(worldNormal, viewDir, GetMainLightDir(), GetMainLightColor(exposure), volumeLight.a, surfaceDepthZEye, _ProjectionParams.z, transparent);
		float sunFadeFactor = saturate(abs(sceneZEye - surfaceDepthZEye));
		sunFadeFactor = KWS_Pow5(sunFadeFactor) * saturate(1 - waterfallMask * 2.0);
		finalColor += sunReflection * saturate((1 - foamMask) * sunFadeFactor);

	#endif
	
	half3 fogColor;
	half3 fogOpacity;

	#ifdef KWS_HDRP
	GetInternalFogVariables(i.pos, GetWorldSpaceViewDirNorm(i.worldPos), LinearEyeDepthUniversal(i.pos.z), i.worldPos, fogColor, fogOpacity);
	#else
	GetInternalFogVariables(LinearEyeDepthUniversal(i.pos.z), fogColor, fogOpacity);
	#endif
                
	finalColor.xyz = ComputeInternalFog(finalColor.xyz , fogColor, fogOpacity);
	finalColor.xyz  = ComputeThirdPartyFog(finalColor.xyz , i.worldPos, i.pos.xy / _ScreenParams.xy, i.pos.z);
	

	finalColor += srpBatcherFix;

	alpha *= saturate(waterDepth * 5);
	alpha = lerp(alpha, 1, rainMask);

	return float4(finalColor, alpha);
}


struct WaterFragmentOutput
{
	half4 pass1 : SV_Target0;
	half2 pass2 : SV_Target1;
};

WaterFragmentOutput  fragDepth(v2fDepth i,  bool face : SV_IsFrontFace)
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
	WaterFragmentOutput o = (WaterFragmentOutput)0;
	
	float front = face > 0 ? 1.0 : 0.0;
	
	#if defined(SHADER_API_XBOXONE)
		//front = dot(i.worldNormal, GetWorldSpaceViewDirNorm(i.worldPos)) > 0 ? 1.0 : 0.0;
	#endif
	

	float facingColor = 0.75 - front * 0.25;
	float2 screenUV = i.screenPos.xy / i.screenPos.w;
	float surfaceDepthZ = i.screenPos.z / i.screenPos.w;
	
	//half surfaceMask = max(i.surfaceMask.x >= 0.999, i.surfaceMask.x <= 0.0001);
	half surfaceMask = (i.surfaceMask.x >= 0.999 || i.surfaceMask.x <= 0.0001) ? 1.0h : 0.0h;
	
	
	#ifdef KWS_USE_CLIP_MASKING
		if (ShouldClipWaterSurface(screenUV, surfaceDepthZ))
		{
			discard;
		}
	#endif
	
	
	float distanceToCamera = GetWorldToCameraDistance(i.worldPos);
	//if (KWS_SDF_Sphere(i.worldPos - Test4.xyz, Test4.w) < 0) discard;
	
	#ifdef KWS_PRE_PASS_BACK_FACE
		float mask = i.surfaceMask.x < 0.9999 ?                   0.1 : 1;
	#else
		//float mask = front > 0 ?                   0.25 : 0.75;
		float mask = lerp(0.75, 0.25, front);
		if (surfaceMask < 0.5)
		{
			//mask = front > 0.0 ?                   0.1 : 1;
			mask = lerp(1.0, 0.1, front);
		}
	#endif


	float2 flowDirection = 0;
	bool isDynamicWavesZone = false;
	
	float3 dynamicWavesNormal = float3(0, 1, 0);
	float dynamicWavesNormalMask = 0;
	float borderFade = 0;
	float flowSpeedMultiplier = 1;
	float zoneTimeScale = 1;
	float velocityLength = 0;
	float shorelineMask = 1;
	float riverMask = 0;
	
	#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES)

	
	float3 dynamicWavesMapUV = GetDynamicWavesMapUV(i.worldPosRefracted, GetWorldToCameraDistanceXZ(i.worldPos));
	isDynamicWavesZone = !IsOutsideUvBorders(dynamicWavesMapUV.xy);
	if (isDynamicWavesZone)
	{
		float4 dynamicWaves = GetDynamicWavesMapBicubic(dynamicWavesMapUV);
		DynamicWavesAdditionalData additionalData = GetDynamicWavesAdditionalMapBicubic(dynamicWavesMapUV);
		dynamicWavesNormal = GetDynamicWavesNormalMap(dynamicWavesMapUV);
		float waterfallThreshold = GetDynamicWavesWaterfallTreshold(dynamicWavesNormal);
		dynamicWaves.xy = lerp(dynamicWaves.xy, dynamicWaves.xy * 0.2, waterfallThreshold);
			
		borderFade = additionalData.zoneFade;
		flowDirection = dynamicWaves.xy * borderFade;
		shorelineMask = max(additionalData.shorelineMask, 1.0-borderFade);
		velocityLength = length(dynamicWaves.xy) * borderFade;

		ZoneData zone = KWS_ZoneData[additionalData.zoneID];
		flowSpeedMultiplier = zone.flowSpeedMultiplier;
		zoneTimeScale = zone.timeScale;
	}
	
	#endif

	
	//float alpha = lerp(1-KWS_Pow5(1-borderFade), 1, KWS_RenderOcean);
	//if (alpha < 0.001)		discard;

	float2 flowDirectionNormalized = NormalizeDynamicWavesVelocity(flowDirection);
	
	/////////////////////////////////////////////////////////////  NORMAL  ////////////////////////////////////////////////////////////////////////////////////////////////////////

	float3 tangentNormal = float3(0, 1, 0);

	#if defined(KWS_USE_OCEAN_RENDERING)
		float3 wavesNormalFoam = GetFftWavesNormalFoam(i.worldPos, i.windAttenuation);
		tangentNormal = float3(wavesNormalFoam.x, 1, wavesNormalFoam.z);
	
		//tangentNormal = lerp(float3(0, 1, 0), tangentNormal, any(fwidth(i.worldPos.xz))); //fix horizontal flickering
		float fw = max(fwidth(i.worldPos.x), fwidth(i.worldPos.z));
		tangentNormal = lerp(float3(0,1,0), tangentNormal, fw > 0 ? 1 : 0);
		
	#endif
	
	float3 tangentNormalScatter = tangentNormal;

	#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES) || defined(KWS_USE_ZONE_INSTANCE)
		if (isDynamicWavesZone && borderFade > 0.0)
		{
			riverMask = GetDynamicWavesRiverMask(i.worldPosRefracted.y, shorelineMask);
			KWS_BlendDynamicWavesNormals(riverMask,	i.worldPos.xz, shorelineMask, flowDirectionNormalized, velocityLength, zoneTimeScale, flowSpeedMultiplier, dynamicWavesNormal, tangentNormal);
		}
		
	#endif

	

	//half surfaceMask = max(i.surfaceMask.x >= 0.999, i.surfaceMask.x <= 0.0001);
	tangentNormal = lerp(float3(0, 1, 0), tangentNormal, surfaceMask);
	float3 worldNormal = KWS_BlendNormals(tangentNormal, i.worldNormal);

	#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES)
		if (isDynamicWavesZone)
		{
			float underwaterRefractionFix = 1 - saturate(1.25 * saturate(1 - dot(dynamicWavesNormal, float3(0, 1, 0))));
			worldNormal *= underwaterRefractionFix;
		}
		
	#endif


	float transparent = KWS_Transparent;

	#if defined(KWS_USE_LOCAL_WATER_ZONES)
		
		UNITY_LOOP
		for (uint zoneIdx = 0; zoneIdx < KWS_WaterLocalZonesCount; zoneIdx++)
		{
			LocalZoneData zone = KWS_ZoneData_LocalZone[zoneIdx];
			
			if (zone.overrideHeight > 0.5 && zone.clipWaterBelowZone)
			{
				float2 distanceToBox = abs(mul(i.worldPos.xz - zone.center.xz, zone.rotationMatrix)) / zone.halfSize.xz;
				float distanceToBorder = (float)max(distanceToBox.x, distanceToBox.y);
				float zoneMinHeight = zone.center.y - zone.halfSize.y;
	
				if (distanceToBorder < 1.1 && i.worldPosRefracted.y < zoneMinHeight && GetCameraAbsolutePosition().y > i.worldPos.y) discard;
			}
		}
			
	#endif

	
	float3 lightDir = GetMainLightDir();
	float3 viewDir = GetWorldSpaceViewDirNorm(i.worldPosRefracted);
	
	float dotVal = dot(lightDir, float3(0, 1, 0));
	float sunAngleAttenuation = smoothstep(-0.1, 1.0, dotVal);

	float3 refractedVector = normalize(refract(-viewDir, tangentNormalScatter, 0.66));
	float scattering = pow(saturate(dot(refractedVector, lightDir)), 16);
	float sss = saturate(scattering * sunAngleAttenuation * 5000);
	
	
	half tensionMask = 0;
	
	/*
	#ifdef KWS_USE_HALF_LINE_TENSION
		
		tensionMask = abs(i.localHeightAndTensionMask.y) * KWS_InstancingWaterScale.y * lerp(40, 10, KWS_UnderwaterHalfLineTensionScale);
		if (tensionMask >= 0.99) tensionMask = ((1.2 - tensionMask) * 5);
		float sceneDepth = GetSceneDepth(screenUV);
		if (i.surfaceMask.x > 0.9999 || front < 0 || surfaceDepthZ <= sceneDepth) tensionMask = 0;
		tensionMask *= 1 - saturate(distanceToCamera * 0.1);
	
	#endif
	*/

	o.pass1 = half4(transparent / 100.0, mask, sss, tensionMask);
	o.pass2 = worldNormal.xz;

	return o;
}

float4 fragPrePassAdditionalMask(v2fDepth i, float face : VFACE) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
	WaterFragmentOutput o = (WaterFragmentOutput)0;
	
	float front = face >= 0 ? 1.0 : 0.0;
	half surfaceMask = (i.surfaceMask.x >= 0.999 || i.surfaceMask.x <= 0.0001) ? 1.0h : 0.0h;
	
	#ifdef KWS_PRE_PASS_BACK_FACE
		float mask = i.surfaceMask.x < 0.9999 ?                   0.1 : 1;
	#else
		//float mask = front > 0 ?                   0.25 : 0.75;
		float mask = lerp(0.75, 0.25, front);
		if (surfaceMask < 0.5)
		{
			//mask = front > 0.0 ?                   0.1 : 1;
			mask = lerp(1.0, 0.1, front);
		}
	#endif
	

	return mask;
}


#endif