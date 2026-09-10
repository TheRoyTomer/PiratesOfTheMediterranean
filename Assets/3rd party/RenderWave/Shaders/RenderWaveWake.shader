Shader "RenderWave/URP/WakeSurface"
{
    Properties
    {
        [Header(WakeTint)]
        _WakeColor("Wake Color", Color) = (0.82, 0.93, 0.98, 0.6)

        [Header(WakeShape)]
        _EdgeFeather("Edge Feather", Range(0.5, 8.0)) = 2.5
        _LengthFeather("Length Feather", Range(0.02, 0.45)) = 0.18
        _CenterHighlight("Center Highlight", Range(0.0, 2.0)) = 0.7

        [Header(WakeLighting)]
        _SoftLighting("Soft Lighting", Range(0.0, 1.0)) = 0.35
        _FresnelStrength("Fresnel Strength", Range(0.0, 1.0)) = 0.2
        _FresnelPower("Fresnel Power", Range(1.0, 8.0)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-40"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

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
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _WakeColor;
                float _EdgeFeather;
                float _LengthFeather;
                float _CenterHighlight;
                float _SoftLighting;
                float _FresnelStrength;
                float _FresnelPower;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.worldPos);
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centeredUV = abs((input.uv * 2.0) - 1.0);
                float sideMask = pow(saturate(1.0 - centeredUV.x), _EdgeFeather);
                float frontFade = smoothstep(0.0, _LengthFeather * 0.7, input.uv.y);
                float backFade = 1.0 - smoothstep(1.0 - (_LengthFeather * 1.6), 1.0, input.uv.y);
                float lengthMask = frontFade * backFade;
                float coreMask = saturate(1.0 - (centeredUV.x * 0.55));
                float mask = sideMask * max(lengthMask, coreMask * 0.35);
                float alpha = _WakeColor.a * input.color.a * mask;
                clip(alpha - 0.001);

                float3 geometricNormal = normalize(cross(ddy(input.worldPos), ddx(input.worldPos)));
                if (geometricNormal.y < 0.0)
                {
                    geometricNormal *= -1.0;
                }

                float3 viewDirWS = SafeNormalize(GetCameraPositionWS() - input.worldPos);
                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(geometricNormal, normalize(mainLight.direction)));
                float fresnel = pow(1.0 - saturate(dot(geometricNormal, viewDirWS)), _FresnelPower) * _FresnelStrength;
                float centerBand = pow(saturate(1.0 - centeredUV.x), 1.35) * _CenterHighlight;

                float lighting = lerp(0.72, 1.0 + _SoftLighting, ndotl);
                float3 color = _WakeColor.rgb * lighting;
                color = lerp(color, color * 1.35, centerBand);
                color += _WakeColor.rgb * (fresnel * 0.35);
                color *= lerp(1.0, mainLight.color.rgb, 0.25);
                color = MixFog(color, input.fogFactor);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
