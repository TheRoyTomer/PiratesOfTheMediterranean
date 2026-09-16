#ifndef KWS_WATER_VARIABLES
#define KWS_WATER_VARIABLES


#include "KWS_WaterDefines.cginc"
#if !defined(KWS_BUILTIN) && !defined(KWS_URP) && !defined(KWS_HDRP)
	#define KWS_URP
#endif

#if defined(STEREO_INSTANCING_ON)
	#define DECLARE_TEXTURE(tex) Texture2DArray tex
	#define DECLARE_TEXTURE_UINT(tex) Texture2DArray<uint> tex
	#define SAMPLE_TEXTURE(tex, samplertex, uv) tex.Sample(samplertex, float3(uv, (float)unity_StereoEyeIndex))
	#define SAMPLE_TEXTURE_LOD(tex, samplertex, uv, lod) tex.SampleLevel(samplertex, float3(uv, (float)unity_StereoEyeIndex), lod)
	#define SAMPLE_TEXTURE_LOD_OFFSET(tex, samplertex, uv, lod, offset) tex.SampleLevel(samplertex, float3(uv, (float)unity_StereoEyeIndex), lod, offset)
	#define SAMPLE_TEXTURE_GATHER(tex, samplertex, uv) tex.Gather(samplertex, float3(uv, (float)unity_StereoEyeIndex))
	#define SAMPLE_TEXTURE_GATHER_GREEN(tex, samplertex, uv) tex.GatherGreen(samplertex, float3(uv, (float)unity_StereoEyeIndex))
	#define SAMPLE_TEXTURE_GATHER_BLUE(tex, samplertex, uv) tex.GatherBlue(samplertex, float3(uv, (float)unity_StereoEyeIndex))
	#define SAMPLE_TEXTURE_GATHER_ALPHA(tex, samplertex, uv) tex.GatherAlpha(samplertex, float3(uv, (float)unity_StereoEyeIndex))
	#define LOAD_TEXTURE(tex, uv) tex.Load(uint4(uv, unity_StereoEyeIndex, 0))
	#define LOAD_TEXTURE_OFFSET(tex, uv, offset) tex.Load(uint4(uv, unity_StereoEyeIndex, 0), offset)
	#define SAMPLE_TEXTURE_GRAD(tex, samplertex, uv, dx, dy) tex.SampleGrad(samplertex, float3(uv, (float)unity_StereoEyeIndex), dx, dy)
#else
	#define DECLARE_TEXTURE(tex) Texture2D tex
	#define DECLARE_TEXTURE_UINT(tex) Texture2D<uint> tex
	#define SAMPLE_TEXTURE(tex, samplertex, uv) tex.Sample(samplertex, uv)
	#define SAMPLE_TEXTURE_LOD(tex, samplertex, uv, lod) tex.SampleLevel(samplertex, uv, lod)
	#define SAMPLE_TEXTURE_LOD_OFFSET(tex, samplertex, uv, lod, offset) tex.SampleLevel(samplertex, uv, lod, offset)
	#define SAMPLE_TEXTURE_GATHER(tex, samplertex, uv) tex.Gather(samplertex, uv)
	#define SAMPLE_TEXTURE_GATHER_GREEN(tex, samplertex, uv) tex.GatherGreen(samplertex, uv)
	#define SAMPLE_TEXTURE_GATHER_BLUE(tex, samplertex, uv) tex.GatherBlue(samplertex, uv)
	#define SAMPLE_TEXTURE_GATHER_ALPHA(tex, samplertex, uv) tex.GatherAlpha(samplertex, uv)
	#define LOAD_TEXTURE(tex, uv) tex.Load(uint3(uv, 0))
	#define LOAD_TEXTURE_OFFSET(tex, uv, offset) tex.Load(uint3(uv, 0), offset)
	#define SAMPLE_TEXTURE_GRAD(tex, samplertex, uv, dx, dy) tex.SampleGrad(samplertex, uv, dx, dy)
#endif

float4x4 KWS_MATRIX_VP_STEREO[2];
float4x4 KWS_PREV_MATRIX_VP_STEREO[2];
float4x4 KWS_MATRIX_I_VP_STEREO[2];
float4x4 KWS_MATRIX_CAMERA_PROJECTION_STEREO[2];
float4x4 KWS_MATRIX_I_P_STEREO[2];

float4x4 KWS_MATRIX_VP;
float4x4 KWS_PREV_MATRIX_VP;
float4x4 KWS_MATRIX_I_VP;
float4x4 KWS_MATRIX_CAMERA_PROJECTION;
float4x4 KWS_MATRIX_I_P;

float4x4 KW_ViewToWorld;
float4x4 KW_ProjToView;

float4x4 KWS_MATRIX_VP_ORTHO;
float3 KWS_WorldSpaceCameraPosOrtho;

#if defined(STEREO_INSTANCING_ON)
	#define KWS_MATRIX_VP KWS_MATRIX_VP_STEREO[unity_StereoEyeIndex]
	#define KWS_PREV_MATRIX_VP KWS_PREV_MATRIX_VP_STEREO[unity_StereoEyeIndex]
	#define KWS_MATRIX_I_VP KWS_MATRIX_I_VP_STEREO[unity_StereoEyeIndex]
	#define KWS_MATRIX_CAMERA_PROJECTION KWS_MATRIX_CAMERA_PROJECTION_STEREO[unity_StereoEyeIndex]
	#define KWS_MATRIX_I_P KWS_MATRIX_I_P_STEREO[unity_StereoEyeIndex]
#else
	#define KWS_MATRIX_VP KWS_MATRIX_VP
	#define KWS_PREV_MATRIX_VP KWS_PREV_MATRIX_VP
	#define KWS_MATRIX_I_VP KWS_MATRIX_I_VP
	#define KWS_MATRIX_CAMERA_PROJECTION KWS_MATRIX_CAMERA_PROJECTION
	#define KWS_MATRIX_I_P KWS_MATRIX_I_P
#endif

#if defined(UNITY_COMPILER_HLSL)
	#define KWS_FORCECASE   [forcecase]
#else
	#define KWS_FORCECASE
#endif


#define NAN_VALUE asfloat(0x7fc00000)
#define INF_VALUE asfloat(0x7F800000)
#define UINT_MAX_VALUE 4294967295.0
#define PS5_BOOL_FIX 1.000001

#define MAX_WATER_INSTANCES 32
#define MIN_THRESHOLD 0.00001
#define KWS_SHORELINE_OFFSET_MULTIPLIER 34.0
#define KWS_WATER_MASK_DECODING_VALUE 1.0 / 255.0
#define KWS_WATER_MASK_ENCODING_VALUE 255.0
#define KWS_WATER_SSR_NORMAL_LOD 2.5

#define KWS_MAX_TRANSPARENT 50.0
#define KWS_MAX_TRANSPARENT_INVERSE 1.0 / KWS_MAX_TRANSPARENT
#define KWS_MAX_WIND_SPEED 50.0
#define FOAM_MIN_WIND 4
#define KWS_DYNAMIC_WAVES_MAX_XZ_OFFSET 0.075
#define KWS_MIN_FAR_DISTANCE 250

#define KWS_MESH_TYPE_INFINITE_OCEAN 0
#define KWS_MESH_TYPE_FINITE_BOX 1
#define KWS_MESH_TYPE_RIVER 2
#define KWS_MESH_TYPE_CUSTOM_MESH 3

#define KWS_CAUSTIC_MULTIPLIER 0.2
#define KWS_VOLUME_LIGHT_SLICE_DENSITY 10
#define KWS_VOLUME_LIGHT_ABSORBTION_FACTOR 0.75

#define RIVER_FLOW_FRESNEL_MULTIPLIER 0.3
#define RIVER_FLOW_NORMAL_MULTIPLIER 1.0

#ifdef KWS_HDRP
	#define KWS_VOLUME_LIGHT_TRANSMITANCE_NEAR_OFFSET_FACTOR 0.5
#else
	#define KWS_VOLUME_LIGHT_TRANSMITANCE_NEAR_OFFSET_FACTOR 0.5
#endif


#define KWS_WATER_IOR 1.33
#define KWS_ADVECTED_UV_REPEAT_TIME 3.0

SamplerState sampler_point_clamp;
SamplerState sampler_point_repeat;
SamplerState sampler_linear_repeat;
SamplerState sampler_linear_clamp;
SamplerState sampler_trilinear_clamp;
SamplerState sampler_trilinear_repeat;

float4 Test4;
Texture2D KWS_BlueNoise3D;


float3 KW_DirLightForward;
float3 KW_DirLightColor;

float srpBatcherFix;
float3 KWS_CameraForward;
float3 KWS_CameraRight;
float3 KWS_CameraUp;
float3 KWS_WorldSpaceCameraPos;

Texture2D KW_FluidsFoamTex;
SamplerState sampler_KW_FluidsFoamTex;

Texture2D KWS_OceanFoamTex;
Texture2D KWS_IntersectionFoamTex;
Texture2D KWS_TestTexture;
Texture2D KWS_DebugTexture;
Texture2D KWS_SplashTex0;
Texture2D KWS_WaterDropsTexture;
Texture2D KWS_WaterDropsMaskTexture;
Texture2D KWS_WaterDynamicWavesFlowMapNormal;
Texture2D KWS_PerlinNoise;
Texture2D KWS_CausticFrameLod0;
Texture2D KWS_CausticFrameLod1;


float4 KW_FluidsFoamTex_TexelSize;
float KWS_FoamTextureType;
float KWS_FoamTextureContrast;
float KWS_FoamTextureScaleMultiplier;
float KWS_OceanRainDropsIntensity;



///////////////////////////////////// Dynamic data ////////////////////////////////////////////////////////

float KWS_Time;
//float KWS_ScaledTime;
float KWS_GlobalTimeScale;
float KWS_WaterLevel;

int KWS_IsCameraPartialUnderwater;
int KWS_IsCameraFullyUnderwater;
int KWS_FogState;

float3 KWS_AmbientColor;
float3 KWS_DirLightDireciton;
float3 KWS_DirLightColor;

float KWS_OceanLevel;
float KWS_OceanLevelHeightOffset;
float3 KWS_WaterWorldPosOffset;

///////////////////////////////////////////////////////////////////////////////////////////////////



///////////////////////////////////// Constant data ////////////////////////////////////////////////////////
uint KWS_WaterLayerMask;
uint KWS_WaterLightLayerMask;

float  KWS_RenderOcean;
float KWS_Transparent;
float3 KWS_TurbidityColor;
float3 KWS_WaterColor;
float KWS_UnderwaterTransparentOffset;
float KWS_CausticStrength;

float KW_GlobalTimeScale;
float KWS_GlobalWaterScaleFactor;

float KWS_SunMaxValue;
float KW_WaterFarDistance;
float KW_FFT_Size_Normalized;

float KWS_SunCloudiness;
float KWS_SunStrength;
float KWS_RefractionSimpleStrength;
float KWS_RefractionDispersionStrength;
float _TesselationFactor;
float _TesselationMaxDistance;
float _TesselationMaxDisplace;
float KWS_ReflectionClipOffset;
float3 KWS_CustomSkyColor;
float KWS_AnisoReflectionsScale;
float KWS_AnisoWindCurvePower;
float KWS_SkyLodRelativeToWind;
float KW_FlowMapSize;
float KW_FlowMapSpeed;
float KW_FlowMapFluidsStrength;
float4 KW_FlowMapOffset;
float KWS_UnderwaterHalfLineTensionScale;

float KWS_OceanTimeScale;
float KWS_OceanFoamStrength;
float KWS_OceanFoamDisappearSpeedMultiplier;
float4 KWS_OceanFoamColor;
float2 KWS_IntersectionFoamFadeSize;
float4 KWS_IntersectionFoamColor;



float KWS_WetStrength;
float KWS_WetLevel;

uint KWS_OverrideSkyColor;
//uint KWS_IsCustomMesh;

uint KWS_UseOceanFoam;
uint KWS_UseWireframeMode;
uint KWS_UseMultipleSimulations;

int KWS_MeshType;
uint KWS_UseFilteredNormals;
uint KWS_UseCausticDispersion;
uint KWS_DisableCausticInShadow;
uint KWS_IsAnyZoneUseFoamParticles;

float KWS_AbsorbtionOverrideMultiplier;

uint KWS_IsEditorCamera;
///////////////////////////////////////////////////////////////////////////////////////////////////



/////////////////////////////////////  Instanced data ///////////////////////////////////////////////////
float KWS_WaterYRotationRad;
float3 KWS_InstancingWaterScale;
//uint KWS_IsFiniteTypeInstancing;

#if defined(KWS_USE_WATER_INSTANCING)

	struct InstancedMeshDataStruct
	{
		float3 position;
		float3 size;

		uint downSeam;
		uint leftSeam;
		uint topSeam;
		uint rightSeam;

		uint downInf;
		uint leftInf;
		uint topInf;
		uint rightInf;
	};
	StructuredBuffer<InstancedMeshDataStruct> InstancedMeshData;

#endif


///////////////////////////////////////////////////////////////////////////////////////////////////////////////


/////////////////////////////////////  Shoreline variables ///////////////////////////////////////////////////


//////////////////////////////////////////////////////////////////////////////////////////////////////////////



//////////////////////////////////////// Ortho depth variables //////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////




#endif