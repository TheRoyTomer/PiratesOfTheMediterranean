inline void RayMarchDirLight(RaymarchData raymarchData, inout RaymarchResult result)
{
    result.DirLightScattering = 0;
    result.DirLightSurfaceShadow = 1;
    result.DirLightSceneShadow = 1;
    result.SurfaceLight = 1;

    float3 finalScattering = 0;
    float transmittance = 1;

    Light light = GetMainLight();

    #ifdef _LIGHT_LAYERS
    //if (IsMatchingLightLayer(light.layerMask, KWS_WaterLightLayerMask)) //doesnt work on unity 6+ 
    #endif
    {
        float3 currentPos = raymarchData.rayStart;
        float3 turbidityColor = raymarchData.turbidityColor.xyz;
        float transparent = raymarchData.transparent;
        float waterSurfaceLevel = raymarchData.waterHeight;

        bool isDynamicWavesZone = false;
        #if defined(KWS_USE_DYNAMIC_WAVES)

        float3 dynamicWavesMapUV = GetDynamicWavesMapUV(currentPos, GetWorldToCameraDistance(currentPos));
        if (!IsOutsideUvBorders(dynamicWavesMapUV))
        {
            float4 dynamicWaves = GetDynamicWavesMap(dynamicWavesMapUV);
            DynamicWavesAdditionalData additionalData = GetDynamicWavesAdditionalMapBicubic(dynamicWavesMapUV);
            isDynamicWavesZone = additionalData.zoneFade > 0;
            waterSurfaceLevel += dynamicWaves.z + dynamicWaves.w;
        }
        #endif
        float depthAttenuation = GetVolumeLightInDepthTransmitance(waterSurfaceLevel, transparent);

        float rayLength = GetMaxRayDistanceRelativeToTransparent(transparent);
        float3 step = raymarchData.rayDir * rayLength / (float)KWS_RayMarchSteps;
        currentPos += step * raymarchData.offset;

        float sunAngleAttenuation = GetVolumeLightSunAngleAttenuation(light.direction.xyz);
        finalScattering = GetAmbientColor(GetExposure()) * 0.5;
        finalScattering *= depthAttenuation;
        finalScattering *= sunAngleAttenuation;

        float3 reflectedStep = reflect(raymarchData.rayDir, float3(0, -1, 0)) * (raymarchData.rayLength / KWS_RayMarchSteps);


        UNITY_LOOP
        for (uint i = 0; i < KWS_RayMarchSteps; ++i)
        {
            if (length(currentPos - raymarchData.rayStart) > raymarchData.rayLengthToWaterZ) break;

            float atten = MainLightRealtimeShadow(TransformWorldToShadowCoord(currentPos));

            #if defined(KWS_USE_VOLUMETRIC_DIR_CAUSTIC) || defined(KWS_USE_VOLUMETRIC_FULL_CAUSTIC)
            atten += atten * RaymarchCaustic(raymarchData, currentPos, light.direction, isDynamicWavesZone);
            #endif
            atten *= sunAngleAttenuation;
            atten *= depthAttenuation;

            IntegrateLightSlice(finalScattering, transmittance, atten, rayLength);
            currentPos += step;
        }

        finalScattering *= light.color.xyz * turbidityColor;


        result.DirLightSurfaceShadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(raymarchData.rayStart));
        
        #if defined(KWS_USE_VOLUMETRIC_DIR_CAUSTIC) || defined(KWS_USE_VOLUMETRIC_FULL_CAUSTIC)
        result.DirLightSceneShadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(raymarchData.rayEnd));
        #endif

        result.SurfaceLight = GetAmbientColor(GetExposure()) + light.color.xyz * result.DirLightSurfaceShadow;
    }

    result.DirLightScattering.rgb = finalScattering;
    result.DirLightScattering.a = saturate(1 - transmittance * (1 + KWS_VOLUME_LIGHT_TRANSMITANCE_NEAR_OFFSET_FACTOR));
}

inline void RayMarchAdditionalLights(RaymarchData raymarchData, inout RaymarchResult result)
{
    result.AdditionalLightsScattering = 0;
    result.AdditionalLightsSceneAttenuation = 0;

    #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)

    uint pixelLightCount = KWS_GetAdditionalLightsCount();
    float3 reflectedStep = reflect(raymarchData.rayDir, float3(0, -1, 0)) * (raymarchData.rayLength / KWS_RayMarchSteps);

    float3 totalScattering = 0;
    float totalTransmittance = 1;
    float3 currentPos = raymarchData.currentPos;
    float3 step = raymarchData.step;

    InputData inputData = (InputData)0;

    float sliceDensity = KWS_VOLUME_LIGHT_SLICE_DENSITY / raymarchData.rayLength;
    float sliceTransmittance = exp(-sliceDensity / (float)KWS_RayMarchSteps);


    UNITY_LOOP for (uint i = 0; i < KWS_RayMarchSteps; ++i)
    {
        float traveledDistance = length(currentPos - raymarchData.rayStart);
        if (traveledDistance > raymarchData.rayLengthToSceneZ) break;
        if (traveledDistance > raymarchData.rayLengthToWaterZ) step = reflectedStep;


        inputData.normalizedScreenSpaceUV = raymarchData.uv;
        inputData.positionWS = currentPos;

        float3 stepScattering = 0;

        LIGHT_LOOP_BEGIN(pixelLightCount)

            Light light = GetAdditionalPerObjectLight(lightIndex, currentPos);
            float atten = AdditionalLightRealtimeShadow(lightIndex, currentPos, light.direction) * saturate(light.distanceAttenuation);

            #if defined(KWS_USE_VOLUMETRIC_FULL_CAUSTIC)
            if (currentPos.y > raymarchData.waterHeight)
            {
                atten += atten * RaymarchCaustic(raymarchData, currentPos, light.direction, false);
            }
            #endif

            half cosAngle = dot(-raymarchData.rayDir, light.direction);
            atten += atten * MieScattering(cosAngle) * 5.0;

            stepScattering += atten * light.color;

        LIGHT_LOOP_END

        float3 sliceLightIntegral = stepScattering * (1.0 - sliceTransmittance);

        totalScattering += max(0, sliceLightIntegral * totalTransmittance);
        totalTransmittance *= sliceTransmittance;

        currentPos += step;
        
    }

    

    result.AdditionalLightsScattering.rgb += totalScattering * raymarchData.turbidityColor;
    
    float scatteringEnergy = dot(result.AdditionalLightsScattering.rgb, float3(0.2126, 0.7152, 0.0722));
    result.AdditionalLightsScattering.a = scatteringEnergy > 0.00001 ? saturate(1.0 - totalTransmittance) : 0.0;

    inputData.normalizedScreenSpaceUV = raymarchData.uv;
    inputData.positionWS = raymarchData.rayStart;

    LIGHT_LOOP_BEGIN(pixelLightCount)
    Light light = GetAdditionalPerObjectLight(lightIndex, raymarchData.rayStart);
    result.SurfaceLight += light.color.xyz * AdditionalLightRealtimeShadow(lightIndex, raymarchData.rayStart, light.direction) * saturate(light.distanceAttenuation);
    LIGHT_LOOP_END

    inputData.normalizedScreenSpaceUV = raymarchData.uv;
    inputData.positionWS = raymarchData.rayEnd;

    LIGHT_LOOP_BEGIN(pixelLightCount)
    Light light = GetAdditionalPerObjectLight(lightIndex, raymarchData.rayEnd);
    result.AdditionalLightsSceneAttenuation = max(result.AdditionalLightsSceneAttenuation, saturate(light.distanceAttenuation));
    LIGHT_LOOP_END

    #endif
}
