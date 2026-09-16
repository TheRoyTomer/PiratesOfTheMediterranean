Shader "KriptoFX/KWS2/KWS_DynamicWavesSplash"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
		
    	KWS_SplashParticlesAlphaMultiplier("Splash Particles Alpha Multiplier", Float) = 1
    	KWS_SplashParticlesRemap("Splash Particles Remap", Vector) = (1, 1, 0, 0)
		[Toggle(KWS_USE_SPLASH_PER_PIXEL_SHADOWS)] _UseCutout("Use Pixel Shadows", Int) = 1
		KWS_HeightOffset("Height OFfset", Float) = 0
    	[KeywordEnum(Add, Blend, Mul)] _BlendMode("Blend Mode", Float) = 1

    }




    HLSLINCLUDE
    #define KWS_LIGHTING_RECEIVE_DIR_SHADOWS
    

    #include "../Common/KWS_WaterHelpers.cginc"

    Texture2D _MainTex;
    float4 _MainTex_ST;
    SamplerState sampler_MainTex;
	float KWS_SplashParticlesAlphaMultiplier;
    float2 KWS_SplashParticlesRemap;
    float KWS_HeightOffset;

    
    struct appdata_t
    {
        float4 vertex : POSITION;
        float4 normal : NORMAL;
        half4 color : COLOR;
        float3 uv : TEXCOORD0; //uv.z = stable random
    	
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    ENDHLSL


    SubShader
    {

        //Tags { "RenderType" = "Opaque" "Queue" = "AlphaTest+1" }
        //Zwrite On
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent+1"
        }

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
            
            #pragma shader_feature _ KWS_USE_SPLASH_PER_PIXEL_SHADOWS
			#pragma shader_feature _ KWS_USE_DIR_SHADOW KWS_USE_ALL_SHADOWS

            #pragma multi_compile_fragment _ KWS_USE_CLIP_MASKING
            
			#define KWS_USE_DIR_SHADOW
            
            #ifdef KWS_BUILTIN

            #pragma multi_compile _ KWS_USE_DIR_LIGHT
            #pragma multi_compile _ KWS_USE_POINT_LIGHTS
            #pragma multi_compile _ KWS_USE_SPOT_LIGHTS

            #ifndef KWS_USE_ADDITIONAL_SHADOW
            #define KWS_DISABLE_POINT_SPOT_SHADOWS
            #endif

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
				//#pragma shader_feature _ _LIGHT_LAYERS //unity bug with rendering
				
            #endif


            #ifdef KWS_HDRP
				
				#pragma multi_compile _ SUPPORT_LOCAL_LIGHTS
				
            #endif


            #include "../PlatformSpecific/KWS_LightingHelpers.cginc"


            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;

                float4 screenPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
				nointerpolation float particleRandom01 : TEXCOORD3;
    	
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };


            v2f vert(appdata_t v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = ObjectToClipPos(v.vertex + float4(0, KWS_HeightOffset, 0, 0));
                o.color = v.color;
                o.uv.xy = v.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				o.particleRandom01 = v.uv.z;
                o.screenPos = ComputeScreenPos(o.vertex);
                o.worldPos = LocalToWorldPos(v.vertex.xyz + float3(0, KWS_HeightOffset, 0)).xyz;
                
               #ifndef KWS_USE_SPLASH_PER_PIXEL_SHADOWS
            		float2 screenUV = o.screenPos.xy / o.screenPos.w;
					o.color.rgb *= KWS_ComputeLighting(o.worldPos.xyz, 0.1, false, screenUV).xyz;
            		//o.color.xyz *= GetVolumetricSurfaceLight(screenUV).xyz;
				#endif
            	//o.color.a *= KWS_SplashParticlesAlphaMultiplier;
                return o;
            }


            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                float4 splashTex = _MainTex.SampleBias(sampler_MainTex, i.uv.xy, -1.0);
             
                float splashMain = splashTex.x;
                float splashShine = splashTex.y;
                float noise = splashTex.z;
                float splashDepth = splashTex.w;

                float3 waterColor = float3(0.75, 0.85, 1) * 0.9;

                if (splashMain + splashShine < 0.05) return 0;

                float lifeTime = 1 - i.color.a;


            	noise = saturate(noise - lifeTime * 2 + 1);
                splashShine = splashShine * noise;
                splashMain = splashMain * noise * lerp(0.3, 1, KWS_SplashParticlesAlphaMultiplier);
                splashShine = splashShine * splashShine * splashShine * lerp(0.35, 1, KWS_SplashParticlesAlphaMultiplier);

             
                float splashAlpha = saturate(splashMain * 0.5 + splashMain * splashMain * 2 + splashShine * 1) - lerp(0.1, 0, KWS_SplashParticlesAlphaMultiplier);

                float waterDepth = LinearEyeDepthUniversal(GetWaterDepth(screenUV));
                float sceneDepth = LinearEyeDepthUniversal(GetSceneDepth(screenUV));

                float surfaceDepth = LinearEyeDepthUniversal(i.screenPos.z / i.screenPos.w);
                float softParticlesFadeWater = saturate(1.0 * (waterDepth - surfaceDepth));
                float softParticlesFadeScene = saturate(1.0 * (sceneDepth - surfaceDepth));
                float softParticlesFade = min(softParticlesFadeWater, softParticlesFadeScene);
                splashAlpha *= lerp(softParticlesFade * saturate(KWS_Pow10(splashDepth) * 5), 1, softParticlesFade);
            	splashAlpha = saturate(splashAlpha * i.color.a);

            
                //return  float4(softParticlesFade, 0,0 , 1);
                #ifdef KWS_USE_SPLASH_PER_PIXEL_SHADOWS
                    i.color.rgb *= KWS_ComputeLighting(i.worldPos.xyz, 0.1, true, screenUV).xyz;
            	
                #endif

                #ifdef KWS_DYNAMIC_WAVES_USE_COLOR
                    //waterColor = lerp(waterColor, i.overrideColor.rgb, saturate(i.overrideColor.a * 2) * (0.8 + particleRandom01 * 0.4));
                #endif

                float3 splashColor = (waterColor * 1 + splashShine * 3) * clamp(i.color.rgb, 0, 1.25);
				splashColor = pow(abs(splashColor * KWS_SplashParticlesRemap.x), KWS_SplashParticlesRemap.y);
            	
                float4 finalColor = float4(splashColor, splashAlpha);
                //finalColor.a = lerp(finalColor.a, 0, i.fogData.w);

                return finalColor;
            }
            ENDHLSL
        }

        Pass
		{
			Tags { "LightMode" = "ShadowCaster"  }

			Cull Off
			ZWrite On

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			
			
			#define KWS_USE_SPLASH_SHADOW_CAST_FAST
			
			struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
				float2 uv : TEXCOORD0;
            	
                float4 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };


            v2f vert(appdata_t v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				#ifdef KWS_USE_SPLASH_SHADOW_CAST_FAST
					if (v.uv.z > 0.3)
					{
						o.vertex.w = NAN_VALUE;
						return o;
					}
            	#endif
            	
                o.vertex = ObjectToClipPos(v.vertex + float4(0, KWS_HeightOffset, 0, 0));
                o.color = v.color;
                o.uv.xy = v.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				
                o.screenPos = ComputeScreenPos(o.vertex);
                o.worldPos = LocalToWorldPos(v.vertex.xyz);
               
                return o;
            }


			float4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				//float ignNoise = KWS_InterleavedGradientNoise(i.pos.xy + i.particleRandom01 * 1000, KWS_ScaledTime * 1);
				//if (ignNoise > 0.75) discard;

				#ifdef KWS_USE_CLIP_MASKING
					if (ShouldClipWaterSurface(i.screenPos.xy/i.screenPos.w, i.screenPos.z / i.screenPos.w)) discard;
				#endif
				
                float4 splashTex = _MainTex.Sample(sampler_MainTex, i.uv.xy);
               
                float splashMain = splashTex.x;
                float splashShine = splashTex.y;
                float noise = splashTex.z;
                float splashDepth = splashTex.w;
				
				float lifeTime = 1 - i.color.a;

                noise = saturate(noise - lifeTime * 2 + 1);
                splashShine = splashShine * noise;
                splashMain = splashMain * noise * lerp(0.3, 1, KWS_SplashParticlesAlphaMultiplier);
                splashShine = splashShine * splashShine * splashShine * lerp(0.35, 1, KWS_SplashParticlesAlphaMultiplier);

             
                float splashAlpha = saturate(splashMain * 0.5 + splashMain * splashMain * 2 + splashShine * 1) - lerp(0.1, 0, KWS_SplashParticlesAlphaMultiplier);
				
				float blueNoise = KWS_BlueNoise3D.SampleLevel(sampler_linear_repeat, i.vertex.xy / 128.0, 0).x;
				float transparencyFactor = 0.75;

				#ifdef KWS_USE_SPLASH_SHADOW_CAST_FAST
					if (saturate(splashAlpha * 10 * transparencyFactor) < blueNoise.x) discard;
				#else
					if (saturate(splashAlpha * 2.5 * transparencyFactor) < blueNoise.x) discard;
				
				#endif
				
				return 0;
			}
			ENDHLSL
		}

    }
}