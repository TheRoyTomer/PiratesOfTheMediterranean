#ifndef KWS_DYNAMIC_WAVES_HELPERS
#define KWS_DYNAMIC_WAVES_HELPERS


Texture2D _SourceRT;
float4 _SourceRT_TexelSize;
float4 _SourceRTHandleScale;

Texture2D KWS_PreviousTarget;
Texture2D KWS_CurrentTarget;
Texture2D KWS_CurrentAdditionalTarget;
Texture2D KWS_PreviousAdditionalTarget;
Texture2D KWS_CurrentNormalTarget;
Texture2D KWS_CurrentColorTarget;

Texture2D KWS_CurrentAdvectedUVTarget;
Texture2D KWS_DynamicWavesIntersection;

float4 KWS_DynamicWavesIntersection_TexelSize;
float4 KWS_CurrentTarget_TexelSize;

float3 KW_AreaOffset;
float3 KW_LastAreaOffset;
float KW_InteractiveWavesPixelSpeed;

float KWS_DynamicWavesRainStrength;
float KWS_DynamicWavesWaterSurfaceHeight;
float KWS_DynamicWavesForce;
float3 KWS_DynamicWavesForceDirection;
float KWS_ObstacleIntersectionAlphaFade;

float KWS_MeshIntersectionThreshold;
float KWS_DynamicWavesGlobalForceScale;
uint KWS_DynamicWavesUseWaterIntersection;
uint KWS_DynamicWavesZoneInteractionType;

uint KWS_CurrentFrame;
uint KWS_DynamicWavesLodIndex;
float3 KWS_DynamicWavesCurrentLodPos;

float KWS_SimulationWaveSpeedMultiplier;
float KWS_TurbulenceFoamStrength;
float KWS_WaveCrestFoamStrength;
float KWS_ShorelineFoamStrength;
float KWS_FoamDisappearSpeed;
float KWS_EvaporationRate;
float KWS_RainDropsIntensity;
float KWS_OceanWavesInfluenceStrength;
float KWS_ZoneShorelineDistance;

float2 GetCellVelocity(float4 center, float4 left, float4 down)
{
	return float2((left.x + center.x) * 0.5, (down.y + center.y) * 0.5);
}

inline void ApplyVorticityConfinement(inout float4 center, float4 left, float4 right, float4 down, float4 top, float localDt, float strength)
{
    // 2D curl: dv/dx - du/dz
    float omega = ((right.y - left.y) - (top.x - down.x)) * 0.5 / GRID_CELL_SIZE;

    // Cheap gradient approximation of vorticity magnitude.
    float omegaX = abs(right.y - center.y) - abs(center.y - left.y);
    float omegaY = abs(top.x - center.x) - abs(center.x - down.x);

    float2 gradOmega = float2(omegaX, omegaY);
    float2 n = SafeNormalize2(gradOmega);

    // Perpendicular force toward vortex center.
    float2 force = float2(n.y, -n.x) * omega;

    float vortexMask = smoothstep(0.02, 0.25, abs(omega));
    float speedMask = smoothstep(0.15, 2.0, length(center));

    center.xy += force * strength * vortexMask * speedMask * localDt;
}

void ApplyVelocityDamping(inout float4 center, float localDt)
{
    float speed = length(center.xy);
    float H = max(center.z, MIN_FRICTION_DEPTH);

    float manningDrag = GRAVITY * MANNING_N * MANNING_N * speed / pow(H, 4.0 / 3.0);
    float totalDrag = manningDrag + LINEAR_DRAG;

    center.xy *= rcp(1.0 + totalDrag * localDt);
}

float DecayFoamFastThenSlow(float foam, float fade, float slowLevel, float fastMul, float slowMul)
{
    foam = saturate(foam);

    if (foam > slowLevel)
    {
        foam = max(slowLevel, foam - fade * fastMul);
    }
    else
    {
        foam = max(0.0, foam - fade * slowMul);
    }

    return foam;
}


static const float2 QuadIndex[6] =
{
    float2(-0.5, -0.5),
    float2(-0.5, 0.5),
    float2(0.5, 0.5),
    float2(0.5, 0.5),
    float2(0.5, -0.5),
    float2(-0.5, -0.5)
};

float sphIntersect(float3 ro, float3 rd, float4 sph)
{
    float3 oc = ro - sph.xyz;
    float b = dot(oc, rd);
    float c = dot(oc, oc) - sph.w * sph.w;
    float h = b * b - c;
    if (h < 0.0) return -1.0;
    h = sqrt(h);
    return -b - h;
}

float FadeWithSoftEdges(float dist, float start, float end, float edgeFraction)
{
    float length = end - start;
    float edge = length * edgeFraction;

    float fadeIn = saturate((dist - start) / edge);
    float fadeOut = saturate((end - dist) / edge);

    return min(fadeIn, fadeOut);
}

float RainNoise(float2 p, float threshold)
{
    p = p * 9757.0 + frac(KWS_Time * 0.01) * 100.0;
    float3 p3 = frac(float3(p.xyx) * float3(.1031, .1030, .0973));
    p3 += dot(p3, p3.yxz + 33.33);
    float3 noise = frac((p3.xxy + p3.yzz) * p3.zyx);
    return (noise.x * noise.y * noise.z) > threshold;
}


float ComputeLocalDt(float2 vel, float h)
{
	return TIME_STEP * KWS_SimulationWaveSpeedMultiplier;
	const float H_EPS = 0.01;
	const float CFL = 0.45;
	const float MAX_CFL_DEPTH = 4.0; 

	float hCfl = min(max(h, H_EPS), MAX_CFL_DEPTH);
	float c = sqrt(GRAVITY * hCfl);
	float waveSpeed = length(vel) + c;

	return min(TIME_STEP * KWS_SimulationWaveSpeedMultiplier, CFL * GRID_CELL_SIZE / max(waveSpeed, 1e-4));
}



void ClampVelocity(inout float2 velocity, float localDT)
{
    float velocityLength = length(velocity);

    if (velocityLength > 0.0)
    {
        float alpha = 0.5;
        float maxVelocity = GRID_CELL_SIZE / localDT * alpha;

        velocity /= velocityLength;
        velocityLength = min(velocityLength, maxVelocity);
        velocity *= velocityLength;
    }
}

float3 ComputeNormal(float4 left, float4 right, float4 top, float4 down)
{
    float dHeightX = ((right.z + right.w) - (left.z + left.w)) / (2.0 * KWS_CurrentTarget_TexelSize.x);
    float dHeightZ = ((top.z + top.w) - (down.z + down.w)) / (2.0 * KWS_CurrentTarget_TexelSize.y);
    float3 normal = normalize(float3(dHeightX, max(KWS_DynamicWavesZoneSize.x, KWS_DynamicWavesZoneSize.z), dHeightZ));

    return float3(-normal.x, normal.y, -normal.z);
}

float4 ComputeAdvectedUV(float2 currentUV, float localDt, float4 center)
{
    float4 uvAdvected = 0;
    if (KWS_CurrentFrame < 3)
    {
        uvAdvected = float4(currentUV, currentUV);
    }
    else
    {
        float dt = -ADVECT_SPEED_RIVER * localDt / GRID_CELL_SIZE;
        float2 advectVelocity = clamp(center.xy, -1, 1);

        float t = (KWS_Time * KWS_SimulationTimeScale) % KWS_ADVECTED_UV_REPEAT_TIME;
        float4 advectedUV = KWS_CurrentAdvectedUVTarget.SampleLevel(sampler_linear_repeat, currentUV +
                                                                    KWS_DynamicWavesZoneFlowSpeedMultiplier * 2.0 * advectVelocity * dt * KWS_CurrentTarget_TexelSize.xy, 0);

        if (t < 0.05) advectedUV.xy = currentUV;
        uvAdvected.xy = advectedUV.xy;

        if (abs(t - KWS_ADVECTED_UV_REPEAT_TIME * 0.5) < 0.05) advectedUV.zw = currentUV;
        uvAdvected.zw = advectedUV.zw;
    }

    return uvAdvected;
}

void UpdateDrySurfaceArtefacts(inout float4 center, float4 left, float4 right, float4 top, float4 down)
{
    if (center.w > 0 && center.z < MINIMUM_WATER_HEIGHT)
    {
        center.w = 0;
        center.z *= 0.75;
        if (right.z > MINIMUM_WATER_HEIGHT || top.z > MINIMUM_WATER_HEIGHT
            || left.z > MINIMUM_WATER_HEIGHT || down.z > MINIMUM_WATER_HEIGHT)
            center.w = min(top.w, min(down.w, min(left.w, right.w)));
    }
}

void UpdateBorders(inout float4 center, float4 right, float4 top, float currentHeight, float rightHeight, float topHeight)
{
    if (((center.z <= MINIMUM_WATER_HEIGHT * GRID_CELL_SIZE) && (center.w > rightHeight)) ||
        ((right.z <= MINIMUM_WATER_HEIGHT * GRID_CELL_SIZE) && (right.w > currentHeight)))
    {
        center.x *= 0.0;
    }

    if (((center.z <= MINIMUM_WATER_HEIGHT * GRID_CELL_SIZE) && (center.w > topHeight)) ||
        ((top.z <= MINIMUM_WATER_HEIGHT * GRID_CELL_SIZE) && (top.w > currentHeight)))
    {
        center.y *= 0.0;
    }
}

float4 UpdateColor(float2 currentUV, float4 center, float localDt, float shorelineMaskFade, float borderFade)
{
    float4 dynamicWaveMaskColor = GetDynamicWavesMaskColor(currentUV);
			
    float4 colorData = KWS_CurrentColorTarget.SampleLevel(sampler_linear_clamp, currentUV, 0);
    float4 colorDataAdvected = KWS_CurrentColorTarget.SampleLevel(sampler_linear_clamp, currentUV - (center.xy * localDt * 1.5) * KWS_CurrentTarget_TexelSize.xy, 0);
    float4 colorDataAdvected2 = KWS_CurrentColorTarget.SampleLevel(sampler_linear_clamp, currentUV + (center.xy * localDt * 0.5) * KWS_CurrentTarget_TexelSize.xy, 0);
     colorDataAdvected = max(colorDataAdvected, colorDataAdvected2);

    float shorelineColorFading = (shorelineMaskFade > 0.01) ?      0.95 : 1;
    colorData.a *= shorelineColorFading;
			
    float4 finalColor = 0;
    finalColor.rgb = max(max(colorData.rgb, colorDataAdvected.rgb * (1 + 1.0 / 128.0)), dynamicWaveMaskColor.rgb);
    finalColor.a = max(max(colorData.a, colorDataAdvected.a), dynamicWaveMaskColor.a);
			
    return finalColor * saturate(borderFade * 10);
}

void UpdateDynamicObstacle(float dynamicWaveMaskDepth, inout float4 center, inout float4 dynamicWaveMask, float rawOrthoDepth, inout float foamMask, inout float wetMap)
{
    if (dynamicWaveMaskDepth > -99999)
    {
        float currentWorldHeight = center.z + center.w + KWS_WaterLevel;
        if (currentWorldHeight - 0.01 < dynamicWaveMaskDepth && center.z > MINIMUM_WATER_HEIGHT) center.z -= MINIMUM_WATER_HEIGHT;
        if (dynamicWaveMask.x > 0.001 && dynamicWaveMaskDepth - lerp(0.25, 4, saturate(dynamicWaveMask.x * 0.5)) > rawOrthoDepth)
        {
            float reversedDynMask = 1-dynamicWaveMask.x * 0.25;
            center.xyz *= reversedDynMask;
	
            foamMask *= reversedDynMask;
            wetMap *= reversedDynMask;
            dynamicWaveMask.x *= reversedDynMask;
        }
        dynamicWaveMask.x = 0.0;
    }
}

void OceanRelax(inout float4 center, float shorelineMask, float localDt)
{
    float oceanRelax = OCEAN_RELAX_RATE * shorelineMask;
    center.z *= rcp(1.0 + oceanRelax * localDt);

}

void AddRain(float2 shiftedUV, float riverMask, inout float4 center, inout float4 additionalData)
{
	float rainThreshold = lerp(0.995, 0.65, KWS_RainDropsIntensity);
	float rain = RainNoise(shiftedUV, rainThreshold);
	additionalData.x += rain * 0.1;
	center.z += rain * lerp(0.001, 0.01, riverMask);
	
}

void AddFFTWaves(inout float4 center, float3 worldPos, float shorelineMask, float2 shoreDir)
{
    float3 fftWaveDisplacement = GetFftWavesDisplacementDynamicWaves(worldPos);
    float a = radians(KWS_WindRotation);
    float2 waveDir  =  normalize(float2(cos(a), sin(a)) + 1e-5);
		  
    float windIncoming = 1;
		
    #if !defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
    windIncoming = saturate(dot(waveDir, shoreDir));
    windIncoming = lerp(windIncoming, 1, KWS_WindTurbulence);
    #endif
		
    float crestWave = clamp(fftWaveDisplacement.y * 0.01 * KWS_OceanWavesInfluenceStrength, -1, 1);
		
    center.z += crestWave * shorelineMask;
    center.xy += shorelineMask * shoreDir * crestWave * lerp(2, 0.5, windIncoming);
		
    //if (sdfDepth > 1 && sdfDepth < 25) center.xy *= lerp(1, 0.95, (1-shorelineMask) * saturate(-backwardAttenuation));
}

void UpdateWetmap(inout float4 additionalData, inout float wetMap, float4 center)
{
    //wet mask
    float wetMaskFrameFading = (KWS_CurrentFrame % 15) == 0 ?          1.0 / 128.0 : 0;
    additionalData.x *= lerp(1, 0.992, additionalData.x > 0.9);
    additionalData.x -= wetMaskFrameFading;

    wetMap = saturate((saturate(center.z) - 0.01) * 10);
    wetMap *= lerp(0.0001, 1, saturate(center.z * 4));
    wetMap = saturate(additionalData.x + wetMap);
}

float GetFoamMask(float2 shiftedUV, float4 additionalData, float4 center, float4 left, float4 right, float4 top, float4 down, float localDt, float2 noise, 
	float sdfDepth, float2 shoreDir, float shorelineMaskFade, float borderFade, float riverMask, float orthoDepth)
{
    
		int foamFadeFrames = max(1, (int)lerp(10.0, 1.0, KWS_FoamDisappearSpeed));
		float foamDecayBase = (KWS_CurrentFrame % foamFadeFrames) == 0 ? 1.0 / 128.0 : 0.0;
		float2 vC = GetCellVelocity(center, left, down);
	
		// Advect previous foam
		float foamAdvectionScale = lerp(1.0, 0.15, smoothstep(1.0, 10.0, sdfDepth));
		float2 foamAdvectionVel =  vC * 1.5 + noise * 1.5;
		float2 foamAdvectionOffset = 1 * foamAdvectionVel * localDt * KWS_CurrentTarget_TexelSize.xy * 2;

		float advectedFoam = KWS_CurrentAdditionalTarget.SampleLevel(sampler_linear_clamp, shiftedUV - foamAdvectionOffset, 0).z;
		float oldFoam = lerp(additionalData.z, advectedFoam, 0.95);


		// Common data
		float hC = center.z;
		float hL = left.z;
		float hR = right.z;
		float hD = down.z;
		float hT = top.z;


		float2 vL = left.xy;
		float2 vR = right.xy;
		float2 vD = down.xy;
		float2 vT = top.xy;

		float h = hC;
		float speed = length(vC);


		// Height shape
		float avgH = (hL + hR + hD + hT) * 0.25;
		float curvature = hC * 4.0 - (hL + hR + hD + hT);

		float2 gradH = float2(hR - hL, hT - hD) * 0.5;
		float slope = length(gradH);


		// Velocity derivatives
		float du_dx = center.x - left.x;
		float dv_dy = center.y - down.y;
		float du_dy = (top.x - down.x) * 0.5;
		float dv_dx = (right.y - left.y) * 0.5;

		float div = du_dx + dv_dy;
		float curl = dv_dx - du_dy;

		float shearX = du_dx - dv_dy;
		float shearY = du_dy + dv_dx;
		float shear = length(float2(shearX, shearY));

		float2 flowDir = normalize(vC + 1e-5);


		// Flow gates
		float froude = speed / max(sqrt(GRAVITY * max(h, 0.02)), 0.001);
		float fastFlow = smoothstep(0.15, 0.65, froude);
	
		float incomingToShore = orthoDepth > 1;
	#if !defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
		incomingToShore = KWS_Pow2(saturate(dot(flowDir, shoreDir)));
	#endif
	
		//float openWaterMask = smoothstep(35.0, 50.0, sdfDepth);
		//float crestDirectionGate = lerp(incomingToShore, 1.0, openWaterMask);
	
		// Crest foam
		float crestPeak = smoothstep(0.006, 0.04, hC - avgH) * smoothstep(0.008, 0.08, curvature);
		float compression = smoothstep(0.04, 0.35, -div);
		float breakingFront = smoothstep(0.015, 0.08, slope) * compression;

		float crestFoam = max(crestPeak, breakingFront * 0.5) * fastFlow * incomingToShore;


		// Turbulence foam
		float turbulenceRaw = abs(curl) + shear * 0.5;
		float turbulence = smoothstep(0.08, 0.55, turbulenceRaw);
		turbulence *= lerp(1.0, 0.35, compression);

		float turbulenceFoam = turbulence * saturate(speed * 0.2);


		// Shoreline foam
		float shoreRange = lerp(2.0, 15.0, saturate(KWS_ShorelineFoamStrength));
		float shoreBand = 1.0 - smoothstep(0.1, shoreRange, sdfDepth);

		float borderShoreMask = saturate(min(1.0 - shorelineMaskFade, borderFade));
		float shallowFoam = 1.0 - smoothstep(0.1, 1.0, saturate(h * 0.2));

		float shorelineFoam = shoreBand * borderShoreMask * shallowFoam * fastFlow * (1.0 - riverMask);


		// Compression / obstacle foam
		float2 bottomGrad = float2(right.w - left.w, top.w - down.w) * 0.5;
		float bottomSlope = length(bottomGrad);

		float2 obstacleDir = normalize(bottomGrad + 1e-5);
		float incomingToObstacle = dot(flowDir, obstacleDir);

		float incomingGate = smoothstep(-0.15, 0.55, incomingToObstacle);
		float slopeGate = smoothstep(0.005, 0.08, bottomSlope);
		float obstacleGate = lerp(0.25, 1.0, slopeGate * incomingGate);

		float depthGate = smoothstep(0.05, 0.45, h);
		float compressionFoam = compression * fastFlow * depthGate * obstacleGate;


		// Final composition
		float stableFoam = crestFoam * KWS_WaveCrestFoamStrength + shorelineFoam * KWS_ShorelineFoamStrength * 0.1;
		float impactFoam = turbulenceFoam * KWS_TurbulenceFoamStrength * 0.05 + compressionFoam * KWS_TurbulenceFoamStrength * 0.1;

		float newFoamValue = saturate(stableFoam + impactFoam);
		newFoamValue *= saturate(h * 4);
		//float foamDecay = lerp(foamDecayBase, 0.015, saturate(oldFoam * oldFoam));
		float foamMask = saturate(oldFoam - foamDecayBase + newFoamValue);

	
		return foamMask;
}

void Advection(inout float4 center, float orthoDepth, float2 uv, float localDt, inout float2 noise)
{
	float zFade = (orthoDepth - 0.5 > 0) ?  saturate(center.z * 20) : 1;
		
	float dt = -1.25 * localDt / GRID_CELL_SIZE;
	float2 time = frac(KWS_Time * KWS_SimulationTimeScale * 0.001) * 1000;
	noise = float2(SimpleNoise1(uv * 100 + center.xy * 0.1 + time * 2.5), SimpleNoise1(uv * 100 + center.xy * 0.1 - (time * 2.5 + 40)));
	noise = lerp(noise, noise * length(center.xy), 0.65) * 1.5 * zFade;
	float2 advectionOffset = ((center.xy * 1.5 + noise *  1.5) * dt) * KWS_CurrentTarget_TexelSize.xy * zFade;
	float2 advectedFlow = UnpackSimulation(KWS_CurrentTarget.SampleLevel(sampler_linear_clamp, uv + advectionOffset, 0)).xy;
	center.xy = lerp(center.xy, advectedFlow, zFade);
}

#endif
