Shader "Hidden/KriptoFX/KWS/KWS_DynamicWavesFoamParticles"
{
    Properties
	{
		[HideInInspector]KWS_StencilMaskValue("KWS_StencilMaskValue", Int) = 32
	}
    
    HLSLINCLUDE
    #define KWS_USE_SOFT_SHADOWS
    #define KWS_USE_DYNAMIC_WAVES
    
    #include "../../Common/KWS_WaterHelpers.cginc"
    //#include "../../PlatformSpecific/KWS_Lighting.cginc"
    	
	#if defined(ENVIRO_3_FOG)
        #pragma multi_compile_fragment _ ENVIRO_VOLUMELIGHT
		#pragma multi_compile_fragment _ ENVIRO_SIMPLEFOG
		#pragma multi_compile_fragment _ ENVIRO_SIMPLESKY
	#endif
				

    Texture2D KWS_DynamicWavesFoamShadowMap;
    float4 KWS_DynamicWavesFoamShadowMap_TexelSize;

    float KWS_ParticlesFoamInterpolationTime;
    float KWS_FoamParticlesScale;
    float KWS_FoamParticlesAlphaMultiplier;


    // static const float2 quadOffsets[3] =
    // {
    // 	float2(1, -1),
    // 	float2(-1, -1),
    // 	float2(0, 1)
    // };

    static const float2 quadOffsets[6] =
    {
        float2(-1, -1),
        float2(1, -1),
        float2(-1, 1),

        float2(-1, 1),
        float2(1, -1),
        float2(1, 1)
    };

    #define FOAM_SIZE_MIN 0.03
    #define FOAM_SIZE_MAX 0.04

    float4 ComputePhytoplanktonColor(float initialRandom01)
    {
	    float4 color = float4(0.01, 0.1, 1, 0.3);
	    color += lerp(0, float4(0.1, 0.5, 3, 0.5), initialRandom01 > 0.93);
        color += lerp(0, float4(0.1, 1.0, 10, 0.9), initialRandom01 > 0.995);
	    
	    return color;
    }


    void GetDynamicWavesFoamParticlesVertexPosition(FoamParticle particle, uint vertexID, float farDistance, out float3 vertex, out float2 uv)
    {
        float3 center = lerp(particle.prevPosition, particle.position, KWS_ParticlesFoamInterpolationTime);
        float currentLifeTime = lerp(particle.prevLifetime, particle.currentLifetime, KWS_ParticlesFoamInterpolationTime);
        float normalizedLifeTime = currentLifeTime / particle.maxLifeTime;
        
        float oceanLevel = KWS_WaterLevel;
        float maxWaveDisplacement = lerp(2, 20, saturate(KWS_WindSpeed * 0.02));
        if (center.y < oceanLevel + maxWaveDisplacement)
        {
            float2 dynamicWavesUV = GetDynamicWavesUV(center);
            float4 dynamicWaves = GetDynamicWavesZone(dynamicWavesUV);
            float4 dynamicWavesAdditionalData = GetDynamicWavesZoneAdditionalData(dynamicWavesUV);
            float3 disp = GetFftWavesDisplacement(center);
            float shorelineMask = dynamicWavesAdditionalData.y;
            float fade = GetDynamicWavesBorderFading(dynamicWavesUV);
            
            disp.y = GetDynamicWavesAttenuatedOceanHeight(dynamicWaves.w, disp.y, shorelineMask, fade);
            disp *= shorelineMask;
            
            center += disp;
        }

        float particleSize = lerp(FOAM_SIZE_MIN, FOAM_SIZE_MAX, saturate(0.1 * length(particle.velocity.xz))) * KWS_FoamParticlesScale;
        //particleSize *= 1 + farDistance * 5;
        //particleSize *= saturate(sin((normalizedLifeTime - 0.25) * 3.1415) * 5);
        //float liftSpeed = saturate((particle.position.y - particle.prevPosition.y) * 2);
        //particleSize *= (1-liftSpeed);

        float2 offset = quadOffsets[vertexID];
        uv = offset * 0.5 + 0.5;

        float3 motionVector =  (particle.position - particle.prevPosition);
    
        float2 velocityXY = float2(dot(motionVector, KWS_CameraRight), dot(motionVector, KWS_CameraUp));
        float2 velocityDir = normalize(velocityXY + 1e-5);
        float2 orthoDir = float2(-velocityDir.y, velocityDir.x);


        float velocitySizeFactor = saturate(length(particle.velocity.xz * 0.3));
        float stretch = lerp(2.0, 7.0, velocitySizeFactor);
        float2 screenOffset = velocityDir * offset.y * particleSize * stretch + orthoDir * offset.x * particleSize;
        vertex = center + KWS_CameraRight * screenOffset.x + KWS_CameraUp * screenOffset.y;
        vertex.y += lerp(0.1, 0.35, velocitySizeFactor) * saturate(0.35 + KWS_FoamParticlesScale);
    }


    struct appdata
    {
        uint instanceID : SV_InstanceID;
        uint vertexID : SV_VertexID;
    };
    ENDHLSL


    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+1" }

//        Stencil
//		{
//			Ref [KWS_StencilMaskValue]
//            ReadMask [KWS_StencilMaskValue]
//			Comp Greater
//			Pass keep
//		}
        
        Pass
        {

            Blend SrcAlpha OneMinusSrcAlpha
            //Blend SrcAlpha One
            Zwrite Off
            
            Cull Off
            
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma editor_sync_compilation
            #pragma target 4.6

            #ifdef UNITY_STEREO_INSTANCING_ENABLED
                #pragma multi_compile _ STEREO_INSTANCING_ON
            #endif
            
            #pragma multi_compile _ KWS_USE_DIR_LIGHT
            #pragma multi_compile _ KWS_USE_PHYTOPLANKTON_EMISSION
            #pragma multi_compile _ KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR
            #pragma multi_compile_fragment _ KWS_USE_CLIP_MASKING
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
                nointerpolation float speed : TEXCOORD1;
                float3 worldPos : TECXOORD2;
                float4 screenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };


            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                float3 vertex;
                float2 uv;
                FoamParticle particle = KWS_FoamParticlesBuffer[v.instanceID];

                float cameraDistance = GetWorldToCameraDistance(particle.position);
                float farDistance = saturate(cameraDistance * 0.0025);

                GetDynamicWavesFoamParticlesVertexPosition(particle, v.vertexID, farDistance, vertex, uv);

                vertex.y += farDistance * 0.5;

                o.pos = ObjectToClipPos(float4(vertex, 1.0));
                o.uv = uv;

                o.screenPos = ComputeScreenPos(o.pos);
                float2 screenUV = o.screenPos.xy / o.screenPos.w;
                bool isUnderwater = GetUnderwaterMask(GetWaterMaskFast(screenUV));
                if (isUnderwater) o.pos.w = NAN_VALUE;
                o.worldPos = LocalToWorldPos(vertex);
                
               
                float2 dynamicWavesUV = GetDynamicWavesUVRotated(o.worldPos);
      
                float borderFade = GetDynamicWavesBorderFading(dynamicWavesUV);
                o.color.rgb = 1;
                o.color.a = saturate(borderFade * 4);

               
                
                #ifdef KWS_USE_PHYTOPLANKTON_EMISSION
                    o.color.rgb = ComputePhytoplanktonColor(particle.initialRandom01).xyz;
                #else
                
                    #ifdef KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR

                        float4 zoneColorData = GetDynamicWavesZoneColorData(dynamicWavesUV);
                        //zoneColorData.rgb = lerp(zoneColorData.rgb, zoneColorData.rgb * 0.35, saturate(zoneColorData.a * zoneColorData.a + zoneColorData.a * 2));
                        o.color.rgb = lerp(float3(1, 1, 1), zoneColorData.rgb, saturate(zoneColorData.a * 2));

                    #endif

                    o.color.rgb *= clamp(GetVolumetricSurfaceLight(screenUV).xyz, 0, 1.15);
                #endif

              
              
                

                return o;
            }


            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                #ifdef KWS_USE_CLIP_MASKING
					if (ShouldClipWaterSurface(i.screenPos.xy/i.screenPos.w, i.screenPos.z / i.screenPos.w)) discard;
				#endif
                
                
                float bubbleAlpha = 0.1 * KWS_FoamParticlesAlphaMultiplier;
                //float alpha = saturate(1 - length(i.uv - float2(0.5, 0.33)) * 2.5);
                float alpha = saturate(1 - length(i.uv - float2(0.5, 0.5)) * 2);
                alpha = saturate(KWS_Pow2(alpha) * 10);

                float3 bubblesColor = float3(0.75, 0.85, 1);

                bubblesColor *= i.color.rgb;
                alpha *= i.color.a;
                alpha = saturate(bubbleAlpha * alpha);
                
                half3 fogColor;
                half3 fogOpacity;

                #ifdef KWS_HDRP
                    GetInternalFogVariables(i.pos, GetWorldSpaceViewDirNorm(i.worldPos), LinearEyeDepthUniversal(i.pos.z), i.worldPos, fogColor, fogOpacity);
                #else
                    GetInternalFogVariables(LinearEyeDepthUniversal(i.pos.z), fogColor, fogOpacity);
                #endif
                
                bubblesColor = ComputeInternalFog(bubblesColor, fogColor, fogOpacity);
                bubblesColor = ComputeThirdPartyFog(bubblesColor, i.worldPos, i.pos.xy / _ScreenParams.xy, i.pos.z);
                return 0;
                return float4(bubblesColor, alpha);
            }
            ENDHLSL
        }


/*
        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
                float2 uv : TEXCOORD0;
                uint particleType : TEXCOORD1;
                nointerpolation float uvOffset : TEXCOORD2;
                nointerpolation float normalizedLifeTime : TEXCOORD3;
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_INITIALIZE_OUTPUT(v2f, o);

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

           
                float3 vertex;
                float2 uv;
                float4 color;
                FoamParticle particle = KWS_FoamParticlesBuffer[v.instanceID];

                float cameraDistance = GetWorldToCameraDistance(particle.position);
                float farDistance = saturate(cameraDistance * 0.0025);

                GetDynamicWavesFoamParticlesVertexPosition(particle, v.vertexID, farDistance, vertex, uv, color);

                vertex.y += farDistance * 0.5;

                o.pos = ObjectToClipPos(float4(vertex, 1.0));
                o.uv = uv;

                float4 screenPos = ComputeScreenPos(o.pos);
                float2 screenUV = screenPos.xy / screenPos.w;
                bool isUnderwater = GetUnderwaterMask(GetWaterMaskFast(screenUV));
                if (isUnderwater) o.pos.w = NAN_VALUE;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float bubbleAlpha = 0.1 * KWS_FoamParticlesAlphaMultiplier;
                float alpha = saturate(1 - length(i.uv - float2(0.5, 0.5)) * 2);
                alpha = saturate(KWS_Pow2(alpha) * 10);
                alpha = saturate(bubbleAlpha * alpha);
                
                if (alpha < 0.01) discard;
                
                UNITY_SETUP_INSTANCE_ID(i);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDHLSL
        }

*/


    }
}