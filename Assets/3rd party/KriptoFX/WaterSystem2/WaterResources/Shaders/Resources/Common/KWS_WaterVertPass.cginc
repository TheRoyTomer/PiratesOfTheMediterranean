#ifndef KWS_WATER_VERT_PASS
#define KWS_WATER_VERT_PASS

struct waterInput
{
    float4 vertex : POSITION;
    float surfaceMask : COLOR0;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;

    uint instanceID : SV_InstanceID;
};

struct v2fDepth
{
    float4 pos : SV_POSITION;
    float3 worldNormal : NORMAL;
    float2 uv : TEXCOORD0;

    float3 worldPos : TEXCOORD1;
    float3 worldPosRefracted : TEXCOORD2;
    float surfaceMask : COLOR0;
    float windAttenuation : COLOR1;
    float4 screenPos : TEXCOORD3;
    float2 localHeightAndTensionMask : TEXCOORD4;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct v2fWater
{
    float4 pos : SV_POSITION;
    float3 worldNormal : NORMAL;
    float2 uv : TEXCOORD0;

    float surfaceMask : COLOR0;
    float windAttenuation : COLOR1;
    float3 worldPos : TEXCOORD1;
    float3 worldPosRefracted : TEXCOORD2;
    float4 screenPos : TEXCOORD3;

    UNITY_VERTEX_OUTPUT_STEREO
};

struct WaterOffsetData
{
    float3 offset;
    float3 oceanOffset;
    float foamMask;
    float2 flowDirection;
    float orthoDepth;
    float windAttenuation;
    float surfaceMask;
    bool isRequireDiscardTriangle;
};


WaterOffsetData ComputeWaterOffset(float3 worldPos, float2 uv = 0)
{
    WaterOffsetData data = (WaterOffsetData)0;
    data.windAttenuation = 1;
    data.orthoDepth = -100000;
    data.surfaceMask = 1;
    data.isRequireDiscardTriangle = (KWS_RenderOcean == 0);

    float waterLevel = 0;
    float terrainLevel = 0;

    float shorelineMask = 1;


    #if defined(KWS_USE_LOCAL_WATER_ZONES)

    uint zoneIndexOffset_local = 0;
    uint zoneIndexCount_local = 0;
    bool isLocalWaterZone = GetTileRange_LocalZone(worldPos, zoneIndexOffset_local, zoneIndexCount_local);
    float offsetBlending = 0;
    float maxHeightOffset = -100000;

    if (isLocalWaterZone)
    {
        for (uint zoneIndex = zoneIndexOffset_local; zoneIndex < zoneIndexCount_local; zoneIndex++)
        {
            LocalZoneData zone = (LocalZoneData)0;
            if (GetWaterZone_LocalZone(worldPos, zoneIndex, zone))
            {
                if (zone.overrideWindSettings > 0.5)
                {
                    float zoneFade = GetLocalWaterZoneSphereBlendFactor(zone.uv, saturate(zone.windEdgeBlending + 0.25));
                    data.windAttenuation = lerp(lerp(data.windAttenuation, zone.windStrengthMultiplier, zoneFade), zone.windStrengthMultiplier, KWS_Pow10(zone.windEdgeBlending));
                }

                if (zone.overrideHeight > 0.5)
                {
                    float currentHeightOffset = zone.center.y + zone.halfSize.y - KWS_WaterLevel;
                    float heightFade = GetLocalWaterZoneSphereBlendFactor(zone.uv, zone.heightEdgeBlending);
                    maxHeightOffset = max(maxHeightOffset, currentHeightOffset);
                    offsetBlending = lerp(heightFade, 1, KWS_Pow20(zone.heightEdgeBlending));
                    data.surfaceMask = 1 - KWS_Pow20(zone.heightEdgeBlending);
                }
            }
        }
    }
    #endif


    float3 disp = 0;

    #if defined(KWS_USE_OCEAN_RENDERING)
    
        #if defined(KWS_USE_LOCAL_WATER_ZONES)
            disp = GetFftWavesDisplacement(worldPos, data.windAttenuation);
        #else
		    disp = GetFftWavesDisplacement(worldPos);
        #endif
    #endif
    
    data.oceanOffset = disp;

    
    
    #if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES)

        #ifdef KWS_HEIGHT_READBACK
            //Since the map is rendered for the last camera in the rendering queue,
            //we can't use the current camera to calculate the distance. Instead, we use the KWS_DynamicWavesMapPos 
            float distanceToCamera = length(KWS_DynamicWavesMapPos.xz - worldPos.xz);
        #else
            float distanceToCamera = GetWorldToCameraDistanceXZ(worldPos);
        #endif
        
        float3 dynamicWavesMapUV = GetDynamicWavesMapUV(worldPos, distanceToCamera);
        if (!IsOutsideUvBorders(dynamicWavesMapUV.xy))
        {
    	    float4 dynamicWaves = GetDynamicWavesMapBicubic(dynamicWavesMapUV);
            DynamicWavesAdditionalData additionalData = GetDynamicWavesAdditionalMap(dynamicWavesMapUV);
            waterLevel = dynamicWaves.z;
            terrainLevel = dynamicWaves.w;
            shorelineMask =  max(additionalData.shorelineMask, 1 - additionalData.zoneFade);

            data.isRequireDiscardTriangle = additionalData.zoneFade <= 0.001;
            if (KWS_RenderOcean == 0 && additionalData.zoneFade <= 0.001) disp.y -= 1000;

            data.flowDirection = dynamicWaves.xy;
            data.foamMask = additionalData.foamMask * additionalData.zoneFade;
            //data.orthoDepth = max(data.orthoDepth, zoneDepthMask > 0.0 ? zoneDepthMask : -100000);

      
            //disp.y = GetDynamicWavesAttenuatedOceanHeight(terrainLevel, disp.y, shorelineMask, additionalData.zoneFade);
          //  disp.y = PreventOceanHeightTerrainIntersection(terrainLevel, disp.y);
            
            disp *= shorelineMask;
            
            //disp.xz += clamp(data.flowDirection, -1, 1) * max(0.5, waterLevel) * KWS_DYNAMIC_WAVES_MAX_XZ_OFFSET * lerp(7, 2, 1-KWS_Pow2(1-shorelineMask)) * 0.5;
            //disp.xz += clamp(data.flowDirection, -1, 1) * lerp(1, 3, saturate(waterLevel * waterLevel)) * KWS_DYNAMIC_WAVES_MAX_XZ_OFFSET;
            //disp.y = lerp(disp.y, max(disp.y, terrainLevel + 0.5), shorelineMask);
            
            #if defined(KWS_USE_LOCAL_WATER_ZONES)
                float dynWavesOffset = waterLevel + terrainLevel;
                dynWavesOffset = lerp(dynWavesOffset, 0, offsetBlending);
                disp.y += dynWavesOffset;
               
            #else
                disp.y += waterLevel + terrainLevel;
                disp.y = max(disp.y, terrainLevel);
            #endif
  

        }

    #endif



    #if defined(KWS_USE_ZONE_INSTANCE)
    
        float4 dynamicWaves = GetDynamicWavesZone(uv);
        float zoneFade = GetDynamicWavesBorderFading(uv);
        disp = lerp(disp, float3(0, 0, 0), zoneFade);

        
        float2 time = frac(KWS_Time * KWS_SimulationTimeScale * 0.001) * 1000;
        float noiseScale = 0.25;
        float2 noise = float2(SimpleNoise1(worldPos.xz * noiseScale + time * 1.5), SimpleNoise1(worldPos.xz * noiseScale - (time * 2 + 40))) ;

        float waveDepth = length(dynamicWaves.xy) * dynamicWaves.z;
        disp.xz += noise * saturate(0.03 + waveDepth * 0.25) * 1.5;

        disp.y += lerp(0, dynamicWaves.z + dynamicWaves.w, KWS_BakedZoneInPreviewMode);

    #if defined(KWS_USE_LOCAL_WATER_ZONES)
        maxHeightOffset = min(maxHeightOffset, dynamicWaves.z + dynamicWaves.w);
    #endif
  
    #endif


    #if defined(KWS_USE_LOCAL_WATER_ZONES)
        disp.y = lerp(disp.y, disp.y + maxHeightOffset, offsetBlending);

    #endif


    data.offset += disp;

    data.offset.y += KWS_OceanLevelHeightOffset;

    return data;
}


v2fDepth vertDepth(waterInput v)
{
    v2fDepth o = (v2fDepth)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    KWS_INITIALIZE_DEFAULT_MATRIXES;

    #if defined(KWS_USE_WATER_INSTANCING)
		UpdateInstanceData(v.instanceID, v.uv, v.vertex, KWS_MATRIX_M, KWS_MATRIX_I_M);
    #endif

    o.worldPos.xyz = LocalToWorldPos(v.vertex.xyz, KWS_MATRIX_M);

    WaterOffsetData offsetData = ComputeWaterOffset(o.worldPos.xyz, v.uv);
    float3 offset = WorldToLocalPosWithoutTranslation(offsetData.offset, KWS_MATRIX_I_M);

    // if (offsetData.isRequireDiscardTriangle)
    // {
    // 	o.pos.w = NAN_VALUE;
    // 	return o;
    // }
    //
    v.vertex.xyz += offset;

    o.surfaceMask = v.surfaceMask * offsetData.surfaceMask;
    o.windAttenuation = offsetData.windAttenuation;

    o.worldPosRefracted.xyz = LocalToWorldPos(v.vertex.xyz, KWS_MATRIX_M);
    o.localHeightAndTensionMask.x = offset.y;
    o.localHeightAndTensionMask.y = v.vertex.y - offset.y;

    o.pos = ObjectToClipPos(v.vertex, KWS_MATRIX_M, KWS_MATRIX_I_M);
    o.screenPos = ComputeScreenPos(o.pos);
    o.worldNormal = GetWorldSpaceNormal(v.normal, KWS_MATRIX_M);
    o.uv = v.uv;

    return o;
}

v2fWater vertWater(waterInput v)
{
    v2fWater o = (v2fWater)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    KWS_INITIALIZE_DEFAULT_MATRIXES;

    #if defined(KWS_USE_WATER_INSTANCING)
		UpdateInstanceData(v.instanceID, v.uv, v.vertex, KWS_MATRIX_M, KWS_MATRIX_I_M);
    #endif
    o.worldPos = LocalToWorldPos(v.vertex.xyz, KWS_MATRIX_M);

    WaterOffsetData offsetData = ComputeWaterOffset(o.worldPos.xyz, v.uv);
    float3 offset = WorldToLocalPosWithoutTranslation(offsetData.offset, KWS_MATRIX_I_M);

    // if (offsetData.isRequireDiscardTriangle)
    // {
    // 	o.pos.w = NAN_VALUE;
    // 	return o;
    // }
    v.vertex.xyz += offset;

    o.surfaceMask = v.surfaceMask * offsetData.surfaceMask;
    o.windAttenuation = offsetData.windAttenuation;

    o.worldPosRefracted = LocalToWorldPos(v.vertex.xyz, KWS_MATRIX_M);
    o.pos = ObjectToClipPos(v.vertex, KWS_MATRIX_M, KWS_MATRIX_I_M);
    o.screenPos = ComputeScreenPos(o.pos);
    o.worldNormal = GetWorldSpaceNormal(v.normal, KWS_MATRIX_M);
    o.uv = v.uv;

    return o;
}
#endif
