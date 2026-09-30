Shader "Game/CannonAimLine"
{
    Properties
    {
        [HDR] _Color ("Laser Color", Color) = (0.25, 1, 0.025, 1)
        _GlowStrength ("Glow Strength", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Overlay" "RenderType" = "Transparent" }
        Pass
        {
            ZTest LEqual
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _GlowStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float distanceToCenter = abs(input.uv.y * 2.0 - 1.0);
                float aa = max(fwidth(distanceToCenter), 0.015);
                float core = 1.0 - smoothstep(0.16 - aa, 0.16 + aa, distanceToCenter);
                float halo = pow(saturate(1.0 - distanceToCenter), 2.0) * _GlowStrength;
                half3 coreColor = lerp(_Color.rgb, half3(0.8, 1.0, 0.35), 0.45);
                return half4(lerp(_Color.rgb, coreColor, core), saturate(core + halo) * _Color.a);
            }
            ENDHLSL
        }
    }
}
