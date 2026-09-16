Shader "KriptoFX/KWS2/Bubbles"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _AlphaThreshold("Alpha Threshold", Range (0, 1)) = 0.5

        _NormalMap("NormalMap", 2D) = "bump" {}
        _NormalStrength("NormalStrength", Float) = 1
        _EmissionColor("EmissionColor", Color) = (0,0,0,0)

        _NoiseTiling("NoiseTiling (XY)", Vector) = (1, 1, 0, 0)
        _NoiseStrength("NoiseStrength (XY Min Max)", Vector) = (0.25, 1, 0, 0)
        _NoiseOffsetMultiplier("NoiseOffsetMultiplier", Vector) = (3,3,0,0)
        
        [Toggle(KWS_USE_TILE_WARP_PARTICLES)] _UseTileWarp("Use Infinite Tile Warp Particles", Int) = 1
        [Toggle(KWS_REMOVE_UNDER_SURFACE)] _RemoveUnderSurface("Remove Under Surface", Int) = 0
    }




    HLSLINCLUDE
    #define KWS_LIGHTING_RECEIVE_DIR_SHADOWS

    #include "../PlatformSpecific/Includes/KWS_VertFragIncludes.cginc"

    DECLARE_TEXTURE(_MainTex);
    DECLARE_TEXTURE(_NormalMap);


    SamplerState sampler_MainTex;
    SamplerState sampler_NormalMap;



    float4 _EmissionColor;
    float _NormalStrength;
    float4 _NoiseTiling;
    float4 _NoiseStrength;
    float4 _NoiseOffsetMultiplier;
    float _AlphaThreshold;

    struct appdata_t
    {
        float4 vertex : POSITION;
        float4 normal : NORMAL;
        half4 color : COLOR;
        float4 uv : TEXCOORD0;
        float4 centerSize : TEXCOORD1;

        UNITY_VERTEX_INPUT_INSTANCE_ID
    };


    inline float4 UpdateParticlePosition(float3 center, float4 vertex, out float2 sizeAndDistanceToSurface)
    {
        #ifdef KWS_USE_TILE_WARP_PARTICLES
            vertex.xyz = TileWarpParticlesOffsetXZ(vertex.xyz, center);
        #endif

        float3 quadOffset = vertex.xyz - center.xyz;
        float quadOffsetLength = length(quadOffset) * 2;

        WaterOffsetData waterData = ComputeWaterOffset(vertex);
        float surfaceLevel = KWS_WaterLevel + waterData.offset.y;
        
        if (center.y > surfaceLevel - quadOffsetLength)
        {
            #ifdef KWS_REMOVE_UNDER_SURFACE
                quadOffset = 0;
            #endif
            center.y = surfaceLevel - quadOffsetLength;
            vertex.xyz = center.xyz + quadOffset;
        }
        sizeAndDistanceToSurface.x = quadOffsetLength;
        sizeAndDistanceToSurface.y = surfaceLevel - center.y;

        return vertex;
    }
    ENDHLSL


    SubShader
    {

        Tags
        {
            "Queue" = "Transparent+2"
        }

        Pass
        {
            
            
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma editor_sync_compilation

            #pragma target 4.6

            #pragma shader_feature _ KWS_USE_TILE_WARP_PARTICLES
            #pragma shader_feature _ KWS_REMOVE_UNDER_SURFACE
            
            #pragma multi_compile _ KWS_USE_OCEAN_RENDERING
            #pragma multi_compile _ KWS_USE_DYNAMIC_WAVES KWS_USE_COLORED_DYNAMIC_WAVES
            #pragma multi_compile _ KWS_USE_LOCAL_WATER_ZONES
            
            #pragma shader_feature_fragment _ KWS_USE_VOLUMETRIC_LIGHT

            #define KWS_USE_DIR_SHADOW
            //#define KWS_USE_ADDITIONAL_SHADOW

            #ifdef KWS_BUILTIN

            #pragma multi_compile _ KWS_USE_DIR_LIGHT
            #pragma multi_compile _ KWS_USE_POINT_LIGHTS
            #pragma multi_compile _ KWS_USE_SPOT_LIGHTS

            #define KWS_DISABLE_POINT_SPOT_SHADOWS

            #endif


            #ifdef KWS_URP

				#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
				#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
				
				#if !(UNITY_VERSION < 60010000)
                #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
                #else
                #pragma multi_compile _ _FORWARD_PLUS
                #endif
				//#pragma shader_feature _ _LIGHT_LAYERS
				
            #endif


            #ifdef KWS_HDRP
				
				#pragma multi_compile _ SUPPORT_LOCAL_LIGHTS
				
            #endif


            #include "../PlatformSpecific/KWS_LightingHelpers.cginc"


            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;

                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                nointerpolation float2 SizeAndDistanceToSurface : TEXCOORD3;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                v.vertex = UpdateParticlePosition(v.centerSize.xyz, v.vertex, o.SizeAndDistanceToSurface);
                o.vertex = ObjectToClipPos(v.vertex);
                o.color = v.color;
                o.worldPos.xyz = v.vertex; //particle system always in world pos
                o.uv = v.uv.xy;

                o.screenPos = ComputeScreenPos(o.vertex);
                float2 screenUV = o.screenPos.xy / o.screenPos.w;
                o.color.rgb *= KWS_ComputeLighting(o.worldPos.xyz, 0.05, false, screenUV);
                return o;
            }


            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

             //return 1;
                float size = i.SizeAndDistanceToSurface.x;
                float normalizedDistanceToSurface = saturate(i.SizeAndDistanceToSurface.y * 2);

                float2 noiseUV = float2(i.worldPos.x + i.worldPos.z, i.worldPos.y) * _NoiseOffsetMultiplier.xy;
                float2 noise = SAMPLE_TEXTURE(KWS_PerlinNoise, sampler_linear_repeat, (i.uv * _NoiseTiling) + noiseUV).rg;
                noise *= lerp(_NoiseStrength.x, _NoiseStrength.y, size) * normalizedDistanceToSurface;
                float4 albedo = SAMPLE_TEXTURE(_MainTex, sampler_MainTex, i.uv + noise);


                float bubbleShine = albedo.r;
                float bubbleColor = albedo.g;
                float alpha = (albedo.a - 0.5);

                float3 result = bubbleColor * 0.1 + bubbleShine * 20;
                result *= i.color.xyz * _EmissionColor.xyz;
                alpha *= saturate(dot(i.color, 0.33)) * i.color.a * _EmissionColor.a;
                alpha *= lerp(normalizedDistanceToSurface, 1, KWS_IsCameraPartialUnderwater);
                
                if (alpha < 0.01) discard;
                
                float transparent = KWS_Transparent;
                float3 turbidityColor = KWS_TurbidityColor;
                float3 waterColor = KWS_WaterColor;
                float waterLevel = KWS_WaterLevel;
                
                transparent = clamp(transparent + KWS_UnderwaterTransparentOffset, 1, KWS_MAX_TRANSPARENT * 2);
                
                #if defined(KWS_USE_LOCAL_WATER_ZONES)

                    LocalZoneData blendedZone = (LocalZoneData)0;
                    blendedZone.transparent = transparent;
                    blendedZone.turbidityColor.xyz = turbidityColor.xyz;
                    blendedZone.waterColor.xyz = waterColor.xyz;
                    float3 camPos = GetCameraAbsolutePosition();
                
                    EvaluateBlendedZoneData(blendedZone, camPos, normalize(i.worldPos - camPos), GetWorldToCameraDistance(i.worldPos), waterLevel + 20, noise);

                    transparent = blendedZone.transparent;
                    turbidityColor.xyz = blendedZone.turbidityColor.xyz;
                    waterColor.xyz = blendedZone.waterColor.xyz;

                #endif


                
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
               
                float distanceToVertex = GetWorldToCameraDistance(i.worldPos);
		        float3 volLight = GetVolumetricLightWithAbsorbtionByDistance(screenUV, screenUV, transparent, turbidityColor, waterColor, result, distanceToVertex, GetExposure(), 0).xyz;
               
                return float4(volLight, saturate(alpha));

            }
            ENDHLSL
        }

//
//        Pass
//        {
//            Tags
//            {
//                "LightMode" = "ShadowCaster"
//            }
//
//            Cull Off
//            ZWrite On
//
//            HLSLPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//            #pragma multi_compile_shadowcaster
//
//            #pragma shader_feature _ KWS_USE_TILE_WARP_PARTICLES
//            #pragma shader_feature _ KWS_REMOVE_UNDER_SURFACE

//            #pragma multi_compile _ KWS_USE_OCEAN_RENDERING
//            #pragma multi_compile _ KWS_USE_LOCAL_WATER_ZONES
//            #pragma multi_compile _ KWS_USE_DYNAMIC_WAVES KWS_USE_COLORED_DYNAMIC_WAVES
//
//
//            #define KWS_USE_DIR_SHADOW
//            //#define KWS_USE_ADDITIONAL_SHADOW
//
//            #ifdef KWS_BUILTIN
//
//            #pragma multi_compile _ KWS_USE_DIR_LIGHT
//            #pragma multi_compile _ KWS_USE_POINT_LIGHTS
//            #pragma multi_compile _ KWS_USE_SPOT_LIGHTS
//
//            #define KWS_DISABLE_POINT_SPOT_SHADOWS
//
//            #endif
//
//
//            #ifdef KWS_URP
//
//            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
//            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
//            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
//
//            #if !(UNITY_VERSION < 60010000)
//#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
//#else
//#pragma multi_compile _ _FORWARD_PLUS
//#endif

//
//            #endif
//
//
//            #ifdef KWS_HDRP
//				
//				#pragma multi_compile _ SUPPORT_LOCAL_LIGHTS
//				
//            #endif
//
//
//            #include "../PlatformSpecific/KWS_LightingHelpers.cginc"
//
//
//            struct v2f
//            {
//                float4 vertex : SV_POSITION;
//                half4 color : COLOR;
//                float2 uv : TEXCOORD0;
//                float3 worldPos : TEXCOORD1;
//                nointerpolation float2 SizeAndDistanceToSurface : TEXCOORD2;
//
//                UNITY_VERTEX_INPUT_INSTANCE_ID
//                UNITY_VERTEX_OUTPUT_STEREO
//            };
//
//
//            v2f vert(appdata_t v)
//            {
//                v2f o = (v2f)0;
//                UNITY_SETUP_INSTANCE_ID(v);
//                UNITY_TRANSFER_INSTANCE_ID(v, o);
//                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
//
//                v.vertex = UpdateParticlePosition(v.centerSize.xyz, v.vertex, o.SizeAndDistanceToSurface);
//                o.vertex = ObjectToClipPos(v.vertex);
//
//                o.worldPos.xyz = v.vertex; //particle system always in world pos
//                o.uv = v.uv.xy;
//
//                float4 screenPos = ComputeScreenPos(o.vertex);
//                float2 screenUV = screenPos.xy / screenPos.w;
//                o.color.rgb = KWS_ComputeLighting(o.worldPos.xyz, 0.05, false, screenUV);
//
//                return o;
//            }
//
//
//            float4 frag(v2f i) : SV_Target
//            {
//                float size = i.SizeAndDistanceToSurface.x;
//                float normalizedDistanceToSurface = saturate(i.SizeAndDistanceToSurface.y * 2);
//
//                float2 noiseUV = float2(i.worldPos.x + i.worldPos.z, i.worldPos.y) * _NoiseOffsetMultiplier.xy;
//                float2 noise = SAMPLE_TEXTURE(_NoiseTex, sampler_NoiseTex, (i.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw) + noiseUV).rg * 2 - 1;
//                noise *= lerp(_NoiseStrength.x, _NoiseStrength.y, size) * normalizedDistanceToSurface;
//                float4 albedo = SAMPLE_TEXTURE(_MainTex, sampler_MainTex, i.uv + noise);
//
//                float alpha = (albedo.a - 0.75);
//                alpha *= saturate(dot(i.color, 0.33));
//                alpha *= lerp(normalizedDistanceToSurface, 1, KWS_IsCameraPartialUnderwater);
//
//                float blueNoise = KWS_BlueNoise3D.SampleLevel(sampler_linear_repeat, i.vertex.xy / 128.0, 0).x;
//                if (alpha * _AlphaThreshold * normalizedDistanceToSurface < blueNoise.x) discard;
//
//                return float4(1, 1, 1, alpha);
//            }
//            ENDHLSL
//        }



    }
}