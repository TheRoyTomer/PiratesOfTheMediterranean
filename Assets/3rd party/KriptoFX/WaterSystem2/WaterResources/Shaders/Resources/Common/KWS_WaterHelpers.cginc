#ifndef KWS_WATER_PASS_HELPERS
#define KWS_WATER_PASS_HELPERS


#include "KWS_WaterDefines.cginc"
#if !defined(KWS_BUILTIN) && !defined(KWS_URP) && !defined(KWS_HDRP)
	#define KWS_URP
#endif

#include "KWS_WaterVariables.cginc"
#include "../Common/KWS_CommonHelpers.cginc"


#if defined(KWS_BUILTIN)
	#include "../PlatformSpecific/KWS_PlatformSpecificHelpers_Builtin.cginc"
#elif defined(KWS_HDRP)
	#include "../PlatformSpecific/KWS_PlatformSpecificHelpers_HDRP.cginc"
#else // URP by default
	#include "../PlatformSpecific/KWS_PlatformSpecificHelpers_URP.cginc"
#endif


float CalcMipLevel(float2 uv)
{
	float2 dx = ddx(uv);
	float2 dy = ddy(uv);
	float delta = max(dot(dx, dx), dot(dy, dy));
	return max(0.0, 0.5 * log2(delta));
}


//////////////////////////////////////////////    FFT_Waves_Pass    //////////////////////////////////////////////
#define MAX_FFT_WAVES_MAX_CASCADES 4
#define BAKED_FFT_DEFAULT_TIME_SCALE 1.5


float4 KWS_WavesDomainSizes;
float4 KWS_WavesDomainSizesInv;
float4 KWS_WavesDomainScaledSizes;
float4 KWS_WavesDomainScaledSizesInv;

float4 KWS_WavesDomainVisibleArea;
float4 KWS_WavesDomainVisibleAreaInv;
float4 KWS_WavesDomainHeightScales;
float4 KWS_WavesDomainHeightScalesInv;

float KWS_WindSpeed;
float KWS_WindRotation;
float KWS_WindTurbulence;

float KWS_WavesAreaScale;
float KWS_WavesCascades;

Texture2DArray KWS_FftWavesDisplace;
Texture2DArray KWS_FftWavesDisplacePrev;
Texture2DArray KWS_FftWavesNormal;
SamplerState sampler_KWS_FftWavesNormal;
float4 KWS_FftWavesDisplace_TexelSize;
float4 KWS_FftWavesNormal_TexelSize;



float3 GetFftWavesDisplacementSlice(float3 worldPos, uint slice)
{
	worldPos += KWS_WaterWorldPosOffset;
	return KWS_FftWavesDisplace.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv[slice], slice), 0).xyz * KWS_WavesAreaScale;
}

float3 GetFftWavesDisplacementSliceBicubic(float3 worldPos, uint slice)
{
	worldPos += KWS_WaterWorldPosOffset;
	return Texture2DArraySampleLevelBicubic(KWS_FftWavesDisplace, sampler_linear_repeat, worldPos.xz * KWS_WavesDomainScaledSizesInv[slice], KWS_FftWavesDisplace_TexelSize, slice, 0).xyz;
}

inline float GetFftFade(float distanceToCamera, int lodIdx, float farDistanceMinFade = 0.0)
{
	if (lodIdx == KWS_WavesCascades - 1)
	{
		float farDist = max(500, KW_WaterFarDistance * 0.5);
		return saturate(1.0 + farDistanceMinFade - saturate(distanceToCamera / farDist));
	}
	else
	{
		float fadeLod = saturate(distanceToCamera * KWS_WavesDomainVisibleAreaInv[lodIdx]);
		fadeLod = 1 - fadeLod * fadeLod * fadeLod;
		return fadeLod;
	}
}

inline float4 KWS_GetFftFade4(float distanceToCamera)
{
	float4 fade = saturate(distanceToCamera.xxxx * KWS_WavesDomainVisibleAreaInv);
	fade = 1 - KWS_Pow3(fade);
	return fade;
}

float3 GetFftWavesDisplacementLast(float3 worldPos)
{
	worldPos += KWS_WaterWorldPosOffset;
	int lastCascadeIdx = max(0, KWS_WavesCascades - 1);
	return KWS_FftWavesDisplace.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv[lastCascadeIdx], lastCascadeIdx), 0).xyz * KWS_WavesAreaScale;
}

float3 GetFftWavesDisplacementDetailsHQ(float3 worldPos)
{
	worldPos += KWS_WaterWorldPosOffset;
	float3 disp = Texture2DArraySampleLevelBicubic(KWS_FftWavesDisplace, sampler_linear_repeat, worldPos.xz * KWS_WavesDomainScaledSizesInv[0], KWS_FftWavesDisplace_TexelSize, 0, 0).xyz;
	if (KWS_WavesCascades > 1) disp += Texture2DArraySampleLevelBicubic(KWS_FftWavesDisplace, sampler_linear_repeat, worldPos.xz * KWS_WavesDomainScaledSizesInv[1], KWS_FftWavesDisplace_TexelSize, 1, 0).xyz;
	return disp * KWS_WavesAreaScale;
}

float3 GetFftWavesDisplacementDetails(float3 worldPos)
{
	worldPos += KWS_WaterWorldPosOffset;
	float3 disp = KWS_FftWavesDisplace.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv[0], 0), 0).xyz;
	if (KWS_WavesCascades > 1) disp += KWS_FftWavesDisplace.SampleLevel(sampler_linear_repeat, float3(worldPos.xz  * KWS_WavesDomainScaledSizesInv[1], 1), 0).xyz;
	return disp * KWS_WavesAreaScale;
}

float3 GetFftWavesDisplacementBuoyancy(float3 worldPos)
{
	worldPos += KWS_WaterWorldPosOffset;

	float3 finalData = 0;
	for (int idx = KWS_WavesCascades - 1; idx > 0; idx--)
	{
		finalData += KWS_FftWavesDisplace.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv[idx], idx), 0).xyz;
	}
	return finalData * KWS_WavesAreaScale;
}

float3 GetFftWavesDisplacementDynamicWaves(float3 worldPos)
{
	worldPos += KWS_WaterWorldPosOffset;
	int maxCascadeIdx = KWS_WavesCascades - 1;

	float3 finalData = 0;
	for (int idx = maxCascadeIdx; idx > 1; idx--)
	{
		finalData += KWS_FftWavesDisplace.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv[idx], idx), 0).xyz * saturate((idx + 0.25) / maxCascadeIdx);
	}
	return finalData * KWS_WavesAreaScale;
}

float3 GetFftWavesDisplacementDynamicWavesPrev(float3 worldPos)
{
	worldPos += KWS_WaterWorldPosOffset;
	int maxCascadeIdx = KWS_WavesCascades - 1;

	float3 finalData = 0;
	for (int idx = maxCascadeIdx; idx > 1; idx--)
	{
		finalData += KWS_FftWavesDisplacePrev.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv[idx], idx), 0).xyz * saturate((idx + 0.25) / maxCascadeIdx);
	}
	return finalData * KWS_WavesAreaScale;
}



Texture2DArray KWS_BakedFFT_Lod0;
Texture2DArray KWS_BakedFFT_Lod1;
Texture2DArray KWS_BakedFFT_Lod2;
Texture2DArray KWS_BakedFFT_Lod3;

Texture2DArray KWS_BakedNormalFFT_Lod0;
Texture2DArray KWS_BakedNormalFFT_Lod1;
Texture2DArray KWS_BakedNormalFFT_Lod2;
Texture2DArray KWS_BakedNormalFFT_Lod3;

float3 GetFftWavesDisplacement(float3 worldPos, float attenuation = 1)
{
	//#define KWS_USE_BAKED_OCEAN_WAVES
	
	worldPos += KWS_WaterWorldPosOffset;
	float distanceToCamera = GetWorldToCameraDistance(worldPos);
	float4 fade4 = KWS_GetFftFade4(distanceToCamera);
	float4 windAttenuation = lerp(attenuation * saturate(1.2 - float4(0.0, 0.2, 0.4, 0.6)), float4(1, 1, 1, 1), attenuation);
				
	float3 finalData = 0;

	float3 lod0 = 0;
	float3 lod1 = 0;
	float3 lod2 = 0;
	float3 lod3 = 0;
		
	#ifdef KWS_USE_BAKED_OCEAN_WAVES

		float loopTime = 5;
		float fps = 12;
		float frames = loopTime * fps;
	
		float oceanTime = KWS_OceanTimeScale * KWS_Time * BAKED_FFT_DEFAULT_TIME_SCALE;

		float norm = frac(oceanTime / loopTime);
		float fIndex = norm * frames;

		int prevFrameIndex = floor(fIndex);
		int nextFrameIndex = (prevFrameIndex + 1) % frames;

		float lerpVal = frac(fIndex);
	
		half3 prevLod0 = KWS_BakedFFT_Lod0.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.x, prevFrameIndex), 0);
		half3 prevLod1 = KWS_BakedFFT_Lod1.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.y, prevFrameIndex), 0);
		half3 prevLod2 = KWS_BakedFFT_Lod2.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.z, prevFrameIndex), 0);
		half3 prevLod3 = KWS_BakedFFT_Lod3.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.w, prevFrameIndex), 0);

		half3 currentLod0 = KWS_BakedFFT_Lod0.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.x, nextFrameIndex), 0);
		half3 currentLod1 = KWS_BakedFFT_Lod1.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.y, nextFrameIndex), 0);
		half3 currentLod2 = KWS_BakedFFT_Lod2.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.z, nextFrameIndex), 0);
		half3 currentLod3 = KWS_BakedFFT_Lod3.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.w, nextFrameIndex), 0);

		lod0 = lerp(prevLod0, currentLod0, lerpVal);
		lod1 = lerp(prevLod1, currentLod1, lerpVal);
		lod2 = lerp(prevLod2, currentLod2, lerpVal);
		lod3 = lerp(prevLod3, currentLod3, lerpVal);
			
	#else
	
		lod0 = KWS_FftWavesDisplace.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.x, 0), 0).xyz;
		lod1 = KWS_FftWavesDisplace.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.y, 1), 0).xyz;
		lod2 = KWS_FftWavesDisplace.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.z, 2), 0).xyz;
		lod3 = KWS_FftWavesDisplace.SampleLevel(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.w, 3), 0).xyz;
		

	#endif
	
	finalData += fade4.x * windAttenuation.x * lod0;
	finalData += fade4.y * windAttenuation.y * lod1;
	finalData += fade4.z * windAttenuation.z * lod2;
	finalData += fade4.w * windAttenuation.w * lod3;
	
	return finalData * KWS_WavesAreaScale;
}



float GetNoiseMask(float2 uv, float scale, float timeScale)
{
	uv *= scale;
	float currentTime = KWS_Time * timeScale;
	//float2 noise1 = float2(SimpleNoise1(uv * 0.005 + currentTime * 0.15), SimpleNoise1(uv * 0.005 - (currentTime * 0.15 + 40)));
	float2 noise2 = float2(SimpleNoise1(uv * 0.026 + currentTime * 0.06), SimpleNoise1(uv * 0.024 - (currentTime * 0.07 + 40)));
	//float animNoise1 = saturate(0.05 + KWS_Pow10(1 - saturate(max(noise1.x, noise1.y) * 0.2)));
	float animNoise2 = saturate(0.1 + KWS_Pow5(1 - saturate(max(noise2.x, noise2.y) * 0.75)));
	return animNoise2;
}

float3 GetFftWavesNormalFoam(float3 worldPos, float attenuation)
{
	
	//#define KWS_USE_BAKED_OCEAN_WAVES
	
	
	worldPos += KWS_WaterWorldPosOffset;
	float distanceToCamera = GetWorldToCameraDistance(worldPos);
	float4 fade4 = KWS_GetFftFade4(distanceToCamera);
	float4 windAttenuation = lerp(attenuation * saturate(1.2 - float4(0.0, 0.2, 0.4, 0.6)), float4(1, 1, 1, 1), attenuation);
	
	float3 lod0 = 0;
	float3 lod1 = 0;
	float3 lod2 = 0;
	float3 lod3 = 0;
		
	#ifdef KWS_USE_BAKED_OCEAN_WAVES

		float3 finalData = 0;

		float loopTime = 5;
		float fps = 12;
		float frames = loopTime * fps;
	
		float oceanTime = KWS_OceanTimeScale * KWS_Time * BAKED_FFT_DEFAULT_TIME_SCALE;

		float norm = frac(oceanTime / loopTime);
		float fIndex = norm * frames;

		int prevFrameIndex = floor(fIndex);
		int nextFrameIndex = (prevFrameIndex + 1) % frames;

		float lerpVal = frac(fIndex);
	
		half3 prevLod0 = KWS_BakedNormalFFT_Lod0.Sample(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.x, prevFrameIndex));
		half3 prevLod1 = KWS_BakedNormalFFT_Lod1.Sample(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.y, prevFrameIndex));
		half3 prevLod2 = KWS_BakedNormalFFT_Lod2.Sample(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.z, prevFrameIndex));
		half3 prevLod3 = KWS_BakedNormalFFT_Lod3.Sample(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.w, prevFrameIndex));

		half3 currentLod0 = KWS_BakedNormalFFT_Lod0.Sample(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.x, nextFrameIndex));
		half3 currentLod1 = KWS_BakedNormalFFT_Lod1.Sample(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.y, nextFrameIndex));
		half3 currentLod2 = KWS_BakedNormalFFT_Lod2.Sample(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.z, nextFrameIndex));
		half3 currentLod3 = KWS_BakedNormalFFT_Lod3.Sample(sampler_linear_repeat, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.w, nextFrameIndex));

	
		lod0 = lerp(prevLod0, currentLod0, lerpVal);
		lod1 = lerp(prevLod1, currentLod1, lerpVal);
		lod2 = lerp(prevLod2, currentLod2, lerpVal);
		lod3 = lerp(prevLod3, currentLod3, lerpVal);
				
	
	#else

		lod0 = Texture2DArraySampleBicubic(KWS_FftWavesNormal, sampler_KWS_FftWavesNormal, worldPos.xz * KWS_WavesDomainScaledSizesInv.x, KWS_FftWavesNormal_TexelSize, 0).xyz;
		lod1 = KWS_FftWavesNormal.Sample(sampler_KWS_FftWavesNormal, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.y, 1)).xyz;
		lod2 = KWS_FftWavesNormal.Sample(sampler_KWS_FftWavesNormal, float3(worldPos.xz * KWS_WavesDomainScaledSizesInv.z, 2)).xyz;
		lod3 = Texture2DArraySampleBicubic(KWS_FftWavesNormal, sampler_KWS_FftWavesNormal, worldPos.xz * KWS_WavesDomainScaledSizesInv.w, KWS_FftWavesNormal_TexelSize, 3).xyz;
	
	#endif
	
	
	float2 normal0 = saturate(fade4.x + 0.15) * windAttenuation.x * (lod0.xz * 2 - 1);
	float2 normal1 = saturate(fade4.y + 0.2) * windAttenuation.y * (lod1.xz * 2 - 1);
	float2 normal2 = saturate(fade4.z + 0.25) * windAttenuation.z * (lod2.xz * 2 - 1);
	float2 normal3 = saturate(fade4.w + 0.25) * windAttenuation.w * (lod3.xz * 2 - 1);
		
	float foam = saturate(lod0.y + lod1.y + lod2.y + lod3.y);
		
	float2 finalNormal = KWS_BlendNormalsXZ(normal0, normal1, normal2, normal3);
			
	return float3(finalNormal.x, foam, finalNormal.y);
}


float GetFftWavesHeight(float3 worldPos, uint iterations)
{
	float3 invertedDisplacedPosition = worldPos;
	for (uint i = 0; i < iterations; i++)
	{
		float3 displacement = GetFftWavesDisplacementBuoyancy(invertedDisplacedPosition);
		float3 error = (invertedDisplacedPosition + displacement) - worldPos;
		invertedDisplacedPosition -= error;
	}

	float3 disp = GetFftWavesDisplacement(invertedDisplacedPosition);
	return disp.y;
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////




//////////////////////////////////////////////    PrePass (Mask/Normal/SSS/VolumeMask/Depth)   //////////////////////////////////////////////


//#define WATER_MASK_PASS_UNDERWATER_THRESHOLD 0.35
#define WATER_VOLUME_PRE_PASS_MAX_VALUE 1000000
#define WATER_LINE_HEIGHT_ENCODE_VALUE 10000

DECLARE_TEXTURE(KWS_WaterPrePassRT0);
DECLARE_TEXTURE(KWS_WaterPrePassRT1);
DECLARE_TEXTURE(KWS_WaterDepthRT);
DECLARE_TEXTURE(KWS_WaterIntersectionHalfLineTensionMaskRT);
float4 KWS_WaterPrePassRT0_TexelSize;
float4 KWS_WaterPrePass_RTHandleScale;

DECLARE_TEXTURE(KWS_WaterBackfacePrePassRT0);
DECLARE_TEXTURE(KWS_WaterBackfacePrePassRT1);
DECLARE_TEXTURE(KWS_WaterBackfaceDepthRT);

float4 KWS_WaterBackfacePrePassRT0_TexelSize;
float4 KWS_WaterBackfacePrePass_RTHandleScale;

DECLARE_TEXTURE(KWS_WaterClipMaskDepthFrontRT);
DECLARE_TEXTURE(KWS_WaterClipMaskDepthBackRT);
DECLARE_TEXTURE(KWS_WaterClipMaskRT);
float4 KWS_WaterClipMaskDepthFrontRT_TexelSize;
float4 KWS_WaterClipMask_RTHandleScale;

inline float2 GetWaterPrePassUV(float2 uv)
{
	uv = GetRTHandleUV(uv, KWS_WaterPrePassRT0_TexelSize.xy, 1.0, KWS_WaterPrePass_RTHandleScale.xy);
	return uv;
}

inline float2 GetWaterBackfacePrePassUV(float2 uv)
{
	uv = GetRTHandleUV(uv, KWS_WaterBackfacePrePassRT0_TexelSize.xy, 1.0, KWS_WaterBackfacePrePass_RTHandleScale.xy);
	return uv;
}

inline float2 GetWaterMaskThicknessUV(float2 uv)
{
	uv = GetRTHandleUV(uv, KWS_WaterClipMaskDepthFrontRT_TexelSize.xy, 1.0, KWS_WaterClipMask_RTHandleScale.xy);
	return uv;
}


inline float GetWaterSSS(float2 uv)
{
	return SAMPLE_TEXTURE_LOD(KWS_WaterPrePassRT0, sampler_linear_clamp, GetWaterPrePassUV(uv), 0).z;
}

inline float3 GetWaterNormals(float2 uv)
{
	float2 rawNormal = SAMPLE_TEXTURE_LOD(KWS_WaterPrePassRT1, sampler_linear_clamp, GetWaterPrePassUV(uv), 0).xy;
	#ifdef KWS_USE_AQUARIUM_RENDERING
		float2 rawNormalBackface = SAMPLE_TEXTURE_LOD(KWS_WaterBackfacePrePassRT1, sampler_point_clamp, GetWaterBackfacePrePassUV(uv), 0).xy;
		rawNormal += rawNormalBackface;
	#endif
	
	return float3(rawNormal.x, 1, rawNormal.y);
}


inline float GetWaterAquariumBackfaceMask(float2 uv)
{
	return SAMPLE_TEXTURE_LOD(KWS_WaterBackfacePrePassRT0, sampler_point_clamp, GetWaterBackfacePrePassUV(uv), 0).y;
}


//inside = 1, surface outside = 0.5, box fringe = 0.1
inline float GetWaterMaskFast(float2 uv, float2 offset = float2(0, 0))
{
	return SAMPLE_TEXTURE_LOD(KWS_WaterPrePassRT0, sampler_linear_clamp, GetWaterPrePassUV(uv), 0).y;
}


//inside = 1, surface outside = 0.5, box fringe = 0.1
inline float GetWaterMask(float2 uv, float2 offset = float2(0, 0))
{
	
		#ifdef KWS_SHARED_API_INCLUDED
		return GetWaterMaskFast(uv);
		#else
			
		float4 mask = SAMPLE_TEXTURE_GATHER_GREEN(KWS_WaterPrePassRT0, sampler_linear_clamp, GetWaterPrePassUV(uv) + KWS_WaterPrePassRT0_TexelSize.xy * offset);
		return max(mask.x, max(mask.y, max(mask.z, mask.w)));
		#endif

		//float mask = SAMPLE_TEXTURE_LOD(KWS_WaterPrePassRT0, sampler_point_clamp, GetWaterPrePassUV(uv), 0).y;
			
		//float center = SAMPLE_TEXTURE_LOD(KW_WaterMaskScatterNormals, sampler_point_clamp, GetWaterPrePassUV(uv), 0).x;
		//float up = SAMPLE_TEXTURE_LOD_OFFSET(KW_WaterMaskScatterNormals, sampler_point_clamp, GetWaterPrePassUV(uv), 0, int2(0, 1)).x;
		//float down = SAMPLE_TEXTURE_LOD_OFFSET(KW_WaterMaskScatterNormals, sampler_point_clamp, GetWaterPrePassUV(uv), 0, int2(0, -1)).x;
		//float diff = (up + down) * 0.5 - center;

		//if((center == 0.0 || center == 1.0) && down == up) return down;
			
		//return mask;

}

inline float GetWaterClipMask(float2 uv, float2 offset = float2(0, 0))
{
	#ifdef KWS_USE_CLIP_MASKING
		uv = GetWaterMaskThicknessUV(uv);
		return SAMPLE_TEXTURE_LOD(KWS_WaterClipMaskRT, sampler_linear_clamp, uv  + KWS_WaterClipMaskDepthFrontRT_TexelSize.xy * offset, 0).x;
		
	#endif
	return 0;
}


inline float GetUnderwaterMask(float waterMask)
{
	return waterMask > 0.5;
}

inline bool GetSurfaceMask(float waterMask)
{
	return abs(waterMask - 0.25) < 0.01;
}

inline bool GetUnderwaterSurfaceMask(float waterMask)
{
	return abs(waterMask - 0.75) < 0.01;
}


inline float GetWaterHalfLineTensionMask(float2 uv)
{
	float2 scaledUV = GetWaterPrePassUV(uv);
	float intersectionMask = SAMPLE_TEXTURE_LOD(KWS_WaterIntersectionHalfLineTensionMaskRT, sampler_linear_clamp, scaledUV, 0).x;
	intersectionMask *= 1.4;
	if (intersectionMask >= 0.99) intersectionMask = saturate((1.2 - intersectionMask) * 5);

	return intersectionMask;
}


inline float GetWaterDepth(float2 uv)
{
	return SAMPLE_TEXTURE_LOD(KWS_WaterDepthRT, sampler_linear_clamp, GetWaterPrePassUV(uv), 0).x;
}

inline float GetWaterDepthFiltered(float2 uv)
{
	float4 depth = SAMPLE_TEXTURE_GATHER(KWS_WaterDepthRT, sampler_linear_clamp, GetWaterPrePassUV(uv));
	return KWS_MIN(depth);
}


inline float GetWaterBackfaceDepth(float2 uv)
{
	return SAMPLE_TEXTURE_LOD(KWS_WaterBackfaceDepthRT, sampler_linear_clamp, GetWaterBackfacePrePassUV(uv), 0).x;
}

//Front depth (x), Back depth (y)
inline float2 GetWaterMaskThickness(float2 uv)
{
	uv = GetWaterMaskThicknessUV(uv);
	float front = SAMPLE_TEXTURE_LOD(KWS_WaterClipMaskDepthFrontRT, sampler_linear_clamp, uv, 0).x;
	float back = SAMPLE_TEXTURE_LOD(KWS_WaterClipMaskDepthBackRT, sampler_linear_clamp, uv, 0).x;
	
	if (back > front)	front = 0;
	
	return float2(front, back);
}

inline uint GetWaterLocalZonesTransparent(float2 uv)
{
	return SAMPLE_TEXTURE_LOD(KWS_WaterPrePassRT0, sampler_linear_clamp, GetWaterPrePassUV(uv), 0).x * 100.0;
}



inline float2 GetWaterVolumeDepth(float2 uv, float sceneZ, float waterMask, float waterDepth, float2 waterClipMaskThickness, float waterClipMask)
{
	float isUnderwater = (float)GetUnderwaterMask(waterMask);

	float2 vol = 0;
	vol.x = waterDepth;
	vol.y = lerp(sceneZ, max(waterDepth, sceneZ), isUnderwater);

	#ifndef KWS_USE_CLIP_MASKING

	vol.x = lerp(vol.x, 1, isUnderwater);

	#else

	float  isUnderwaterAdditional = (float)GetUnderwaterMask(waterClipMask);
	
	float isOutsideCube = (float)(waterClipMaskThickness.x > 0.0);
	float isInsideCube  = (1.0 - isOutsideCube) * (float)(waterClipMaskThickness.y > 0.0);

	// underwater outside cube - no light (near)
	vol.x = lerp(vol.x, 1, isUnderwaterAdditional);

	// outside cube, underwater, looking into hole - volume starts at cube back face
	vol.x = lerp(vol.x, waterClipMaskThickness.y, isOutsideCube * isUnderwater * (1.0 - isUnderwaterAdditional));

	// outside cube, above water - clamp y to cube front face
	vol.y = lerp(vol.y, max(vol.y, waterClipMaskThickness.x), isOutsideCube * isUnderwaterAdditional);
		
	// inside cube - volume starts at far wall
	vol.x = lerp(vol.x, waterClipMaskThickness.y, isInsideCube);
	
	#endif

	// if volume start is behind scene - no volume
	vol.x = vol.x < sceneZ ? 0 : vol.x;
	return vol;
}


inline float2 GetWaterVolumeDepth(float2 uv, float sceneZ, float waterMask, float waterDepth)
{
	float2 waterClipMaskThickness = 0;
	float waterClipMask = 0;
	#ifdef KWS_USE_CLIP_MASKING
		waterClipMaskThickness = GetWaterMaskThickness(uv);
		waterClipMask = GetWaterClipMask(uv, float2(0, 1));
	#endif
	return GetWaterVolumeDepth(uv, sceneZ, waterMask, waterDepth, waterClipMaskThickness, waterClipMask);
}




inline float GetBoxExtrude(float3 pos, float3x3 rotationMatrix, float3 size)
{
	float3 rotatedPos = mul(rotationMatrix, pos).xyz;
	float3 d = abs(rotatedPos) - size;
	return length(max(d, 0)) + KWS_MAX(min(d, 0));
}

inline bool ShouldClipWaterSurface(float2 screenUV, float surfaceZ)
{
	float2 clipMaskThickness = GetWaterMaskThickness(screenUV);

	bool insideAir = (clipMaskThickness.x == 0.0 && clipMaskThickness.y > 0);
	bool surfaceToMaskIntersection = clipMaskThickness.y < surfaceZ &&  (insideAir || clipMaskThickness.x > surfaceZ);

	return surfaceToMaskIntersection;
}

inline bool ShouldClipWaterSurface(float2 screenUV, float surfaceZ, out float2 clipMaskThickness)
{
	clipMaskThickness = GetWaterMaskThickness(screenUV);

	bool insideAir = (clipMaskThickness.x == 0.0 && clipMaskThickness.y > 0);
	bool surfaceToMaskIntersection = clipMaskThickness.y < surfaceZ &&  (insideAir || clipMaskThickness.x > surfaceZ);

	return surfaceToMaskIntersection;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////





//////////////////////////////////////////////    VolumetricLighting_Pass    //////////////////////////////////////////////

DECLARE_TEXTURE(KWS_VolumetricLightRT);
DECLARE_TEXTURE(KWS_VolumetricLightAdditionalDataRT);
DECLARE_TEXTURE(KWS_VolumetricLightSurfaceRT);

float4 KWS_VolumetricLightRT_TexelSize;
float4 KWS_VolumetricLight_RTHandleScale;

DECLARE_TEXTURE(KWS_VolumetricLightRT_Last);
float4 KWS_VolumetricLightRT_Last_TexelSize;
float4 KWS_VolumetricLightRT_Last_RTHandleScale;

struct VolumetricLightAdditionalData
{
	half SurfaceDirShadow;
	half SceneDirShadow;
	half AdditionalLightsAttenuation;
};



inline float GetMaxRayDistanceRelativeToTransparent(float transparent)
{
	return min(KWS_MAX_TRANSPARENT, transparent * 1.5);
}


inline half4 GetVolumetricLight(float2 uv)
{
	return SAMPLE_TEXTURE_LOD(KWS_VolumetricLightRT, sampler_linear_clamp, saturate(uv), 0);
}

//(R) surface dir shadow, (G) scene dir shadow, (B) additional lights attenuation
inline VolumetricLightAdditionalData GetVolumetricLightAdditionalData(float2 uv)
{
	float3 rawData = SAMPLE_TEXTURE_LOD(KWS_VolumetricLightAdditionalDataRT, sampler_linear_clamp, saturate(uv), 0).xyz;
	#if defined(KWS_USE_DYNAMIC_WAVES)
		float4 shadowFix = SAMPLE_TEXTURE_GATHER(KWS_VolumetricLightAdditionalDataRT, sampler_linear_clamp, saturate(uv));
		rawData.x = rawData.x * shadowFix.x * shadowFix.y * shadowFix.z * shadowFix.w;
	#endif


	VolumetricLightAdditionalData volumeData;
	volumeData.SurfaceDirShadow = rawData.x;
	volumeData.SceneDirShadow = rawData.y;
	volumeData.AdditionalLightsAttenuation = rawData.z;
	return volumeData;
}


inline float3 GetVolumetricSurfaceLight(float2 uv)
{
	float3 surfaceLight = SAMPLE_TEXTURE_LOD(KWS_VolumetricLightSurfaceRT, sampler_linear_clamp, saturate(uv), 0).xyz;
	return surfaceLight;
}

inline float GetVolumeLightInDepthTransmitance(float waterSurfaceHeight, float transparent)
{
	float distanceToWaterSurface = waterSurfaceHeight - _WorldSpaceCameraPos.y - 1;
	float lightInDepthTransmitance = saturate(exp2(-0.5 * distanceToWaterSurface / max(2, transparent)) * 1.1 - 0.1);
	
	return lightInDepthTransmitance;
}


inline float GetVolumeLightSunAngleAttenuation(float3 lightDir, float3 normal = float3(0, 1, 0))
{
	float dotVal = dot(lightDir, normal);
	return smoothstep(-0.25, 1.0, dotVal);
	//return saturate(dot(lightDir, normal));

}

inline half3 GetVolumetricSurfaceLight(float4 volumeScattering, float3 normal, float exposure)
{
	float3 ambient = GetAmbientColor(exposure);
	float3 dirLight = GetVolumeLightSunAngleAttenuation(GetMainLightDir(), normal) * GetMainLightColor(exposure);

	return (ambient + dirLight * volumeScattering.a + volumeScattering.rgb);
}

float4 ComputeAbsorbtion(float transparent, float3 waterColor, float rayLength)
{
	float dyeOverrideFactor = dot(waterColor, 0.33);
	float3 absorbtionColor = lerp(float3(1.0, 1.0, 1.0) - waterColor, float3(1.0, 0.12, 0.02), dyeOverrideFactor);
	float absorbtionMultiplier = lerp(0.2, 0.1, saturate((transparent - KWS_MAX_TRANSPARENT) / KWS_MAX_TRANSPARENT));

	float3 finalAbsorbtion = max(float3(0.0005, 0.001, 0.025), exp2(-KWS_VOLUME_LIGHT_ABSORBTION_FACTOR * pow(rayLength, 1.5) * absorbtionColor * absorbtionMultiplier));

	rayLength = min(rayLength, min(KWS_MAX_TRANSPARENT, transparent));
	float extintionIntegralApproximationCoeff = KWS_Pow2(transparent * 0.2);

	float extinction = 1 - saturate(exp2(-rayLength / extintionIntegralApproximationCoeff + KWS_VOLUME_LIGHT_TRANSMITANCE_NEAR_OFFSET_FACTOR));

	return saturate(float4(finalAbsorbtion, extinction));
}

inline half4 GetVolumetricLightWithAbsorbtion(float2 uv, float2 refractionUV, float transparent, float3 tubidityColor, float3 waterColor, float3 sceneColor, float2 volumeDepth, float exposure, float3 sceneColorAdditiveAlbedo)
{
	float3 frontPos = GetWorldSpacePositionFromDepth(refractionUV, volumeDepth.x);
	float3 backPos = GetWorldSpacePositionFromDepth(refractionUV, volumeDepth.y);
	float rayLength = length(backPos - frontPos);
	if (volumeDepth.x == 0) rayLength = 0.1;

	float4 absorbtion = ComputeAbsorbtion(transparent, waterColor, rayLength);

	float waterSurfaceShadow = 1;
	half sceneAttenuation = GetVolumeLightInDepthTransmitance(KWS_WaterLevel, transparent);
	
	#ifdef KWS_USE_VOLUMETRIC_LIGHT
		float4 scattering = GetVolumetricLight(refractionUV);
		//float4 scatteringWithOffset = GetVolumetricLight(uv);
		//if ((scattering.x + scattering.y + scattering.z) <= 0.1) scattering = scatteringWithOffset;
		
		VolumetricLightAdditionalData volumeLightData = GetVolumetricLightAdditionalData(uv);

		sceneAttenuation = max(sceneAttenuation, volumeLightData.AdditionalLightsAttenuation);
		waterSurfaceShadow = volumeLightData.SurfaceDirShadow;
		//absorbtion.a = scattering.a;
	#else
		float phaseG = GetVolumeLightSunAngleAttenuation(GetMainLightDir());
		float3 sceneLight = 0.5 * GetAmbientColor(exposure) + GetMainLightColor(exposure) * phaseG;
		float4 scattering = half4(0.5 * tubidityColor * saturate(sceneLight), 1.0);
	#endif
	
	sceneColor *= sceneAttenuation;

	#ifdef KWS_SHARED_API_INCLUDED
		float3 albedo = sceneColorAdditiveAlbedo * dot(scattering.xyz, 0.33);
		float3 finalColor = lerp(absorbtion.rgb * sceneColor + clamp(albedo, 0, 2), scattering.xyz, absorbtion.a);
	#else
		float3 finalColor = lerp(absorbtion.rgb * sceneColor, scattering.xyz, absorbtion.a);
	#endif
	
	return float4(finalColor * sceneAttenuation, waterSurfaceShadow);
}



inline half4 GetVolumetricLightWithAbsorbtionByDistance(float2 uv, float2 refractionUV, float transparent, float3 tubidityColor, float3 dyeColor, float3 sceneColor, float rayLength, float exposure, float3 sceneColorAdditiveAlbedo)
{
	float4 absorbtion = ComputeAbsorbtion(transparent, dyeColor, rayLength);

	#ifdef KWS_USE_VOLUMETRIC_LIGHT
		float4 scattering = GetVolumetricLight(refractionUV);
		//float4 scatteringWithOffset = GetVolumetricLight(uv);
		//if ((scattering.x + scattering.y + scattering.z) <= 0.1) scattering = scatteringWithOffset;
		
		//absorbtion.a = scattering.a;
	#else
		float phaseG = GetVolumeLightSunAngleAttenuation(GetMainLightDir());
		float3 sceneLight = 0.5 * GetAmbientColor(exposure) + GetMainLightColor(exposure) * phaseG;
		float4 scattering = half4(0.5 * tubidityColor * saturate(sceneLight), 1.0);
	#endif

	#ifdef KWS_SHARED_API_INCLUDED
		float3 albedo = sceneColorAdditiveAlbedo * dot(scattering.xyz, 0.33);
		float3 finalColor = lerp(absorbtion.rgb * sceneColor + clamp(albedo, 0, 2), scattering.xyz, absorbtion.a);
	#else
		float3 finalColor = lerp(absorbtion.rgb * sceneColor, scattering.xyz, absorbtion.a);
	#endif
	
	return float4(finalColor, absorbtion.a);
}


inline half3 GetSurfaceLightWithAbsorbtion(float2 uv, float transparent, float3 tubidityColor, float3 dyeColor, float exposure)
{
	float dyeOverrideFactor = dot(dyeColor, 0.33);
	float3 absorbtionColor = lerp(float3(1.0, 1.0, 1.0) - dyeColor, float3(1.0, 0.13, 0.02), dyeOverrideFactor);
	float absorbtionMultiplier = lerp(1, 0.0, saturate((transparent) / KWS_MAX_TRANSPARENT));

	float extinction = 0;

	float3 finalAbsorbtion = absorbtionColor;

	float phaseG = GetVolumeLightSunAngleAttenuation(GetMainLightDir());
	float3 sceneLight = 0.5 * GetAmbientColor(exposure) + GetMainLightColor(exposure) * phaseG;
	float4 scattering = half4(0.5 * tubidityColor * saturate(sceneLight), 1.0);

	float3 finalColor = lerp(finalAbsorbtion * absorbtionMultiplier, scattering.xyz, 0.5);

	return finalColor;
}

inline half3 GetSurfaceLightWithAbsorbtionByDistance(float2 uv, float transparent, float3 tubidityColor, float3 dyeColor, float rayLength, float exposure)
{
	float dyeOverrideFactor = dot(dyeColor, 0.33);
	float3 absorbtionColor = lerp(float3(1.0, 1.0, 1.0) - dyeColor, float3(1.0, 0.13, 0.02), dyeOverrideFactor);
	float absorbtionMultiplier = lerp(1, 0.0, saturate((transparent) / KWS_MAX_TRANSPARENT));

	float extinction = 1 - saturate(exp2(-rayLength));

	float3 finalAbsorbtion = max(float3(0.0005, 0.001, 0.025), exp2(-rayLength * absorbtionColor));

	float phaseG = GetVolumeLightSunAngleAttenuation(GetMainLightDir());
	float3 sceneLight = 0.5 * GetAmbientColor(exposure) + GetMainLightColor(exposure) * phaseG;
	float4 scattering = half4(0.5 * tubidityColor * saturate(sceneLight), 1.0);

	float3 finalColor = lerp(finalAbsorbtion * absorbtionMultiplier, scattering.xyz, extinction);

	return finalColor;
}


inline half4 GetVolumetricLightLastFrame(float2 uv)
{
	//float2 scaledUV = GetRTHandleUV(uv, KWS_VolumetricLightRT_TexelSize.xy, 1.0, KWS_VolumetricLight_RTHandleScale.xy);
	//scaledUV += KWS_VolumetricLightRT_TexelSize.xy * 0.5;
	return SAMPLE_TEXTURE_LOD(KWS_VolumetricLightRT_Last, sampler_point_clamp, uv, 0);
}


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


//////////////////////////////////////////////    ScreenSpaceReflection_Pass    //////////////////////////////////////////////
DECLARE_TEXTURE(KWS_ScreenSpaceReflectionRT);
float4 KWS_ScreenSpaceReflectionRT_TexelSize;
float4 KWS_ScreenSpaceReflection_RTHandleScale;
#define KWS_ScreenSpaceReflectionMaxMip 4

inline float2 GetScreenSpaceReflectionNormalizedUV(float2 uv)
{
	uv = GetRTHandleUV(uv, KWS_ScreenSpaceReflectionRT_TexelSize.xy, 0.5, KWS_ScreenSpaceReflection_RTHandleScale.xy);
	//uv -= KWS_ScreenSpaceReflectionRT_TexelSize.xy * 0.5;
	return uv;
	//return clamp(uv, 0.001, 0.999) * KWS_ScreenSpaceReflection_RTHandleScale.xy;

}

inline half4 GetScreenSpaceReflection(float2 uv, float3 worldPos)
{
	float2 ssrUV = GetScreenSpaceReflectionNormalizedUV(uv);
	float mipLevel = CalcMipLevel(ssrUV * KWS_ScreenSpaceReflectionRT_TexelSize.zw);

	float distance = length(worldPos.xz - GetCameraAbsolutePosition().xz);
	float anisoScaleRelativeToDistance = saturate(distance * 0.05);

	float lod = min(mipLevel, KWS_ScreenSpaceReflectionMaxMip);
	//lod = anisoScaleRelativeToDistance * 2;

	ssrUV.y -= KWS_ScreenSpaceReflectionRT_TexelSize.y * 2;
	float4 res = SAMPLE_TEXTURE_LOD(KWS_ScreenSpaceReflectionRT, sampler_trilinear_clamp, ssrUV, lod);
	//res.a = saturate(res.a); //I use negative alpha to minimize edge bilinear interpolation artifacts.
	return res;
}

float KWS_ScreenSpaceBordersStretching;
inline half4 GetScreenSpaceReflectionWithStretchingMask(float2 uv, float3 worldPos)
{
	#if defined(STEREO_INSTANCING_ON)
		uv -= mul((float2x2)UNITY_MATRIX_V, float2(0, KWS_ReflectionClipOffset)).xy;
	#else
		uv.y -= KWS_ReflectionClipOffset;
	#endif

	float AngleStretch = max(0.25, saturate(-KWS_CameraForward.y * 1.5));
	float ScreenStretch = saturate(abs(uv.x * 2 - 1) - 0.8);
	float uvOffset = -AngleStretch * ScreenStretch * KWS_ScreenSpaceBordersStretching * 10;
	uv.x = uv.x * (1 + uvOffset * 2) - uvOffset;
	//float stretchingMask = 1 - abs(refl_uv.x * 2 - 1);
	//refl_uv.x = lerp(refl_uv.x * (1 - Test4.x * 2) + Test4.x, refl_uv.x, AngleStretch * ScreenStretch);
	return GetScreenSpaceReflection(uv, worldPos);
}


float GetAnisoScaleRelativeToWind(float windSpeed)
{
	float normalizedWind = saturate((windSpeed) / 10.0);
	float curved = 1.0 - pow(1.0 - normalizedWind, KWS_AnisoWindCurvePower);
	return (curved) * KWS_AnisoReflectionsScale;
}

float2 GetScreenSpaceReflectionUV(float3 reflDir, float2 orthoUV)
{
	UNITY_BRANCH
	if (unity_OrthoParams.w == 1.0) return orthoUV;
	else
	{
		reflDir.y = -reflDir.y;
		float4 projected = mul(UNITY_MATRIX_VP, float4(reflDir, 0));
		float2 uv = (projected.xy / projected.w) * 0.5f + 0.5f;
		
		#ifdef UNITY_UV_STARTS_AT_TOP
			uv.y = 1 - uv.y;
		#endif

		return uv;
	}
}



////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


//////////////////////////////////////////////    Planar Reflection    //////////////////////////////////////////////////////////
DECLARE_TEXTURE(KWS_PlanarReflectionRT);
int KWS_PlanarReflectionInstanceID;
float4 KWS_PlanarReflectionRT_TexelSize;
TextureCube KWS_CubemapReflectionRT;

#define KWS_PlanarReflectionMaxMip 4

inline half3 GetPlanarReflection(float2 refl_uv)
{
	float mipLevel = CalcMipLevel(refl_uv * KWS_PlanarReflectionRT_TexelSize.zw);
	float lod = min(mipLevel, KWS_PlanarReflectionMaxMip);
	return SAMPLE_TEXTURE_LOD(KWS_PlanarReflectionRT, sampler_trilinear_clamp, refl_uv, lod).xyz;
}

inline half3 GetPlanarReflectionRaw(float2 refl_uv)
{
	return SAMPLE_TEXTURE_LOD(KWS_PlanarReflectionRT, sampler_trilinear_clamp, refl_uv, 0).xyz;
}

inline half3 GetPlanarReflectionWithClipOffset(float2 refl_uv)
{
	#if defined(STEREO_INSTANCING_ON)
		refl_uv -= mul((float2x2)UNITY_MATRIX_V, float2(0, KWS_ReflectionClipOffset)).xy;
	#else
		refl_uv.y -= KWS_ReflectionClipOffset;
	#endif
	return GetPlanarReflection(refl_uv);
}


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




//////////////////////////////////////////////    Reflection Effects Pass    //////////////////////////////////////////////////////////

float Fresnel_IOR(float3 viewDir, float3 normal, float ior)
{
	float cosi = clamp(-1, 1, dot(viewDir, normal));
	float etai = 1, etat = ior;
	if (cosi > 0)
	{
		float temp = etat;
		etat = etai;
		etai = temp;
	}
	
	float sint = etai / etat * sqrt(max(0.f, 1 - cosi * cosi));
	
	if (sint >= 1)
	{
		return 1;
	}
	else
	{
		float cost = sqrt(max(0.f, 1 - sint * sint));
		cosi = abs(cosi);
		float Rs = ((etat * cosi) - (etai * cost)) / ((etat * cosi) + (etai * cost));
		float Rp = ((etai * cosi) - (etat * cost)) / ((etai * cosi) + (etat * cost));
		return (Rs * Rs + Rp * Rp) / 2;
	}
	// As a consequence of the conservation of energy, transmittance is given by:
	// kt = 1 - kr;

}

inline half SelfSmithJointGGXVisibilityTerm(half NdotL, half NdotV, half roughness)
{
	half a = roughness;
	half lambdaV = NdotL * (NdotV * (1 - a) + a);
	half lambdaL = NdotV * (NdotL * (1 - a) + a);

	return 0.5f / (lambdaV + lambdaL + 1e-5f);
}


inline half SelfGGXTerm(half NdotH, half roughness)
{
	half a2 = roughness * roughness;
	half d = (NdotH * a2 - NdotH) * NdotH + 1.0f; // 2 mad
	return 0.31830988618f * a2 / (d * d + 1e-7f); // This function is not intended to be running on Mobile,
	// therefore epsilon is smaller than what can be represented by half

}

inline half ComputeSpecular(half nl, half nv, half nh, half viewDistNormalized, half smoothness)
{
	half V = SelfSmithJointGGXVisibilityTerm(nl, nv, smoothness);
	half D = SelfGGXTerm(nh, viewDistNormalized * 0.1 + smoothness);

	half specularTerm = V * D;
	specularTerm = max(0, specularTerm * nl * KWS_SunStrength);

	return specularTerm;
}



float ComputeWaterFresnel(float3 normal, float3 viewDir, float minValue = 0.05)
{
	float x = 1 - saturate(dot(normal, viewDir));
	return minValue + (1 - minValue) * x * x * x * x ; //fresnel aproximation http://wiki.nuaj.net/images/thumb/1/16/Fresnel.jpg/800px-Fresnel.jpg

}

half3 ComputeSSS(float2 screenUV, float sssMask, half3 underwaterColor, half shadowMask, half transparent)
{
	float3 sssColor = underwaterColor;
	return sssMask * shadowMask * sssColor * 1;
}

half3 ComputeSunlight(half3 normal, half3 viewDir, float3 lightDir, float3 lightColor, half shadowMask, float viewDist, float waterFarDistance, half transparent)
{
	half3 halfDir = normalize(lightDir + viewDir);
	half nh = saturate(dot(normal, halfDir));
	half nl = saturate(dot(normal, lightDir));
	half lh = saturate(dot(lightDir, halfDir));
	half fresnel = saturate(dot(normal, viewDir));
	
	float viewDistNormalized = saturate(viewDist / (waterFarDistance * 2));
	half3 specular = ComputeSpecular(nl, fresnel, nh, viewDistNormalized, KWS_SunCloudiness);
	specular = clamp(specular * 10 - 2.5 * saturate(1 - KWS_SunCloudiness * 10), 0, KWS_SunMaxValue);
	//half sunset = saturate(0.01 + dot(lightDir, float3(0, 1, 0))) * 30;

	return shadowMask * specular * lightColor;
}


inline half3 ApplyShorelineWavesReflectionFix(float3 reflDir, half3 reflection, half3 underwaterColor)
{
	float r = 1 - saturate(dot(reflDir, float3(0, 1, 0)));
	return lerp(reflection, max(underwaterColor, reflection), KWS_Pow5(r));
}
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////






//////////////////////////////////////////////    Caustic_Pass    //////////////////////////////////////////////////////////
Texture2DArray KWS_CausticRTArray;
float4 KWS_CausticRTArray_TexelSize;
float KWS_CaustisDispersionStrength;

inline float3 GetCausticSlice(float2 uv, uint causticSlice)
{
	float3 caustic = 1;
	#ifdef USE_DISPERSION
		float2 offset = KWS_CausticRTArray_TexelSize.x * KWS_CaustisDispersionStrength;
		caustic.x = KWS_CausticRTArray.Sample(sampler_linear_repeat, float3(uv - offset, causticSlice)).x;
		caustic.y = KWS_CausticRTArray.Sample(sampler_linear_repeat, float3(uv, causticSlice)).x;
		caustic.z = KWS_CausticRTArray.Sample(sampler_linear_repeat, float3(uv + offset, causticSlice)).x;
	#else
		caustic = KWS_CausticRTArray.Sample(sampler_trilinear_repeat, float3(uv, causticSlice)).x;
	#endif

	return caustic;
}

inline half GetCausticOceanLod(float2 uv, uint causticSlice, float lod)
{
	return KWS_CausticRTArray.SampleLevel(sampler_linear_repeat, float3(uv, causticSlice), lod).x;
}

inline half GetCausticDynamicWavesZoneLod0(float2 uv, float lod)
{
	return KWS_CausticFrameLod0.SampleLevel(sampler_linear_repeat, uv, lod).x;
}

inline half GetCausticDynamicWavesZoneLod1(float2 uv, float lod)
{
	return KWS_CausticFrameLod0.SampleLevel(sampler_linear_repeat, uv, lod).x;
}

inline half3 GetOceanCaustic(uint2 pixels, float2 causticUV, float waterDepth)
{
	float3 domainSizes = float3(2.5, 10, 40.0);
	float3 cascadeDepths = float3(2.5, 10, 40.0);

	float noise = KWS_InterleavedGradientNoiseStatic(pixels);
	float depthJittered = waterDepth + noise * 0.5;
	
	int cascadeIdx = 0;
	if (depthJittered > cascadeDepths.x) cascadeIdx = 1;

	float2 uvScaled = causticUV / domainSizes[cascadeIdx];
	float2 gradx = ddx(causticUV) / domainSizes[cascadeIdx];
	float2 grady = ddy(causticUV) / domainSizes[cascadeIdx];

	float4 causticData = Tex2DArrayStochastic(KWS_CausticRTArray, sampler_linear_repeat, uvScaled, gradx, grady, cascadeIdx);
	float3 caustic = causticData.xyz - KWS_CAUSTIC_MULTIPLIER * 0.25;
	caustic = lerp(caustic.xxx, caustic.xyz, (float)KWS_UseCausticDispersion);
	
	caustic *= saturate(waterDepth * waterDepth);

	float3 dispersionCaustic = clamp(caustic * KWS_CausticStrength * 5, lerp(0, -1, causticData.w).xxx, 10);
	float simpleCaustic = caustic.x * lerp(1, KWS_CausticStrength, saturate(caustic.x*1000)) * 5;
	
	return  lerp(simpleCaustic.xxx, dispersionCaustic, (float)KWS_UseCausticDispersion);
}

inline half3 GetDynamicWavesZoneCaustic(float2 causticUV, float waterDepth, float2 flowDirection, float dynamicWavesHeight, float speedMultiplier, float noiseMask)
{
	float2 causticFlow = flowDirection * 0.8;
	
	float3 caustic0 = Texture2DSampleFlowmapJump(KWS_CausticFrameLod0, sampler_linear_repeat, (causticUV / 4) + flowDirection * flowDirection * 0.5, causticFlow, KWS_Time * speedMultiplier * 2.0, 1.0).xyz;
	float3 caustic1 = Texture2DSampleFlowmapJump(KWS_CausticFrameLod1, sampler_linear_repeat, (causticUV / 6) + flowDirection * flowDirection * 0.5, causticFlow, KWS_Time * speedMultiplier * 1.0, 1.0).xyz - KWS_CAUSTIC_MULTIPLIER * 0.25;
	float3 caustic = (lerp(caustic0.xyz, caustic1.xyz, saturate(KWS_Pow3(waterDepth * 0.25))));
	
	float causticSignMask =  saturate(caustic.x * 1000);

	caustic *= saturate(waterDepth * waterDepth) * (0.5 + saturate(dynamicWavesHeight * 4));
	caustic *= lerp(1, KWS_CausticStrength, causticSignMask) * 5;
	caustic *= lerp(noiseMask, saturate(noiseMask + 0.2), saturate(waterDepth * 0.1));

	return caustic;
}


inline float PreventOceanHeightTerrainIntersection(float terrainLevel, float oceanHeight)
{
	// terrainLevel relative to water level:
	// terrainLevel < 0 = bottom below water
	// terrainLevel > 0 = terrain above water

	float waterDepth = max(-terrainLevel, 0.0);

	// More clearance in shallow water, less in deeper water.
	float clearance = lerp(0.08, 0.02, saturate(waterDepth / 1.5));
	float floorHeight = terrainLevel + clearance;

	float h = oceanHeight;
	float penetration = floorHeight - h;

	if (penetration > 0.0)
	{
		// Soft lift first, чтобы не было прямого hard cut.
		float liftMask = smoothstep(0.0, 0.18, penetration);
		h += penetration * liftMask;
	}

	// Tiny hard guarantee against small remaining intersections.
	h = max(h, terrainLevel + 0.015);

	return h;
}

inline float GetDynamicWavesAttenuatedOceanHeight(
	float terrainLevel,
	float oceanHeight,
	float shorelineMask,
	float zoneFade)
{
	float waterDepth = max(-terrainLevel, 0.0);

	float depthFade = smoothstep(0.15, 2.5, waterDepth);
	float fade = depthFade * shorelineMask * saturate(zoneFade);
	float h = oceanHeight * fade;

	float floorHeight = terrainLevel + 0.02;
	float penetration = floorHeight - h;

	if (penetration > 0.0)
	{
		float lift = penetration * smoothstep(0.0, 0.35, penetration);
		h += lift;
	}

	return h;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



//////////////////////////////////////////////    Flowmap_Pass    /////////////////////////////////////////////////////////////////////////////////////////


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



//////////////////////////////////////////////    Otrho depth    //////////////////////////////////////////////////////////
Texture2D KWS_WaterOrthoDepthRT;
Texture2D KWS_WaterOrthoDepthSDF;
Texture2D KWS_WaterOrthoDepthBackfaceRT;

float3 KWS_OrthoDepthPos;
float4 KWS_OrthoDepthNearFarSize;
float4x4 KWS_OrthoDepthCameraMatrix;
float4 KWS_WaterOrthoDepthRT_TexelSize;
float4 KWS_WaterOrthoDepthSDF_TexelSize;

float4 ReconstructWaterOrthoDepthPos(float3 worldPos)
{
	return mul(KWS_OrthoDepthCameraMatrix, float4(worldPos, 1));
}

float2 GetWaterOrthoDepthUV(float3 worldPos)
{
	return (worldPos.xz - KWS_OrthoDepthPos.xz) / KWS_OrthoDepthNearFarSize.zw + 0.5;
}

float GetWaterOrthoDepth(float2 uv)
{
	float cameraHeight = KWS_OrthoDepthNearFarSize.x;
	float far = KWS_OrthoDepthNearFarSize.y;
	float terrainDepth = KWS_WaterOrthoDepthRT.SampleLevel(sampler_linear_clamp, uv, 0).r;
	
	float worldDepth = cameraHeight - (1.0 - terrainDepth) * far;
	return worldDepth;
}

//float GetWaterOrthoDepthBicubic(float2 uv)
//{
//	float cameraHeight = KWS_OrthoDepthNearFarSize.x;
//	float far = KWS_OrthoDepthNearFarSize.y;
//	float terrainDepth = KWS_WaterOrthoDepthRT.SampleLevel(sampler_linear_clamp, uv, 0).r;

//	float worldDepth = cameraHeight - (1.0 - terrainDepth) * far;
//	return worldDepth;
//}


struct SimulationZoneSDF
{
	float sdf;
	float2 direction;
};

SimulationZoneSDF GetSimulationZoneSDF(float2 uv)
{
	//if (IsOutsideUvBorders(uv)) return 0;
	SimulationZoneSDF zoneSdf;
	float3 data = KWS_WaterOrthoDepthSDF.SampleLevel(sampler_linear_clamp, uv, 0).xyz;
	zoneSdf.direction = SafeNormalize2(data.xy * 2.0 - 1.0);
	zoneSdf.sdf = data.z;
	return zoneSdf;
}


float GetWaterOrthoDepth(float3 worldPos)
{
	float2 uv = GetWaterOrthoDepthUV(worldPos);
	return GetWaterOrthoDepth(uv);
}

SimulationZoneSDF GetSimulationZoneSDF(float3 worldPos)
{
	float2 uv = GetWaterOrthoDepthUV(worldPos);
	return GetSimulationZoneSDF(uv);
}


float2 ComputeNearshoreWaveDirection(float2 oceanDir, SimulationZoneSDF zoneSdf, float refractionDistance, float refractionStrength)
{
	float shoreMask = 1.0 - saturate(abs(zoneSdf.sdf) / max(refractionDistance, 1e-5));
	shoreMask *= shoreMask;

	float2 shoreDir = -zoneSdf.direction;
	float2 waveDir = normalize(lerp(oceanDir, shoreDir, shoreMask * refractionStrength));
	return waveDir;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////









//////////////////////////////////////////////    Local Water Zones   /////////////////////////////////////////////////////////////
#if defined(KWS_USE_LOCAL_WATER_ZONES)

#define MAX_VISIBLE_LOCAL_ZONES 8

struct LocalZoneData
{
	float3 center;
	float3 halfSize;
	float2x2 rotationMatrix; // (c0.x, c0.y, c1.x, c1.y)
	uint id;
	float2 uv;

	float overrideColorSettings;
	float transparent;
	float4 waterColor;
	float4 turbidityColor;
	float useSphereBlending;
	
	float overrideWindSettings;
	float windStrengthMultiplier;
	float windEdgeBlending;
	        
	float  overrideHeight;
	float heightEdgeBlending;
	float clipWaterBelowZone;
};


StructuredBuffer<int> KWS_GlobalTileIndices_LocalZone;
StructuredBuffer<uint2> KWS_TileIndexRanges_LocalZone;
StructuredBuffer<LocalZoneData> KWS_ZoneData_LocalZone;


float2 _GridSize_LocalZone;
float3 _WorldMin_LocalZone;
float3 _WorldSize_LocalZone;
uint KWS_WaterLocalZonesCount;

bool GetTileRange_LocalZone(float3 worldPos, out uint offset, out uint count)
{
	offset = 0;
	count = 0;
	
	float3 localPos = worldPos - _WorldMin_LocalZone.xyz;
	float2 uvGrid = float2(localPos.x / _WorldSize_LocalZone.x, localPos.z / _WorldSize_LocalZone.z);

	int x = floor(uvGrid.x * _GridSize_LocalZone.x);
	int y = floor(uvGrid.y * _GridSize_LocalZone.y);

	if (x < 0 || x >= _GridSize_LocalZone.x || y < 0 || y >= _GridSize_LocalZone.y)
	{
		return false;
	}
	else //metal require it
	{
		uint tileIndex = y * _GridSize_LocalZone.x + x;

		uint2 range = KWS_TileIndexRanges_LocalZone[tileIndex];
		offset = range.x;
		uint size = range.y;
		count = offset +size;
		return true;
	}

	return false;
}

bool GetWaterZone_LocalZone(float3 worldPos, uint idx, out LocalZoneData zone)
{
	zone = (LocalZoneData)0;
	
	int zoneID = KWS_GlobalTileIndices_LocalZone[idx];
	if (zoneID == -1) return false;
	else
	{
		zone = KWS_ZoneData_LocalZone[zoneID];

		float2 pos = worldPos.xz - zone.center.xz;
		float2 localPos = mul(pos, zone.rotationMatrix);

		if (abs(localPos.x) > zone.halfSize.x || abs(localPos.y) > zone.halfSize.z)
		{
			return false;
		}
		else
		{
			zone.uv = localPos / zone.halfSize.xz * 0.5 + 0.5;
			return true;
		}
	}
	return false;
}


float GetLocalWaterZoneBlendFactor(float2 uv, float blendFactor)
{
	uv = abs(uv * 2 - 1);
	float uvEdgeMask = max(uv.x, uv.y);
	uvEdgeMask = 1 - saturate(pow(uvEdgeMask * 1.01, blendFactor * 10));
	return uvEdgeMask;
}

float GetLocalWaterZoneSphereBlendFactor(float2 uv, float blendFactor)
{
	float2 localUV = uv * 2 - 1;
	float dist = length(localUV);
	float mask = 1 - saturate(pow(dist * 1.01, blendFactor * 4));
	return mask;
}

float GetLocalWaterZoneWindBlendFactor(float2 uv, float blendFactor)
{
	uv = abs(uv * 2 - 1);
	float uvEdgeMask = max(uv.x, uv.y);
	float fade = saturate(pow(uvEdgeMask * 1.01, blendFactor * 10));
	return lerp(1, fade, blendFactor);
}

void EvaluateBlendedZoneData(inout LocalZoneData blendedZone, float3 rayStart, float3 rayDir, float rayLengthToSurface, float surfaceHeight, float noise)
{
	UNITY_LOOP
	for (uint zoneIdx = 0; zoneIdx < KWS_WaterLocalZonesCount; zoneIdx++)
	{
		LocalZoneData zone = KWS_ZoneData_LocalZone[zoneIdx];
		float3 surfaceOffset = float3(0, max(0, zone.center.y + zone.halfSize.y - surfaceHeight) * 0.5f, 0);

		float tEntry = 0;

		float2 distanceToBox = abs(mul((rayStart.xz - zone.center.xz), zone.rotationMatrix)) / zone.halfSize.xz;
		float distanceToBorder = max(distanceToBox.x, distanceToBox.y);
		float zoneMinHeight = zone.center.y - zone.halfSize.y;

		if (zone.overrideHeight > 0.5 && zone.clipWaterBelowZone && distanceToBorder < 1.1 && rayStart.y < zoneMinHeight && rayStart.y > KWS_WaterLevel)
		{
			blendedZone.transparent = 0;
			return;
		}
		
		if (zone.overrideColorSettings > 0.5)
		{
			float density = 0;
			if (zone.useSphereBlending > 0.5)
			{
				density = KWS_SDF_SphereDensity(rayStart, rayDir, zone.center, zone.halfSize.x, rayLengthToSurface, tEntry);
			}
			else
			{
				float2 boxSDF = KWS_SDF_IntersectionBox(rayStart , rayDir, zone.rotationMatrix, zone.center, zone.halfSize);
				density = boxSDF.x < boxSDF.y && boxSDF.y > 0 && boxSDF.x < rayLengthToSurface;
				tEntry = boxSDF.x;
			}
			
			
			if (density > 0)
			{
				density = saturate(density * 2);
				density = lerp(0.0, density, saturate(blendedZone.transparent / max(1.0, tEntry)));
			
				blendedZone.transparent = lerp(blendedZone.transparent, zone.transparent, density);
				blendedZone.turbidityColor = lerp(blendedZone.turbidityColor, zone.turbidityColor, density);
				blendedZone.waterColor = lerp(blendedZone.waterColor, zone.waterColor, density);
			}
		}
		
		
	}
}

void EvaluateBlendedZoneDataWithHeight(inout LocalZoneData blendedZone, float3 rayStart, float3 rayDir, float rayLengthToSurface, float surfaceHeight, out float maxHeightOffset, out float offsetBlending)
{
	maxHeightOffset = -100000;
	offsetBlending = 0;
	
	UNITY_LOOP
	for (uint zoneIdx = 0; zoneIdx < KWS_WaterLocalZonesCount; zoneIdx++)
	{
		LocalZoneData zone = KWS_ZoneData_LocalZone[zoneIdx];
		float3 surfaceOffset = float3(0, max(0, zone.center.y + zone.halfSize.y - surfaceHeight) * 0.5f, 0);

		float tEntry = 0;

		float2 distanceToBox = abs(mul((rayStart.xz - zone.center.xz), zone.rotationMatrix)) / zone.halfSize.xz;
		float distanceToBorder = max(distanceToBox.x, distanceToBox.y);
		float zoneMinHeight = zone.center.y - zone.halfSize.y;

		if (zone.overrideHeight > 0.5 && zone.clipWaterBelowZone && distanceToBorder < 1.1 && rayStart.y < zoneMinHeight && rayStart.y > KWS_WaterLevel)
		{
			blendedZone.transparent = 0;
			return;
		}
		
		if (zone.overrideColorSettings > 0.5)
		{
			float density = 0;
			if (zone.useSphereBlending > 0.5)
			{
				density = KWS_SDF_SphereDensity(rayStart, rayDir, zone.center, zone.halfSize.x, rayLengthToSurface, tEntry);
			}
			else
			{
				float2 boxSDF = KWS_SDF_IntersectionBox(rayStart , rayDir, zone.rotationMatrix, zone.center, zone.halfSize);
				density = boxSDF.x < boxSDF.y && boxSDF.y > 0 && boxSDF.x < rayLengthToSurface;
				tEntry = boxSDF.x;
			}
			
			
			if (density > 0)
			{
				density = saturate(density * 2);
				density = lerp(0, density, saturate(blendedZone.transparent / max(1, tEntry)));
			
				blendedZone.transparent = lerp(blendedZone.transparent, zone.transparent, density);
				blendedZone.turbidityColor = lerp(blendedZone.turbidityColor, zone.turbidityColor, density);
				blendedZone.waterColor = lerp(blendedZone.waterColor, zone.waterColor, density);
			}
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


#endif

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////









//////////////////////////////////////////////    Dynamic waves    //////////////////////////////////////////////////////////

Texture2DArray KWS_DynamicWavesMap;
Texture2DArray KWS_DynamicWavesAdditionalMap;
Texture2DArray KWS_DynamicWavesNormalAndWetMap;
Texture2DArray KWS_DynamicWavesAdvectedUVMap;
Texture2DArray KWS_DynamicWavesColorMap;

#define MAX_DYNAMIC_WAVES_MAP_LODS 8
float3 KWS_DynamicWavesMapPos;
float KWS_DynamicWavesMapLodSizes[MAX_DYNAMIC_WAVES_MAP_LODS];
float KWS_DynamicWavesMapLodSizesInverted[MAX_DYNAMIC_WAVES_MAP_LODS];

Texture2D KWS_RainMap;


float4 KWS_DynamicWavesMap_TexelSize;

struct DynamicWavesAdditionalData
{
	uint zoneID;
	float shorelineMask;
	float foamMask;
	float zoneFade;
};

struct DynamicWavesWetData
{
	float wetMask;
	float wetMapDepth;
};


uint GetDynamicWavesLodIndex(float distanceToCamera)
{
	uint lodIndex = 0;
	UNITY_UNROLL
	for (uint i = 0; i < (uint)MAX_DYNAMIC_WAVES_MAP_LODS; i++)
	{
		lodIndex += (distanceToCamera >= KWS_DynamicWavesMapLodSizes[i] * 0.5) ? 1u : 0u;
	}
	return min(lodIndex, (uint)(MAX_DYNAMIC_WAVES_MAP_LODS - 1u));
}

float3 GetDynamicWavesMapUV(float3 worldPos, float distanceToCamera)
{
	float2 localPos = worldPos.xz - KWS_DynamicWavesMapPos.xz;
    float cameraBiasOffset = 5;
	uint lodIndex = GetDynamicWavesLodIndex(distanceToCamera + cameraBiasOffset);
	float invSize  = KWS_DynamicWavesMapLodSizesInverted[lodIndex];
    
	float2 uv = localPos * invSize + 0.5;
	return float3(uv, lodIndex);
}


float4 GetDynamicWavesMap(float3 uv)
{
	return KWS_DynamicWavesMap.SampleLevel(sampler_linear_clamp, uv, 0);
}

float4 GetDynamicWavesMapBicubic(float3 uv)
{
	return Texture2DArraySampleLevelBicubic(KWS_DynamicWavesMap, sampler_linear_clamp, uv.xy, KWS_DynamicWavesMap_TexelSize, uv.z, 0);
}

DynamicWavesAdditionalData GetDynamicWavesAdditionalMap(float3 uv)
{
	float4 rawData = KWS_DynamicWavesAdditionalMap.SampleLevel(sampler_linear_clamp, uv, 0);
	
	#if defined(SHADER_API_PS5) || defined (SHADER_API_VULKAN) || defined (SHADER_API_XBOXONE)
	rawData.yzw *= PS5_BOOL_FIX;
	#endif
	
	DynamicWavesAdditionalData data;
	data.zoneID = Unpack_R8_UNorm_ToUint(rawData.x);
	data.shorelineMask = rawData.y;
	data.foamMask = rawData.z;
	data.zoneFade= rawData.w;

	return data;
}

DynamicWavesAdditionalData GetDynamicWavesAdditionalMapBicubic(float3 uv)
{
	float4 rawData =  Texture2DArraySampleLevelBicubic(KWS_DynamicWavesAdditionalMap, sampler_linear_clamp, uv.xy, KWS_DynamicWavesMap_TexelSize, uv.z, 0);

	#if defined(SHADER_API_PS5) || defined (SHADER_API_VULKAN) || defined (SHADER_API_XBOXONE)
		rawData.yzw *= PS5_BOOL_FIX;
	#endif
	
	DynamicWavesAdditionalData data;
	data.zoneID = Unpack_R8_UNorm_ToUint(rawData.x);
	data.shorelineMask = rawData.y;
	data.foamMask = rawData.z;
	data.zoneFade= rawData.w;
	
	return data;
}

float3 GetDynamicWavesNormalMap(float3 uv)
{
	float2 data = KWS_DynamicWavesNormalAndWetMap.SampleLevel(sampler_linear_clamp, uv, 0).xy;
	return DecodeNormalXZ(data);
}

float3 GetDynamicWavesNormalMapBicubic(float3 uv)
{
	float2 data =  Texture2DArraySampleLevelBicubic(KWS_DynamicWavesNormalAndWetMap, sampler_linear_clamp, uv.xy, KWS_DynamicWavesMap_TexelSize, uv.z, 0).xy;
	return DecodeNormalXZ(data);
}


DynamicWavesWetData GetDynamicWavesWetMap(float3 uv)
{
	float2 rawData =  KWS_DynamicWavesNormalAndWetMap.SampleLevel(sampler_linear_clamp, uv, 0).zw;

	DynamicWavesWetData data;
	data.wetMask = rawData.x;
	data.wetMapDepth = rawData.y;
	return data;
}

DynamicWavesWetData GetDynamicWavesWetMapBicubic(float3 uv)
{
	float2 rawData = Texture2DArraySampleLevelBicubic(KWS_DynamicWavesNormalAndWetMap, sampler_linear_clamp, uv.xy, KWS_DynamicWavesMap_TexelSize, uv.z, 0).zw;
	
	DynamicWavesWetData data;
	data.wetMask = rawData.x;
	data.wetMapDepth = rawData.y;
	return data;
}


float4 GetDynamicWavesAdvectedUVMap(float3 uv)
{
	return KWS_DynamicWavesAdvectedUVMap.SampleLevel(sampler_linear_clamp, uv, 0).xyzw;
}

float4 GetDynamicWavesAdvectedUVMapBicubic(float3 uv)
{
	return Texture2DArraySampleLevelBicubic(KWS_DynamicWavesAdvectedUVMap, sampler_linear_clamp, uv.xy, KWS_DynamicWavesMap_TexelSize, uv.z, 0).xyzw;
}

float4 GetDynamicWavesColorMap(float3 uv)
{
	return KWS_DynamicWavesColorMap.SampleLevel(sampler_linear_clamp, uv, 0);
}

#define MAX_ZONES_PER_TILE 8


Texture2D KWS_DynamicWaves;
Texture2D KWS_DynamicWavesNormals;
Texture2D KWS_DynamicWavesLast;
Texture2D KWS_DynamicWavesAdditionalDataRT;
Texture2D KWS_DynamicWavesColorDataRT;
Texture2D KWS_DynamicWavesMaskRT;
Texture2D KWS_DynamicWavesMaskColorRT;
Texture2D KWS_DynamicWavesDepthMask;
Texture2D KWS_DynamicWavesAdvectedUV;


Texture2D KWS_DynamicWaves0;
Texture2D KWS_DynamicWaves1;
Texture2D KWS_DynamicWaves2;
Texture2D KWS_DynamicWaves3;
Texture2D KWS_DynamicWaves4;
Texture2D KWS_DynamicWaves5;
Texture2D KWS_DynamicWaves6;
Texture2D KWS_DynamicWaves7;

Texture2D KWS_DynamicWavesNormals0;
Texture2D KWS_DynamicWavesNormals1;
Texture2D KWS_DynamicWavesNormals2;
Texture2D KWS_DynamicWavesNormals3;
Texture2D KWS_DynamicWavesNormals4;
Texture2D KWS_DynamicWavesNormals5;
Texture2D KWS_DynamicWavesNormals6;
Texture2D KWS_DynamicWavesNormals7;

Texture2D KWS_DynamicWavesAdditionalDataRT0;
Texture2D KWS_DynamicWavesAdditionalDataRT1;
Texture2D KWS_DynamicWavesAdditionalDataRT2;
Texture2D KWS_DynamicWavesAdditionalDataRT3;
Texture2D KWS_DynamicWavesAdditionalDataRT4;
Texture2D KWS_DynamicWavesAdditionalDataRT5;
Texture2D KWS_DynamicWavesAdditionalDataRT6;
Texture2D KWS_DynamicWavesAdditionalDataRT7;

Texture2D KWS_DynamicWavesColorDataRT0;
Texture2D KWS_DynamicWavesColorDataRT1;
Texture2D KWS_DynamicWavesColorDataRT2;
Texture2D KWS_DynamicWavesColorDataRT3;
Texture2D KWS_DynamicWavesColorDataRT4;
Texture2D KWS_DynamicWavesColorDataRT5;
Texture2D KWS_DynamicWavesColorDataRT6;
Texture2D KWS_DynamicWavesColorDataRT7;

Texture2D KWS_DynamicWavesDepthMask0;
Texture2D KWS_DynamicWavesDepthMask1;
Texture2D KWS_DynamicWavesDepthMask2;
Texture2D KWS_DynamicWavesDepthMask3;
Texture2D KWS_DynamicWavesDepthMask4;
Texture2D KWS_DynamicWavesDepthMask5;
Texture2D KWS_DynamicWavesDepthMask6;
Texture2D KWS_DynamicWavesDepthMask7;

Texture2D KWS_DynamicWavesAdvectedUV0;
Texture2D KWS_DynamicWavesAdvectedUV1;
Texture2D KWS_DynamicWavesAdvectedUV2;
Texture2D KWS_DynamicWavesAdvectedUV3;
Texture2D KWS_DynamicWavesAdvectedUV4;
Texture2D KWS_DynamicWavesAdvectedUV5;
Texture2D KWS_DynamicWavesAdvectedUV6;
Texture2D KWS_DynamicWavesAdvectedUV7;
Texture2D KWS_DynamicWavesAdvectedUV8;

float4 KWS_DynamicWaves0_TexelSize;
float4 KWS_DynamicWaves1_TexelSize;
float4 KWS_DynamicWaves2_TexelSize;
float4 KWS_DynamicWaves3_TexelSize;
float4 KWS_DynamicWaves4_TexelSize;
float4 KWS_DynamicWaves5_TexelSize;
float4 KWS_DynamicWaves6_TexelSize;
float4 KWS_DynamicWaves7_TexelSize;

float4 KWS_DynamicWaves_TexelSize;
float4 KWS_DynamicWavesDepthMask_TexelSize;
float4 KWS_DynamicWavesAdditionalDataRT_TexelSize;

uint KWS_DynamicWavesZoneID;
float3 KWS_DynamicWavesZonePosition;
float3 KWS_DynamicWavesZoneSize;
float4 KWS_DynamicWavesZoneRotationMatrix;
float KWS_DynamicWavesZoneFlowSpeedMultiplier;
float KWS_SimulationTimeScale;
float4 KWS_DynamicWavesZoneFoamData;

float KWS_DynamicWavesRainDropsIntensity;
uint KWS_DynamicWavesUseFoamTexture;
uint KWS_DynamicWavesUseAdvectedUV;

float3 KWS_DynamicWavesZonePositionArray[MAX_ZONES_PER_TILE];
float3 KWS_DynamicWavesZoneSizeArray[MAX_ZONES_PER_TILE];
float4 KWS_DynamicWavesOrthoDepthNearFarSizeArray[MAX_ZONES_PER_TILE];
float4 KWS_DynamicWavesZoneRotationMatrixArray[MAX_ZONES_PER_TILE];

float KWS_DynamicWavesBakingMode;
float KWS_BakedZoneInPreviewMode;

#define DYNAMIC_WAVE_COLOR_MAX_TRANSPARENT 4
#define DYNAMIC_WAVE_PROCEDURAL_MASK_TYPE_SPHERE 1

#define KWS_DYNAMIC_WAVE_PARTICLE_TYPE_FOAM 0
#define KWS_DYNAMIC_WAVE_PARTICLE_TYPE_SPLASH 1
#define KWS_DYNAMIC_WAVE_PARTICLE_TYPE_SPLASH_SURFACE 2


struct KWS_DynamicWavesMask
{
	uint zoneInteractionType;
	float force;
	float waterHeight;
	uint useColor;

	float4 size;
	float4 position;

	float3 forceDirection;
	uint useWaterIntersection;

	float4 color;
	float4x4 matrixTRS;
};

struct FoamParticle
{
	float3 position;
	float initialRandom01;

	float3 prevPosition;
	float prevLifetime;

	float3 velocity;
	float currentLifetime;

	float isFreeMoving;
	float shorelineMask;
	float maxLifeTime;
	float _pad;
};

struct SplashParticle
{
	float initialRandom01;
	float3 position;

	float3 velocity;
	float currentLifetime;

	float shorelineMask;
	float distanceToSurface;
	float uvOffset;
	float initialSpeed;

	float3 prevPosition;
	float prevLifetime;
};


struct ZoneData
{
	float3 center;
	float3 halfSize;
	float2x2 rotationMatrix; // (c0.x, c0.y, c1.x, c1.y)
	uint id;
	float2 uv;
	float flowSpeedMultiplier;
	float particlesAlpha;
	float timeScale;
	uint useAdvectedUV;
	uint useFoamTexture;
	float4 foamData;
	float rainDropsIntensity;
};

#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_USE_COLORED_DYNAMIC_WAVES)

StructuredBuffer<KWS_DynamicWavesMask> KWS_DynamicWavesMaskBuffer;
StructuredBuffer<SplashParticle> KWS_SplashParticlesBuffer;
StructuredBuffer<FoamParticle> KWS_FoamParticlesBuffer;
StructuredBuffer<ZoneData> KWS_ZoneData;

#endif

float2 _GridSize;
float3 _WorldMin;
float3 _WorldSize;
uint KWS_WaterDynamicWavesZonesCount;

float3 KWS_ZonePosition;
float3 KWS_ZoneSize;
float4 KWS_ZoneRotation;


inline float2 RotateDynamicWavesCoord(float2 coord)
{
	return float2(dot(coord, KWS_DynamicWavesZoneRotationMatrix.xy), dot(coord, KWS_DynamicWavesZoneRotationMatrix.zw));
}

inline float2 RotateDynamicWavesCoord(uint id, float2 coord)
{
	float4 mat = KWS_DynamicWavesZoneRotationMatrixArray[id];
	float2x2 invRot = float2x2(mat.x, mat.z, mat.y, mat.w);
	float2 rotated = mul(coord, invRot);

	return rotated;
}


inline float2 RotateDynamicWavesCoordInverse(float2 coord)
{
	return float2(dot(coord, KWS_DynamicWavesZoneRotationMatrix.xz), dot(coord, KWS_DynamicWavesZoneRotationMatrix.yw));
}

inline float2 GetDynamicWavesUV(float3 worldPos)
{
	return (worldPos.xz - KWS_DynamicWavesZonePosition.xz) / KWS_DynamicWavesZoneSize.xz + 0.5;
}

inline float2 GetDynamicWavesUVRotated(float3 worldPos)
{
	float2 local = worldPos.xz - KWS_DynamicWavesZonePosition.xz;

	float2x2 invRot = float2x2(
		KWS_DynamicWavesZoneRotationMatrix.x, KWS_DynamicWavesZoneRotationMatrix.z,
		KWS_DynamicWavesZoneRotationMatrix.y, KWS_DynamicWavesZoneRotationMatrix.w
	);

	float2 rotated = mul(invRot, local);
	return rotated / KWS_DynamicWavesZoneSize.xz + 0.5;
}


float EncodeDynamicWavesHeight(float worldHeight)
{
	float minY = KWS_DynamicWavesZonePosition.y - KWS_DynamicWavesZoneSize.y;
	float maxY = KWS_DynamicWavesZonePosition.y + KWS_DynamicWavesZoneSize.y;
	return saturate((worldHeight - minY) / (maxY - minY));
}

float DecodeDynamicWavesHeight( float worldHeight, float3 zonePos, float3 zoneSize)
{

	return lerp(zonePos.y - zoneSize.y, zonePos.y + zoneSize.y, worldHeight);
}

float DecodeDynamicWavesHeight(float worldHeight)
{
	float zoneCenter = KWS_DynamicWavesZonePosition.y;
	float zoneSize = KWS_DynamicWavesZoneSize.y;

	return lerp(zoneCenter - zoneSize, zoneCenter + zoneSize, worldHeight);
}

inline float4 GetDynamicWavesMask(float2 uv)
{
	float threshold = 4.0 / 255.0;
	float4 data = KWS_DynamicWavesMaskRT.SampleLevel(sampler_linear_clamp, uv, 0);
	data.xyz = data.xyz * 2 - 1;
	data.xyz *= step(threshold, abs(data.xyz));
	return data;
}


float GetWaterToZoneHeight()
{
	return abs(KWS_DynamicWavesZonePosition.y + KWS_DynamicWavesZoneSize.y * 0.5 - KWS_WaterLevel);
}

float PackWaterZoneDepth(float w)
{
	float yMin = KWS_DynamicWavesZonePosition.y - KWS_DynamicWavesZoneSize.y * 0.5;
	float yMax = KWS_DynamicWavesZonePosition.y + KWS_DynamicWavesZoneSize.y * 0.5;
	return saturate((w - yMin) / (yMax - yMin));
}

float UnpackWaterZoneDepth(float w)
{
	float yMin = KWS_DynamicWavesZonePosition.y - KWS_DynamicWavesZoneSize.y * 0.5;
	float yMax = KWS_DynamicWavesZonePosition.y + KWS_DynamicWavesZoneSize.y * 0.5;
	return w *(yMax-yMin) + yMin;
}

float4 PackSimulation(float4 value)
{
	value.xy = clamp(value.xy, -10, 10) / 20.0 + 0.5;
	value.z = (clamp(value.z, -2, 20) + 2) / 22.0;
	value.w = value.w / GetWaterToZoneHeight();
	return value;
}


float4 UnpackSimulation(float4 value)
{
	value.xy = (value.xy - 0.5) * 20.0;
	value.z = value.z * 22.0 - 2.0;
	value.w = value.w * GetWaterToZoneHeight();
	return value;
}




inline float4 GetDynamicWavesMaskColor(float2 uv)
{
	return KWS_DynamicWavesMaskColorRT.SampleLevel(sampler_linear_clamp, uv, 0);
}

inline float4 GetDynamicWavesZoneRaw(float2 uv)
{
	return KWS_DynamicWaves.SampleLevel(sampler_linear_clamp, uv, 0);
}

inline float4 GetDynamicWavesZone(float2 uv)
{
	float4 data = UnpackSimulation(KWS_DynamicWaves.SampleLevel(sampler_linear_clamp, uv, 0));
	data.xy = RotateDynamicWavesCoord(data.xy);
	return data;
}

inline float4 GetDynamicWavesZone(float2 uv, float2 offset)
{
	float4 data = UnpackSimulation(KWS_DynamicWaves.SampleLevel(sampler_linear_clamp, uv + offset * KWS_DynamicWaves_TexelSize.xy, 0));
	data.xy = RotateDynamicWavesCoord(data.xy);
	return data;
}


inline float4 GetDynamicWavesZoneBicubic(float2 uv)
{
	float4 data = UnpackSimulation(Texture2DSampleLevelBicubic(KWS_DynamicWaves, sampler_linear_clamp, uv, KWS_DynamicWaves_TexelSize, 0));
	data.xy = RotateDynamicWavesCoord(data.xy);
	return data;
}


inline float4 GetDynamicWavesZoneAdditionalData(float2 uv)
{
	return KWS_DynamicWavesAdditionalDataRT.SampleLevel(sampler_linear_clamp, uv, 0);
}


inline float4 GetDynamicWavesZoneAdditionalDataBicubic(float2 uv)
{
	return Texture2DSampleLevelBicubic(KWS_DynamicWavesAdditionalDataRT, sampler_linear_clamp, uv, KWS_DynamicWavesAdditionalDataRT_TexelSize, 0);
}

inline float4 GetDynamicWavesZoneColorData(float2 uv)
{
	return KWS_DynamicWavesColorDataRT.SampleLevel(sampler_linear_clamp, uv, 0);
}


inline float4 GetDynamicWavesZoneAdvectedUV(float2 uv)
{
	return KWS_DynamicWavesAdvectedUV.SampleLevel(sampler_linear_clamp, uv, 0);
}


inline float3 GetDynamicWavesZoneNormals(float2 uv)
{
	return KWS_DynamicWavesNormals.SampleLevel(sampler_linear_clamp, uv, 0).xyz;
}


inline float3 GetDynamicWavesZoneNormalsBicubic(float2 uv)
{
	return Texture2DSampleLevelBicubic(KWS_DynamicWavesNormals, sampler_linear_clamp, uv, KWS_DynamicWaves_TexelSize, 0).xyz;
}

inline float UnpackDepthMask(float maskDepth)
{
	if (maskDepth > 0)
	{
		float cameraHeight = KWS_OrthoDepthNearFarSize.x;
		float far = KWS_OrthoDepthNearFarSize.y;
		float near = 0.0001;

		float worldDepth = cameraHeight - far + near + maskDepth * far;
		return worldDepth;
	}
	else return -100000;
}



inline float GetDynamicWavesZoneDepthMaskBicubic(float2 uv)
{
	//return 0;
	float maskDepth = Texture2DSampleLevelBicubic(KWS_DynamicWavesDepthMask, sampler_linear_clamp, uv, KWS_DynamicWavesDepthMask_TexelSize, 0).x;
	return UnpackDepthMask(maskDepth);
}

inline float GetDynamicWavesZoneDepthMask(float2 uv)
{
	//return 0;
	float maskDepth = KWS_DynamicWavesDepthMask.SampleLevel(sampler_linear_clamp, uv, 0).x;
	return UnpackDepthMask(maskDepth);
}



inline float GetDynamicWavesBorderFading(float2 uv, float multiplier = 1.01, float powValue = 15)
{
	uv = abs(uv * 2 - 1);
	float uvEdgeMask = max(uv.x, uv.y);
	uvEdgeMask = 1 - saturate(pow(uvEdgeMask * multiplier, powValue));
	return uvEdgeMask;
}

inline float2 NormalizeDynamicWavesVelocity(float2 velocity)
{
	return clamp(velocity, -1, 1);
	
	float flowStrength = max(length(velocity), 0.001);
	return (velocity / flowStrength) * saturate(1 - exp(-flowStrength));
	
}

inline float2 KWS_ClampLength2(float2 v, float maxLen)
{
	float len = length(v);
	if (len > maxLen)
		v *= maxLen / max(len, 1e-5);
	return v;
}

float GetDynamicWavesHeightOffset(float3 worldPos, uint iterations = 3)
{
	float3 disp = GetFftWavesHeight(worldPos, iterations);

	float2 uv = GetDynamicWavesUV(worldPos);
	float4 dynamicWaves = GetDynamicWavesZone(uv);
	float zoneFade = GetDynamicWavesBorderFading(uv);

	float waterLevel = disp.y + dynamicWaves.z * zoneFade + dynamicWaves.w + KWS_WaterLevel;
	return waterLevel;
}

float GetDynamicWavesHeightOffset(float3 worldPos, float2 dynamicWavesUV, float4 dynamicWaves, uint iterations = 3)
{
	float3 disp = 0;
	if (KWS_WindSpeed > 6)
	{
		disp = GetFftWavesHeight(worldPos, iterations);
	}
	
	float zoneFade = GetDynamicWavesBorderFading(dynamicWavesUV);

	float waterLevel = disp.y + dynamicWaves.z * zoneFade + dynamicWaves.w + KWS_WaterLevel;
	return waterLevel;
}

float GetDynamicWavesWaterfallTreshold(float3 zoneNormal)
{
	float waterfallThreshold = saturate(2 * saturate(1 - dot(zoneNormal, float3(0, 1, 0))) - 0.05);
	return waterfallThreshold;
}

float GetDynamicWavesRiverMask(float3 worldPosRefracted, float shorelineMask)
{
	return saturate(worldPosRefracted.y - KWS_WaterLevel - 3) * (1 - shorelineMask);
}

inline void KWS_BlendDynamicWavesNormals(float riverMask, float2 worldPosXZ, float shorelineMask, float2 flowDirectionNormalized,
                                  float velocityLength, float zoneTimeScale, float flowSpeedMultiplier, inout float3 dynamicWavesNormal, inout float3 tangentNormal)
{
	float flowSpeed = lerp(0.25, 0.5, riverMask);

	float3 dynamicWavesFlowNormal = Texture2DSampleFlowmapJumpStochastic(KWS_WaterDynamicWavesFlowMapNormal, sampler_linear_repeat, worldPosXZ * 0.05,
	                                                                     flowDirectionNormalized * flowSpeed, KWS_Time * zoneTimeScale * flowSpeedMultiplier, 5).xzy * 2.0 - 1.0;

	dynamicWavesFlowNormal.z *= -1.0;
	dynamicWavesFlowNormal.xz *= RIVER_FLOW_NORMAL_MULTIPLIER;

	dynamicWavesFlowNormal = lerp(float3(0, 1, 0), dynamicWavesFlowNormal, riverMask * saturate(velocityLength - 0.15));
	dynamicWavesNormal = KWS_BlendNormals(dynamicWavesNormal, dynamicWavesFlowNormal);

	tangentNormal = lerp(KWS_BlendNormals(dynamicWavesNormal, tangentNormal), dynamicWavesNormal, saturate((1.0 - shorelineMask) - 0.2));
}



DECLARE_TEXTURE(ScreenSpaceFoamTex);
DECLARE_TEXTURE(ScreenSpaceFoamTexBlured);
float4 ScreenSpaceFoamTex_TexelSize;

StructuredBuffer<uint> ScreenSpaceFoamBuffer0;
StructuredBuffer<uint> ScreenSpaceFoamBuffer1;
StructuredBuffer<uint> ScreenSpaceFoamBuffer2;

float4 ScreenSpaceFoamBufferSizes[3];

uint GetScreenSpaceFoamBufferIndex(float2 uv, uint lodIdx)
{
	uint2 pixel = uint2(uv * (uint2)ScreenSpaceFoamBufferSizes[lodIdx].xy);
	pixel = min(pixel, uint2((uint)ScreenSpaceFoamBufferSizes[lodIdx].x - 1u, (uint)ScreenSpaceFoamBufferSizes[lodIdx].y - 1u));
	uint index = pixel.y * (uint)ScreenSpaceFoamBufferSizes[lodIdx].x + pixel.x;
	return index;
}

float SampleFoamLOD(float2 uv, int lod)
{
	if (lod == 0) return ScreenSpaceFoamBuffer0[GetScreenSpaceFoamBufferIndex(uv, 0)];
	if (lod == 1) return ScreenSpaceFoamBuffer1[GetScreenSpaceFoamBufferIndex(uv, 1)];
	return ScreenSpaceFoamBuffer2[GetScreenSpaceFoamBufferIndex(uv, 2)];
}

inline float4 GetScreenSpaceFoam(float2 uv, float2 offset = 0)
{
	return SAMPLE_TEXTURE_LOD(ScreenSpaceFoamTex, sampler_linear_clamp, uv + offset * ScreenSpaceFoamTex_TexelSize.xy, 0);
	//return Texture2DSampleBicubic(ScreenSpaceFoamTex, sampler_linear_clamp, uv, ScreenSpaceFoamTex_TexelSize);
}



inline float4 GetScreenSpaceFoamBlured(float2 uv)
{
	return SAMPLE_TEXTURE_LOD(ScreenSpaceFoamTexBlured, sampler_linear_clamp, uv, 0);
	//return Texture2DSampleBicubic(ScreenSpaceFoamTex, sampler_linear_clamp, uv, ScreenSpaceFoamTex_TexelSize);
	
	//float4 data = SAMPLE_TEXTURE_GATHER(ScreenSpaceFoamTex, sampler_linear_clamp, uv);
	//return dot(data, 0.25);
}



float3 ComputeRipple(float2 uv, float time, float weight)
{
	float4 ripple = KWS_RainMap.Sample(sampler_linear_repeat, uv);
	ripple.yz = ripple.yz * 2 - 1;

	float dropFrac = frac(ripple.w + time);
	float timeFrac = dropFrac - 1.0f + ripple.x;
	float dropFactor = saturate(0.2f + weight * 0.8f - dropFrac);
	float finalFactor = dropFactor * ripple.x * sin( clamp(timeFrac * 9.0f, 0.0f, 3.0f) * 3.1415);

	return float3(ripple.yz * finalFactor * 0.35f, 1.0f);
}


float3 GetRainMap(float2 uv, float time, float4 weights, float rainIntensity)
{
	float4 TimeMul = float4(0.75f, 0.85f, 0.93f, 1.15f); 
	float4 TimeAdd = float4(0.0f, 0.2f, 0.45f, 0.7f);
	float4 Times = (time * TimeMul + TimeAdd) * 1.6f;
	Times = frac(Times);
	float2 jitter1 = KWS_PointOnUnitCircle(time * 0.1);
	float2 jitter2 = KWS_PointOnUnitCircle(time * 0.05 + 0.35);
	
	float3 Ripple1 = ComputeRipple(uv + float2( 0.25f,0.0f) + jitter1 * 0.35, Times.x, weights.x);
	float3 Ripple2 = ComputeRipple(uv + float2(-0.55f,0.3f) - jitter1 * 0.35, Times.y, weights.y);
	float3 Ripple3 = ComputeRipple(uv + float2(0.6f, 0.85f) + jitter2 * 0.5, Times.z, weights.z);
	float3 Ripple4 = ComputeRipple(uv + float2(0.5f,-0.75f) - jitter2 * 0.5, Times.w, weights.w);

	
	float4 Weights = rainIntensity - float4(0, 0.25, 0.5, 0.75);
	Weights = saturate(Weights * 4);

	float4 Z = lerp(1, float4(Ripple1.z, Ripple2.z, Ripple3.z, Ripple4.z), Weights);
	float3 Normal = float3( Weights.x * Ripple1.xy +
							Weights.y * Ripple2.xy + 
							Weights.z * Ripple3.xy + 
							Weights.w * Ripple4.xy, 
							Z.x * Z.y * Z.z * Z.w);
	// return result                             
	return Normal.xzy;
}

//////////////////////////////////////////////////////////////////////////////////////




//////////////////////////////////////////////   Underwater    //////////////////////////////////////////////////////////
#define UNDERWATER_DEPTH_SCALE 0.75




/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




//////////////////////////////////////////////   Refraction    //////////////////////////////////////////////////////////

inline void FixRefractionSurfaceLeaking(float surfaceDepthZEye, float sceneZ, float sceneZEye, float2 screenUV, inout float refractedSceneZ, inout float refractedSceneZEye, inout float2 refractionUV)
{
	if (surfaceDepthZEye > refractedSceneZEye)
	{
		refractedSceneZ = sceneZ;
		refractedSceneZEye = sceneZEye;
		refractionUV = screenUV;
	}
}

half3 ComputeWaterRefractRay(half3 viewDir, half3 normal, half depth)
{
	half nv = dot(normal, viewDir);
	half v2 = dot(viewDir, viewDir);
	half knormal = (sqrt(((1.7689 - 1.0) * v2) / (nv * nv) + 1.0) - 1.0) * nv;
	half3 result = depth * (viewDir + (knormal * normal));
	return result;
}



inline float2 GetRefractedUV_IOR(float3 viewDir, half3 normal, float3 worldPos, float sceneZEye, float waterSurfaceZEye, float transparent, float depthValue)
{
	float zFix = saturate(sceneZEye - waterSurfaceZEye);
	float aproximatedDepth = min(transparent, depthValue * zFix);
	float3 refractedRay = ComputeWaterRefractRay(-viewDir, normal, aproximatedDepth);
	refractedRay.y *= 0.4; //fix information lost in the near camera
	
	float4 refractedClipPos = mul(UNITY_MATRIX_VP, float4(GetCameraRelativePosition(worldPos + refractedRay), 1.0));
	float4 refractionScreenPos = ComputeScreenPos(refractedClipPos);
	float2 uv = refractionScreenPos.xy / refractionScreenPos.w;

	/*float2 overflowUV = abs(uv * 2 - 1);
	float overflowFix = saturate(max(overflowUV.x, overflowUV.y) - 0.6);
	overflowFix *= overflowFix;
	overflowFix = lerp(0, overflowFix, zFix);
	uv = lerp(uv, uv * 0.5 + 0.25, overflowFix);*/
	
	if (uv.x >= 0.995) uv.x = 1.99 - uv.x;
	if (uv.y >= 0.995) uv.y = 1.99 - uv.y;
	if (uv.y <= 0) uv.y = -uv.y;
	if (uv.x <= 0) uv.x = -uv.x;
	
	return uv;
}

inline float2 GetRefractedUV_Simple(float2 uv, half3 normal)
{
	return uv + normal.xz * KWS_RefractionSimpleStrength * 0.5;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////





float4 LocalPosToToClipPosOrtho(float3 vertex)
{
	float3 worldPos = GetCameraRelativePositionOrtho(mul(UNITY_MATRIX_M, float4(vertex.xyz, 1.0)).xyz);
	return mul(KWS_MATRIX_VP_ORTHO, float4(GetCameraAbsolutePosition(worldPos), 1));
}

float4 WorldPosToToClipPosOrtho(float3 worldPos)
{
	worldPos = GetCameraRelativePositionOrtho(worldPos);
	return mul(KWS_MATRIX_VP_ORTHO, float4(worldPos, 1));
}


inline half GetSurfaceToSceneFading(float zEye, float surfaceZEye, float multiplier)
{
	return saturate((zEye - surfaceZEye) * multiplier);
}

inline half GetWaterRawFade(float3 worldPos, float surfaceDepthZEye, float refractedSceneZEye, half surfaceMask, half depthAngleFix)
{
	return (refractedSceneZEye - surfaceDepthZEye) * lerp(depthAngleFix, 1, unity_OrthoParams.w);
}




//////////////////////////////////////////////  Scene Color Pass    //////////////////////////////////////////////////////////

float4 KWS_CameraOpaqueTextureAfterWaterPass_TexelSize;
float4 KWS_CameraOpaqueTextureAfterWaterPass_RTHandleScale;

DECLARE_TEXTURE(KWS_CameraOpaqueTextureAfterWaterPass);


inline float2 GetSceneColorAfterWaterPassNormalizedUV(float2 uv)
{
	float2 maxCoord = 1.0f - KWS_CameraOpaqueTextureAfterWaterPass_TexelSize.xy * 0.5;
	return min(uv, maxCoord) * KWS_CameraOpaqueTextureAfterWaterPass_RTHandleScale.xy;
}

inline half3 GetSceneColorAfterWaterPass(float2 uv)
{
	#ifdef KWS_HDRP
		return GetSceneColor(uv);
	#else
		return SAMPLE_TEXTURE_LOD(KWS_CameraOpaqueTextureAfterWaterPass, sampler_linear_clamp, GetSceneColorAfterWaterPassNormalizedUV(uv), 0).xyz;
	#endif
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

Texture2DArray KWS_CurlNoiseArray;

float3 SampleCurlNoiseArray(float2 worldXZ, float time, float spatialScale, float animSpeed, float sliceCount)
{
	float frame = frac(time * animSpeed) * sliceCount;

	float slice0 = floor(frame);
	float slice1 = slice0 + 1.0;
	if (slice1 >= sliceCount) slice1 = 0.0;

	float blend = frac(frame);

	float2 uv = worldXZ * spatialScale;

	float3 curl0 = KWS_CurlNoiseArray.SampleLevel(sampler_linear_repeat, float3(uv, slice0), 0).rgb * 2.0 - 1.0;
	float3 curl1 = KWS_CurlNoiseArray.SampleLevel(sampler_linear_repeat, float3(uv, slice1), 0).rgb * 2.0 - 1.0;

	return lerp(curl0, curl1, blend);
}







#endif