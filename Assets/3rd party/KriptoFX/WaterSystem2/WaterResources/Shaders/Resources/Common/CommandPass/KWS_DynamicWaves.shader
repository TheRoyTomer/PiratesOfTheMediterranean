Shader "Hidden/KriptoFX/KWS/DynamicWaves"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "" { }
	}

	HLSLINCLUDE

	#define KWS_USE_DYNAMIC_WAVES
	
	#define MAX_SOLVER_CFL_DEPTH 2.0
	#define MINIMUM_WATER_HEIGHT  0.001
	#define GRID_CELL_SIZE 1
	#define TIME_STEP 0.1
	#define GRAVITY 10.0
	
	#define ADVECT_SPEED_RIVER 2.5
	#define ADVECT_SPEED_OCEAN 1.25
	
	#define ADVECT_NOISE_SCALE_RIVER 1.0
	#define ADVECT_NOISE_SCALE_OCEAN 1.0
	
	#define ADVECT_BLEND_FACTOR_RIVER 0.9
	#define ADVECT_BLEND_FACTOR_OCEAN 0.95
	
	#define VORTICITY_RIVER 1.0
	#define VORTICITY_MOVED_ZONE 1.5
	
	#define VELOCITY_SOURCE 0.025
	
	#define OCEAN_RELAX_RATE      0.008   // 0..1, how much excess free-surface height is removed per frame

	
	#define MANNING_N           0.03
	#define MIN_FRICTION_DEPTH  0.025
	#define LINEAR_DRAG         0.002

	#define OCEAN_TRANSPORT_DEPTH 3.0
	#define OVERSHOOT_BETA        2.0
	

	#define DYNAMIC_WAVES_MASK_OBSTACLE 1
	
	
	
	#include "../../Common/KWS_WaterHelpers.cginc"
	#include "../KWS_DynamicWavesHelpers.cginc"

	
	struct vertexInputMaskMesh
	{
		float4 vertex : POSITION;
		float3 uv : TEXCOORD0;
	};

	struct vertexInputMap
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2fMaskMesh
	{
		float4 pos : SV_POSITION;
		float3 worldPos : TEXCOORD0;
	};

	struct v2fMap
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float3 worldPos : TEXCOORD1;
	};


	struct v2fMaskProcedural
	{
		float4 pos : SV_POSITION;
		uint instanceID : TEXCOORD1;
		float3 worldPos : TEXCOORD2;
	};

	struct MaskFragmentOutput
	{
		float4 data : SV_Target0;
		#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
			float4 color : SV_Target1;
		#endif
	};
	
	struct MapFragmentOutput
	{
		float4 data : SV_Target0;
		float4 additionalData : SV_Target1;
		float4 normalAndWetMap : SV_Target2;
		#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_ADVECTED_UV
			float4 advectedUVMap : SV_Target3;

			#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
				float4 color : SV_Target4;
			#endif
		#else

			#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
				float4 color : SV_Target3;
			#endif
			
		#endif
		
	
	};


	v2fMaskMesh vertMaskMesh(vertexInputMaskMesh v)
	{
		v2fMaskMesh o;
		o.pos = LocalPosToToClipPosOrtho(v.vertex.xyz);
		o.worldPos = LocalToWorldPos(v.vertex.xyz);
		return o;
	}

	v2fMaskProcedural vertMaskProcedural(uint instanceID : SV_InstanceID, float4 vertex : POSITION)
	{
		v2fMaskProcedural o;
		KWS_DynamicWavesMask data = KWS_DynamicWavesMaskBuffer[instanceID];
		float3 worldPos = mul(data.matrixTRS, float4(vertex.xyz, 1)).xyz;

		
		o.pos = WorldPosToToClipPosOrtho(worldPos);
		o.worldPos = worldPos;
		o.instanceID = instanceID;
		

		return o;
	}
	
	v2fMap vertMap(vertexInputMap v)
	{
		v2fMap o;
		o.pos = LocalPosToToClipPosOrtho(v.vertex.xyz);
		o.uv = v.uv;
		o.worldPos = LocalToWorldPos(v.vertex.xyz);
		return o;
	}

	v2fMap vertBakePostprocessing(uint vertexID : SV_VertexID)
	{
		v2fMap o;
		o.pos = GetTriangleVertexPosition(vertexID);
		o.uv = GetTriangleUVScaled(vertexID);
		o.worldPos = 0;
		return o;
	}
	

	MaskFragmentOutput fragDrawMesh(v2fMaskMesh i) 
	{
		MaskFragmentOutput o = (MaskFragmentOutput)0;
		
		float force = clamp(KWS_DynamicWavesForce, -1, 1) * 0.5 + 0.5;
		o.data = float4(force,  0.5,  0.5, KWS_ObstacleIntersectionAlphaFade);
	
		return o;
		
	}


	MaskFragmentOutput fragDrawProcedural(v2fMaskProcedural i) 
	{
		MaskFragmentOutput o = (MaskFragmentOutput)0;

		KWS_DynamicWavesMask data = KWS_DynamicWavesMaskBuffer[i.instanceID];
		float intersectionAlpha = 1;
		
		if (data.useWaterIntersection)
		{
			float2 zoneUV = GetDynamicWavesUV(i.worldPos);
			float4 dynamicWaves = GetDynamicWavesZone(zoneUV);
			float currentWaterLevel = GetDynamicWavesHeightOffset(i.worldPos, zoneUV, dynamicWaves);
				
			
			float3 ax = float3(data.matrixTRS._m00, data.matrixTRS._m10, data.matrixTRS._m20);
			float3 ay = float3(data.matrixTRS._m01, data.matrixTRS._m11, data.matrixTRS._m21);
			float3 az = float3(data.matrixTRS._m02, data.matrixTRS._m12, data.matrixTRS._m22);

			float halfSizeY = 0.5 * (abs(ax.y) + abs(ay.y) + abs(az.y));
			halfSizeY = max(halfSizeY, 1e-5);
			
			float intersectionMask = abs(currentWaterLevel - data.position.y) / halfSizeY;
			float intersectionMask2 = abs(currentWaterLevel - i.worldPos.y);
			intersectionAlpha *= 1 - saturate(intersectionMask * intersectionMask2);
		}
		

		data.force = clamp(data.force * 10, -1, 1) * 0.5 + 0.5;
		data.forceDirection.xz = clamp(data.forceDirection.xz, -1, 1) * 0.5 + 0.5;
		
		o.data = float4(data.force * saturate(intersectionAlpha * 5), data.forceDirection.xz, saturate(intersectionAlpha * 2));
		
		#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
			if(data.useColor == 1) o.color = data.color;
		#endif

		return o;
	}

	MapFragmentOutput fragMap(v2fMap i) 
	{
		MapFragmentOutput o = (MapFragmentOutput)0;

		float distanceToCamera = GetWorldToCameraDistance(i.worldPos);
		float pixelOffsetRelativeToDistance = saturate((distanceToCamera - 50) * 0.0005) * 20;

	
		float fade = GetDynamicWavesBorderFading(i.uv, 1.01, 10);
		
		o.data = GetDynamicWavesZoneBicubic(i.uv);
		
		/*
		float4 data1 = GetDynamicWavesZone(i.uv, float2(-1, 0) * pixelOffsetRelativeToDistance);
		float4 data2 = GetDynamicWavesZone(i.uv, float2(1, 0) * pixelOffsetRelativeToDistance);
		float4 data3 = GetDynamicWavesZone(i.uv, float2(0, -1) * pixelOffsetRelativeToDistance);
		float4 data4 = GetDynamicWavesZone(i.uv, float2(0, 1) * pixelOffsetRelativeToDistance);
		
		o.data.z = dot(float4(data1.z, data2.z, data3.z, data4.z), 0.25);
		o.data.w = KWS_MAX(float4(data1.w, data2.w, data3.w, data4.w));*/

		//float4 additionalData = GetDynamicWavesZoneAdditionalData(i.uv);
		//o.additionalData = float4(Pack_UintTo_R8_UNorm(KWS_DynamicWavesZoneID), additionalData.y, additionalData.z, fade);
		//float2 texel = KWS_CurrentTarget_TexelSize.xy * Test4.x;
		//float4 additionalData = GetDynamicWavesZoneAdditionalData(i.uv);
		float4 additionalData = GetDynamicWavesZoneAdditionalData(i.uv);
/*
		float foamC = additionalData.z;
		float foamL = GetDynamicWavesZoneAdditionalData(i.uv - float2(texel.x, 0)).z;
		float foamR = GetDynamicWavesZoneAdditionalData(i.uv + float2(texel.x, 0)).z;
		float foamD = GetDynamicWavesZoneAdditionalData(i.uv - float2(0, texel.y)).z;
		float foamT = GetDynamicWavesZoneAdditionalData(i.uv + float2(0, texel.y)).z;

		float foamBlur = (foamC * 4.0 + foamL + foamR + foamD + foamT) * 0.125;
		additionalData.z = lerp(foamC, foamBlur, Test4.y);
		*/
		o.additionalData = float4(Pack_UintTo_R8_UNorm(KWS_DynamicWavesZoneID), additionalData.y, additionalData.z, fade);
	
		float3 normal = GetDynamicWavesZoneNormals(i.uv);
		normal.xz = RotateDynamicWavesCoord(normal.xz);
		
		o.normalAndWetMap.xy = normal.xz * 0.5 + 0.5;
		
		o.data.xyz *= fade;
		//o.additionalData.y *= fade;
		o.normalAndWetMap.xy  = lerp(float2(0.5, 0.5), o.normalAndWetMap.xy, fade);
		o.normalAndWetMap.zw = float2(additionalData.x, additionalData.w);

		#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_ADVECTED_UV
			o.advectedUVMap = GetDynamicWavesZoneAdvectedUV(i.uv);
		#endif

		#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
			o.color = GetDynamicWavesZoneColorData(i.uv);
		#endif
		
		return o;
	}

	float4 fragPostprocessing(v2fMap i) : SV_Target
	{
		//float4 data = UnpackSimulation(_SourceRT.SampleLevel(sampler_linear_clamp, i.uv, 0));

		float4 data = GaussianBlur5x5(_SourceRT, sampler_linear_clamp, i.uv, _SourceRT_TexelSize.xy);
		data = UnpackSimulation(data);
		
		float borderFade = GetDynamicWavesBorderFading(i.uv);
		data.xyz *= borderFade;
		
		return PackSimulation(data);
	}

	float4 fragIntersections(v2fMap i) : SV_Target
	{
		float fade = GetDynamicWavesBorderFading(i.uv);
		float waterLevel = GetDynamicWavesZone(i.uv).z;
		
		return waterLevel * saturate(fade * 5);
	}

	
	struct vertexInput
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		uint vertexID : SV_VertexID;
	};
	

	struct v2f
	{
		float4 vertex : SV_POSITION;
		float3 worldPos : TEXCOORD0;
		float2 currentUV : TEXCOORD1;
		float2 shiftedUV : TEXCOORD2;
	};
	
	v2f vert(vertexInput v)
	{
		v2f o;
		
		o.vertex = GetTriangleVertexPosition(v.vertexID);
		o.currentUV = GetTriangleUVScaled(v.vertexID);
		float2 worldUV = o.currentUV * KWS_DynamicWavesZoneSize.xz - KWS_DynamicWavesZoneSize.xz * 0.5;
		worldUV = RotateDynamicWavesCoord(worldUV);
		o.worldPos = float3(worldUV.x, 0, worldUV.y) + KWS_DynamicWavesZonePosition.xyz;
		
		o.shiftedUV = o.currentUV + KW_AreaOffset.xz;
		
		return o;
	}

	struct SimulationFragmentOutput
	{
		float4 data : SV_Target0;
		float4 additionalData : SV_Target1;

		#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
			float4 colorData : SV_Target2;
		#endif
	};

	SimulationFragmentOutput frag1(v2f i)
	{
		SimulationFragmentOutput o = (SimulationFragmentOutput)0;
		float2 border = KWS_CurrentTarget_TexelSize.xy * 3;

		UNITY_BRANCH
		if (KWS_CurrentFrame < 3)
		{
			o.data = PackSimulation(float4(0, 0, 0, 0));
			o.additionalData = float4(0, 1, 0, 0);
			#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
				o.colorData = float4(0, 0, 0, 0);
			#endif
			return o;
		}
		
		float4 center = UnpackSimulation(KWS_CurrentTarget.SampleLevel(sampler_linear_clamp, i.shiftedUV, 0));
		float4 right = UnpackSimulation(KWS_CurrentTarget.SampleLevel(sampler_linear_clamp, i.shiftedUV + float2(KWS_CurrentTarget_TexelSize.x, 0), 0));
		float4 left = UnpackSimulation(KWS_CurrentTarget.SampleLevel(sampler_linear_clamp, i.shiftedUV - float2(KWS_CurrentTarget_TexelSize.x, 0), 0));
		float4 top = UnpackSimulation(KWS_CurrentTarget.SampleLevel(sampler_linear_clamp, i.shiftedUV + float2(0, KWS_CurrentTarget_TexelSize.y), 0));
		float4 down = UnpackSimulation(KWS_CurrentTarget.SampleLevel(sampler_linear_clamp, i.shiftedUV - float2(0, KWS_CurrentTarget_TexelSize.y), 0));

		float borderFade = GetDynamicWavesBorderFading(i.shiftedUV);

		float4 dynamicWaveMask = GetDynamicWavesMask(i.shiftedUV);
		dynamicWaveMask.yz = RotateDynamicWavesCoordInverse(dynamicWaveMask.yz);
		
		
		#ifdef KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE
			float rawOrthoDepth = -1000;
		#else
			float rawOrthoDepth = GetWaterOrthoDepth(i.shiftedUV);
		#endif
		float dynamicWaveMaskDepth = GetDynamicWavesZoneDepthMaskBicubic(i.shiftedUV);
		dynamicWaveMaskDepth = lerp(rawOrthoDepth, dynamicWaveMaskDepth, dynamicWaveMask.a);
		float orthoDepth = max(rawOrthoDepth, dynamicWaveMaskDepth);
		float wetMapDepth = EncodeDynamicWavesHeight(orthoDepth);
		
		orthoDepth -= KWS_WaterLevel;
		
		
		float sdfDepth = 1000;
		float2 sdfDirection = float2(1, 0);
		#if !defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
			SimulationZoneSDF zoneSDF = GetSimulationZoneSDF(i.currentUV);
			sdfDepth = zoneSDF.sdf;
			sdfDirection = zoneSDF.direction;
			
		#endif
		float2 shoreDir = normalize(-sdfDirection + 1e-5);
		
	
		float orthoDepthWaterMask = (orthoDepth) < 1;
		float shorelineMaskFade = saturate(smoothstep(1, 25, sdfDepth)) * orthoDepthWaterMask;
		float shorelineMask = saturate(smoothstep(0.1, 5, sdfDepth)) * orthoDepthWaterMask;
		float riverMask = GetDynamicWavesRiverMask(orthoDepth + KWS_WaterLevel, shorelineMask);
		
		
		
		
		float4 additionalData = KWS_CurrentAdditionalTarget.SampleLevel(sampler_linear_clamp, i.shiftedUV, 0);
		//float4 lastCenter = KWS_PreviousAdditionalTarget.SampleLevel(sampler_linear_clamp, i.uv, 0);
		
		float heightLeft = (left.x >= 0.0) ?                 left.z : center.z;
		float heightRight = (center.x <= 0.0) ?                 right.z : center.z;
		float heightDown = (down.y >= 0.0) ?                 down.z : center.z;
		float heightTop = (center.y <= 0.0) ?                 top.z : center.z;

		float localDt = ComputeLocalDt(center.xy, center.z);
	
		#ifdef KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE
			float MAX_DEPTH_LIMIT = 3.0;
			float maxAverageHeight = MAX_DEPTH_LIMIT * GRID_CELL_SIZE / (GRAVITY * localDt);
			float heightAdjustment = max(-3, (left.z + right.z + down.z + top.z) / 4.0 - maxAverageHeight);
		#else
			float MAX_DEPTH_LIMIT = 2.0;
			float maxAverageHeight = MAX_DEPTH_LIMIT * GRID_CELL_SIZE / (GRAVITY * localDt);
			float heightAdjustment = max(lerp(-2, 0, saturate(orthoDepth)), (left.z + right.z + down.z + top.z) / 4.0 - maxAverageHeight);
		#endif

		heightLeft -= heightAdjustment;
		heightRight -= heightAdjustment;
		heightDown -= heightAdjustment;
		heightTop -= heightAdjustment;

		float heightChange = - ((heightRight * center.x - heightLeft * left.x) / GRID_CELL_SIZE
		+ (heightTop * center.y - heightDown * down.y) / GRID_CELL_SIZE);

		center.z += heightChange * localDt;

				
		float2 noise = 0;
		Advection(center, orthoDepth, i.shiftedUV, localDt, noise);
		
		//manning formula
		float MANNING = lerp(0.02, 0.075, KWS_RainDropsIntensity);    
		const float H_EPS     = 0.01; 

		float h = max(max(center.z, 0), H_EPS);
		float U = max(length(center.xy), 1e-6);

		// du/dt = - g n^2 |u| u / h^(4/3)
		float drag = GRAVITY * MANNING * MANNING * U / pow(h, 4.0/3.0);
		center.xy *= 1.0 / (1.0 + drag * localDt);
		center.xy *= 0.9985;
	
		float evoporateEachFrame = lerp(100, 1, KWS_EvaporationRate);
		float evaporationRate = (KWS_CurrentFrame % evoporateEachFrame) == 0 ?          MINIMUM_WATER_HEIGHT * 0.25 : 0;
		center.z = max(center.z - evaporationRate, 0);

		// Dynamic wave adjustments
		
		
		#if defined(KWS_USE_OCEAN_RENDERING)
			AddFFTWaves(center, i.worldPos, shorelineMask, shoreDir);
		#endif
		
		if (KWS_RainDropsIntensity > 0.001) AddRain(i.shiftedUV, riverMask, center, additionalData);
		

		float wetMap = 0;
		
		#if !defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
			 UpdateWetmap(additionalData, wetMap, center);
		#endif

		float foamMask = GetFoamMask(i.shiftedUV, additionalData, center, left, right, top, down, localDt, noise, sdfDepth, shoreDir, shorelineMaskFade, borderFade, riverMask, orthoDepth);
		
#if !defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
		OceanRelax(center, shorelineMask, localDt);
		if (orthoDepth < 0.001) { wetMap = 1; }
#endif


		UpdateDynamicObstacle(dynamicWaveMaskDepth, center, dynamicWaveMask, rawOrthoDepth, foamMask, wetMap);
		
		float2 externalVelocity = dynamicWaveMask.yz * 3;
		float externalSource = dynamicWaveMask.x * 0.1;
		float externalForceSpeed =  length(externalVelocity);
				
		center.xy -= externalVelocity;
		center.z += lerp(0, saturate(externalForceSpeed) * VELOCITY_SOURCE, orthoDepth < 0.5);
		
		
		if (center.z < 10) center.z += externalSource;
		center.w = orthoDepth;
		



#if !defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
		center.xy *= lerp(0.2f, 1, saturate(center.z * 10 + wetMap * 2));
#endif


		
		center.xy *= saturate(borderFade * 10);
		foamMask *= saturate(borderFade * 4);
		#if defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
			center.z *= saturate(borderFade * 4);
			foamMask *= saturate(borderFade);
		#endif

		o.data = PackSimulation(center);
		o.additionalData = float4(wetMap, shorelineMaskFade, foamMask, wetMapDepth);

		
		#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
			o.colorData = UpdateColor(i.shiftedUV, center, localDt, shorelineMaskFade, borderFade);
		#endif


		return o;
	}


	struct SimulationFragment2Output
	{
		float4 data : SV_Target0;
		float3 normal : SV_Target1;
		#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_ADVECTED_UV
			float4 uvAdvected : SV_Target2;
		#endif
	};
	
	SimulationFragment2Output frag2(v2f i)
	{
		SimulationFragment2Output o = (SimulationFragment2Output)0;
		
		float4 center = UnpackSimulation(KWS_CurrentTarget.SampleLevel(sampler_linear_clamp, i.shiftedUV, 0));
		float4 right = UnpackSimulation(KWS_CurrentTarget.SampleLevel(sampler_linear_clamp, i.shiftedUV + float2(KWS_CurrentTarget_TexelSize.x, 0), 0));
		float4 left = UnpackSimulation(KWS_CurrentTarget.SampleLevel(sampler_linear_clamp, i.shiftedUV - float2(KWS_CurrentTarget_TexelSize.x, 0), 0));
		float4 top = UnpackSimulation(KWS_CurrentTarget.SampleLevel(sampler_linear_clamp, i.shiftedUV + float2(0, KWS_CurrentTarget_TexelSize.y), 0));
		float4 down = UnpackSimulation(KWS_CurrentTarget.SampleLevel(sampler_linear_clamp, i.shiftedUV - float2(0, KWS_CurrentTarget_TexelSize.y), 0));
		
		
		
		#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_INTERSECTION

			float borderFade = GetDynamicWavesBorderFading(i.currentUV);
			float waterLevel = Texture2DSampleLevelBicubic(KWS_DynamicWavesIntersection, sampler_linear_clamp, i.currentUV, KWS_DynamicWavesIntersection_TexelSize, 0).x;
			center.z = max(center.z, waterLevel * 0.45);
			center.z *= saturate(borderFade * 100 + 0.997);
			
		#endif

		float localDt = ComputeLocalDt(center.xy, center.z);
		
		float currentHeight = center.z + center.w;
		float rightHeight = right.z + right.w;
		float topHeight = top.z + top.w;

	    float2 velocityChange;
		
	
		
	    velocityChange.x = -GRAVITY / GRID_CELL_SIZE * (rightHeight - currentHeight);
	    velocityChange.y = -GRAVITY / GRID_CELL_SIZE * (topHeight   - currentHeight);
		center.xy += velocityChange * localDt;

		
		#if defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
			ApplyVorticityConfinement(center, left, right, down, top, localDt, VORTICITY_MOVED_ZONE);
		#else 
			ApplyVorticityConfinement(center, left, right, down, top, localDt, VORTICITY_RIVER);
		#endif
		
		ClampVelocity(center.xy, localDt);
		
				
		UpdateBorders(center, right, top,  currentHeight, rightHeight, topHeight);
		UpdateDrySurfaceArtefacts(center, left, right, top, down);
		
		o.data = PackSimulation(center);
		o.normal = ComputeNormal(left, right, top, down);

		#ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_ADVECTED_UV
			o.uvAdvected =  ComputeAdvectedUV(i.currentUV, localDt, center);
		#endif
		
		return o;
		
	}


	ENDHLSL

	Subshader
	{

		//0 draw mesh mask
		Pass
		{
			ZTest LEqual
			Cull Back
			ZWrite On
			//Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma vertex vertMaskMesh
			#pragma fragment fragDrawMesh
			#pragma target 4.6
			#pragma editor_sync_compilation
			#pragma enable_d3d11_debug_symbols
			
			#pragma multi_compile_fragment _ KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
			#pragma multi_compile_fragment _ KWS_LOCAL_DYNAMIC_WAVES_USE_ADVECTED_UV
			#pragma multi_compile_fragment _ KWS_USE_OCEAN_RENDERING

			ENDHLSL
		}

		//1 draw procedural mask
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off

			//BlendOp Max
			//Blend One One
			Blend 0 SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma vertex vertMaskProcedural
			#pragma fragment fragDrawProcedural

			#pragma multi_compile_fragment _ KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
			#pragma multi_compile_fragment _ KWS_LOCAL_DYNAMIC_WAVES_USE_ADVECTED_UV
			#pragma multi_compile_fragment _ KWS_USE_OCEAN_RENDERING
			
			#pragma editor_sync_compilation
			#pragma enable_d3d11_debug_symbols
			
			#pragma target 4.6
			ENDHLSL
		}
		
		//2
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag1

			#pragma shader_feature _ KWS_DYNAMIC_WAVES_BAKE_MODE
			
			#pragma multi_compile_fragment _ KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE
			#pragma multi_compile_fragment _ KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
			#pragma multi_compile_fragment _ KWS_USE_OCEAN_RENDERING

			
			#pragma editor_sync_compilation
			#pragma enable_d3d11_debug_symbols
			
			#pragma target 4.6
			ENDHLSL
		}
		
		//3
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag2

			#pragma shader_feature _ KWS_DYNAMIC_WAVES_BAKE_MODE
			
			#pragma multi_compile_fragment _ KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE
			#pragma multi_compile_fragment _ KWS_LOCAL_DYNAMIC_WAVES_USE_ADVECTED_UV
			#pragma multi_compile_fragment _ KWS_LOCAL_DYNAMIC_WAVES_USE_INTERSECTION
			
			#pragma editor_sync_compilation
			#pragma enable_d3d11_debug_symbols
			
			#pragma target 4.6
			ENDHLSL
		}

		//4 draw map
		Pass
		{
			ZTest LEqual
			Cull Back
			ZWrite On
			
			BlendOp Max
			
			HLSLPROGRAM
			#pragma vertex vertMap
			#pragma fragment fragMap
			#pragma target 4.6
			#pragma editor_sync_compilation
			#pragma enable_d3d11_debug_symbols
			
			#pragma multi_compile_fragment _ KWS_LOCAL_DYNAMIC_WAVES_USE_ADVECTED_UV
			#pragma multi_compile_fragment _ KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR

			ENDHLSL
		}

		//5 bake postprocessing
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex vertBakePostprocessing
			#pragma fragment fragPostprocessing
			#pragma target 4.6
			#pragma editor_sync_compilation
			#pragma enable_d3d11_debug_symbols
			

			ENDHLSL
		}

		//6 draw intersections
		Pass
		{
			ZTest Always
			Cull Back
			ZWrite On
			
			BlendOp Max
			
			HLSLPROGRAM
			#pragma vertex vertMap
			#pragma fragment fragIntersections
			#pragma target 4.6
			#pragma editor_sync_compilation
			#pragma enable_d3d11_debug_symbols
			
			ENDHLSL
		}
	}
}