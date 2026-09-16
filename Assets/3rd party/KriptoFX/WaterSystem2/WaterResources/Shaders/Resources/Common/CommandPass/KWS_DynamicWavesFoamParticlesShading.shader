Shader "Hidden/KriptoFX/KWS/KWS_DynamicWavesFoamParticlesShading"
{

    SubShader
    {
        Blend SrcAlpha OneMinusSrcAlpha

        ZWrite Off
        Cull Off

        // pass 0:  render to texture
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.6
            #pragma enable_d3d11_debug_symbols
            
            #pragma multi_compile _ KWS_USE_PHYTOPLANKTON_EMISSION
            #pragma multi_compile _ KWS_USE_COLORED_DYNAMIC_WAVES
            
            #define KWS_USE_DYNAMIC_WAVES
            
            #include "../../Common/KWS_WaterHelpers.cginc"

            DECLARE_TEXTURE(_SourceRT);
            float4 _SourceRTHandleScale;
            float4 _SourceRT_TexelSize;
            
           
            
            float4 ComputePhytoplanktonColor(float foamDensity)
            {
	            float4 color = float4(0.01, 0.05, 0.5, 1.0);
	            color += lerp(0, float4(0.15, 0.25, 1.0, 1.0), (foamDensity > 2));
                color += lerp(0, float4(0.15, 0.5, 3, 1.0), (foamDensity > 10));
	            
	            return color * color;
            }

            struct vertexInput
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct vertexOutput
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            vertexOutput vert(vertexInput v)
            {
                vertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = GetTriangleVertexPosition(v.vertexID);
                o.uv = GetTriangleUVScaled(v.vertexID);
                return o;
            }

            half4 frag(vertexOutput i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 uv = i.uv;

                float f0 = SampleFoamLOD(uv, 0);
                float f1 = SampleFoamLOD(uv, 1);
                float f2 = SampleFoamLOD(uv, 2);

                f0 *= 1.0;
                f1 *= 1.0;
                f2 *= 1.0;

                float foamDensity = (f0 + f1 + f2);
                
             
                float alpha = 1.0;
                float foamDensityMultiplierScale = 1;
                
                float waterDepth = GetWaterDepth(uv);
                float3 waterWorldPos = GetWorldSpacePositionFromDepth(uv, waterDepth);
                float3 dynamicWavesMapUV = GetDynamicWavesMapUV(waterWorldPos, GetWorldToCameraDistanceXZ(waterWorldPos));
                bool isDynamicWavesZone = !IsOutsideUvBorders(dynamicWavesMapUV.xy);
		        if (isDynamicWavesZone)
		        {
		            DynamicWavesAdditionalData additionalData = GetDynamicWavesAdditionalMapBicubic(dynamicWavesMapUV);
		            ZoneData zone = KWS_ZoneData[additionalData.zoneID];
		            alpha *= zone.particlesAlpha;
		            foamDensityMultiplierScale *= zone.particlesAlpha;
		        }
              
                float3 lightColor = 1;
                #ifdef KWS_USE_PHYTOPLANKTON_EMISSION
                    lightColor = ComputePhytoplanktonColor(foamDensity);

                #else
                
                    #ifdef KWS_USE_COLORED_DYNAMIC_WAVES
                     if (isDynamicWavesZone)
                     {
                         float4 zoneColorData = GetDynamicWavesColorMap(dynamicWavesMapUV);
                         lightColor.xyz = lerp(float3(1, 1, 1), zoneColorData.rgb, saturate(zoneColorData.a * 2));
                     }
                    #endif

                    lightColor.xyz *= clamp(GetVolumetricSurfaceLight(uv).xyz, 0, 1.5) * float3(0.75, 0.85, 1);
                #endif

                
                
               //float3 foamColor = GetVolumetricSurfaceLight(uv).xyz * float3(0.75, 0.85, 1);
                float foamLow = saturate(foamDensity * saturate(0.1 + foamDensityMultiplierScale)) * 0.2;
                float foamHigh = saturate(foamDensity * foamDensity * 0.01 * foamDensityMultiplierScale) * 0.5;
                //foam = 1.0 - exp(-foam * Test4.x);
   
                return saturate(float4((lightColor.xyz * (foamLow + foamHigh) * alpha), 1));
              
            }
            ENDHLSL
        }

        //pass 1: dilation
        Pass
        {
            Blend One One
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.6
            #pragma enable_d3d11_debug_symbols

            #include "../../Common/KWS_WaterHelpers.cginc"

            DECLARE_TEXTURE(_SourceRT);
            float4 _SourceRTHandleScale;
            float4 _SourceRT_TexelSize;


            static const float gaussianWeight5[5] =
            {
                //0.003,
                0.0625,
                0.25,
                0.375,
                0.25,
                0.0625
                //0.003,
            };


            static const float gaussianWeight7[7] =
            {
                0.015627, //-3 0
                0.09375, //-2 1 
                0.234375, //-1 2
                0.312, //0 3
                0.234375, //1 4
                0.09375, //2 5
                0.015627 //3 6
            };

            struct vertexInput
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct vertexOutput
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            vertexOutput vert(vertexInput v)
            {
                vertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = GetTriangleVertexPosition(v.vertexID);
                o.uv = GetTriangleUVScaled(v.vertexID);
                return o;
            }
            
             half4 frag(vertexOutput i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv = i.uv;
                
                float foamColor = GetScreenSpaceFoam(uv).x;
                float2 px = float2(1, 0);
                float2 py = float2(0, 1);
                
		        foamColor = max(foamColor, GetScreenSpaceFoam(uv, px).x);
		        foamColor = max(foamColor, GetScreenSpaceFoam(uv, -px).x);
                foamColor = max(foamColor, GetScreenSpaceFoam(uv, py).x);
                foamColor = max(foamColor, GetScreenSpaceFoam(uv, -py));
                    
               
            	return float4(foamColor.xxx, 1);
            }

            ENDHLSL
        }



        //pass 2:  render to camera
        Pass
        {
            Blend One One
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.6
            #pragma enable_d3d11_debug_symbols

            #include "../../Common/KWS_WaterHelpers.cginc"

            DECLARE_TEXTURE(_SourceRT);
            float4 _SourceRTHandleScale;
            float4 _SourceRT_TexelSize;


            struct vertexInput
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct vertexOutput
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            vertexOutput vert(vertexInput v)
            {
                vertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = GetTriangleVertexPosition(v.vertexID);
                o.uv = GetTriangleUVScaled(v.vertexID);
                return o;
            }
            
             half4 frag(vertexOutput i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv = i.uv;
                float3 foamColor = GetScreenSpaceFoam(uv).xyz;
            	return float4(foamColor, 1);
            }

            ENDHLSL
        }



    }
}