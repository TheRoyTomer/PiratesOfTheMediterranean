Shader "RenderWave/URP/OceanFarField"
{
    Properties
    {
        [Header(Colors)]
        _SurfaceColor("Shallow Color", Color) = (0.11, 0.55, 0.68, 1.0)
        _DeepColor("Deep Color", Color) = (0.03, 0.12, 0.19, 1.0)
        _HighlightColor("Highlight Color", Color) = (0.72, 0.9, 0.98, 1.0)

        [Header(Lighting)]
        _FresnelPower("Fresnel Power", Range(1.0, 8.0)) = 4.0
        _FresnelStrength("Fresnel Strength", Range(0.0, 1.5)) = 0.45
        _DiffuseStrength("Diffuse Strength", Range(0.0, 1.5)) = 0.85
        _SpecularStrength("Specular Strength", Range(0.0, 2.0)) = 0.45
        _SpecularExponent("Specular Exponent", Range(4.0, 128.0)) = 36.0

        [Header(WaveContract)]
        _LargeWaveDirection("Large Wave Direction", Vector) = (1, 0, 0, 0)
        _SmallWaveDirection("Small Wave Direction", Vector) = (0, 1, 0, 0)
        _LargeWaveData("Large Wave Data", Vector) = (0.65, 0.15, 0.35, 0)
        _SmallWaveData("Small Wave Data", Vector) = (0.15, 1.05, 1.3, 0)
        _RenderWaveTime("Wave Time", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-110"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 viewDirWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _SurfaceColor;
                half4 _DeepColor;
                half4 _HighlightColor;
                float _FresnelPower;
                float _FresnelStrength;
                float _DiffuseStrength;
                float _SpecularStrength;
                float _SpecularExponent;
                float4 _LargeWaveDirection;
                float4 _SmallWaveDirection;
                float4 _LargeWaveData;
                float4 _SmallWaveData;
                float _RenderWaveTime;
            CBUFFER_END

            float SampleWaveHeight(float2 worldXZ, out float2 gradient)
            {
                float largePhase = dot(worldXZ, _LargeWaveDirection.xy) * _LargeWaveData.y + (_RenderWaveTime * _LargeWaveData.z);
                float smallPhase = dot(worldXZ, _SmallWaveDirection.xy) * _SmallWaveData.y + (_RenderWaveTime * _SmallWaveData.z);

                float largeHeight = sin(largePhase) * _LargeWaveData.x;
                float smallHeight = sin(smallPhase) * _SmallWaveData.x;

                gradient.x = cos(largePhase) * _LargeWaveData.x * _LargeWaveData.y * _LargeWaveDirection.x;
                gradient.x += cos(smallPhase) * _SmallWaveData.x * _SmallWaveData.y * _SmallWaveDirection.x;
                gradient.y = cos(largePhase) * _LargeWaveData.x * _LargeWaveData.y * _LargeWaveDirection.y;
                gradient.y += cos(smallPhase) * _SmallWaveData.x * _SmallWaveData.y * _SmallWaveDirection.y;

                return largeHeight + smallHeight;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float2 gradient;
                worldPos.y += SampleWaveHeight(worldPos.xz, gradient);

                output.normalWS = normalize(half3(-gradient.x, 1.0, -gradient.y));
                output.positionCS = TransformWorldToHClip(worldPos);
                output.worldPos = worldPos;
                output.viewDirWS = SafeNormalize(GetCameraPositionWS() - worldPos);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float3 normalWS = normalize(float3(input.normalWS));
                float3 viewDirWS = normalize(float3(input.viewDirWS));
                float3 lightDirWS = normalize(GetMainLight().direction);

                if (!isFrontFace)
                {
                    float backView = saturate(-viewDirWS.y);
                    float3 undersideColor = lerp(_DeepColor.rgb * 0.9, _SurfaceColor.rgb * 0.65, backView);
                    undersideColor = MixFog(undersideColor, input.fogFactor);
                    return half4(undersideColor, 1.0);
                }

                float ndotl = saturate(dot(normalWS, lightDirWS));
                float3 halfVectorWS = normalize(lightDirWS + viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower) * _FresnelStrength;
                float specular = pow(saturate(dot(normalWS, halfVectorWS)), _SpecularExponent) * _SpecularStrength;

                float horizon = saturate(abs(viewDirWS.y));
                float depthBlend = saturate(1.0 - horizon);
                float3 baseColor = lerp(_SurfaceColor.rgb, _DeepColor.rgb, depthBlend);
                baseColor *= lerp(0.35, 1.0, ndotl * _DiffuseStrength);
                baseColor += _HighlightColor.rgb * (fresnel * 0.4 + specular);
                baseColor = MixFog(baseColor, input.fogFactor);
                return half4(baseColor, 1.0);
            }
            ENDHLSL
        }
    }
}
