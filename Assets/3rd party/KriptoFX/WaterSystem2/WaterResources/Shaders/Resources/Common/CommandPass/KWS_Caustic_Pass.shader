Shader "Hidden/KriptoFX/KWS/Caustic_Pass"
{
    HLSLINCLUDE
    #include "../../Common/KWS_WaterHelpers.cginc"

    uint KWS_CausticCascadeIndex;
    Texture2DArray KWS_BakedDisplacementFft;
    float4 KWS_BakedDisplacementFft_TexelSize;
    uint KWS_BakedDisplacementSliceIndex;
    float KWS_DispacementAngleOffset;
    float KWS_DispacementScale;
    float KWS_CausticDispersionStrength;

    static const float DepthScaleRelativeToWindMin[3] =
    {
        1.0,
        0.5,
        0.25
    };
    static const float DepthScaleRelativeToWindMax[3] =
    {
        0.35,
        0.1,
        0.025
    };

    struct appdata_caustic
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f_caustic
    {
        float4 vertex : SV_POSITION;
        float3 oldPos : TEXCOORD0;
        float3 newPos : TEXCOORD1;
        #ifdef KWS_USE_CAUSTIC_DISPERSION
            float3 newPosX : TEXCOORD2;
            float3 newPosZ : TEXCOORD3;
        #endif
    };

    float GetDepthScale()
    {
        float normalizedWind = saturate(max(0, KWS_WindSpeed) / 7.0);
        normalizedWind = pow(normalizedWind, 0.35);
        float depthScale = lerp(2 * DepthScaleRelativeToWindMin[KWS_CausticCascadeIndex], DepthScaleRelativeToWindMax[KWS_CausticCascadeIndex], normalizedWind);
        //float depthScale = lerp(KWS_CausticDepthScale, KWS_CausticDepthScale * DepthScaleRelativeToWind[KWS_CausticCascadeIndex], normalizedWind) * DepthScaleRelativeToCascade[KWS_CausticCascadeIndex];
        //depthScale = min(depthScale, MaxDepthScaleRelativeToWind[KWS_CausticCascadeIndex]);
        return depthScale;
    }

    float GetDispersionStrength()
    {
        float normalizedWind = saturate(max(0, KWS_WindSpeed) / 10.0);
        return lerp(0.1, 1, normalizedWind) * KWS_CausticDispersionStrength * 0.015;
    }

    float2 ComputeDisplacementXZ(float2 uv)
    {
        #ifdef KWS_USE_CAUSTIC_FILTERING
			float2 displacement = GetFftWavesDisplacementSliceBicubic(float3(uv.x, 0, -uv.y) * KWS_WavesDomainScaledSizes[KWS_CausticCascadeIndex], KWS_CausticCascadeIndex).xz;
        #else
            float2 displacement = GetFftWavesDisplacementSlice(float3(uv.x, 0, -uv.y) * KWS_WavesDomainScaledSizes[KWS_CausticCascadeIndex], KWS_CausticCascadeIndex).xz;
        #endif

        displacement *= float2(1, -1);
        displacement *= GetDepthScale();
        return displacement;
    }

    float2 ComputeBakeDisplacementXZ(float2 uv)
    {
        float2 displacement = Texture2DArraySampleLevelBicubic(KWS_BakedDisplacementFft, sampler_linear_repeat, float2(uv.x, -uv.y), KWS_BakedDisplacementFft_TexelSize, KWS_BakedDisplacementSliceIndex, 0).xz;

        displacement *= float2(1, -1);
        displacement *= KWS_DispacementScale;
        return displacement;
    }

    v2f_caustic vert_caustic(appdata_caustic v)
    {
        v2f_caustic o;

        o.oldPos = v.vertex.xyz;

        #ifdef KWS_CAUSTIC_BAKE_MODE

            o.newPosX = v.vertex.xyz;
            o.newPosZ = v.vertex.xyz;
            o.newPosX.xy += ComputeBakeDisplacementXZ(v.vertex.xy + GetDispersionStrength());
            o.newPosZ.xy += ComputeBakeDisplacementXZ(v.vertex.xy - GetDispersionStrength());

            v.vertex.xy += ComputeBakeDisplacementXZ(v.vertex.xy);
        #else

            #ifdef KWS_USE_CAUSTIC_DISPERSION
                o.newPosX = v.vertex.xyz;
                o.newPosZ = v.vertex.xyz;
                o.newPosX.xy += ComputeDisplacementXZ(v.vertex.xy + GetDispersionStrength());
                o.newPosZ.xy += ComputeDisplacementXZ(v.vertex.xy - GetDispersionStrength());
            #endif
            v.vertex.xy += ComputeDisplacementXZ(v.vertex.xy);
        #endif

        o.newPos = v.vertex.xyz;


        o.vertex = float4(v.vertex.xy, 0, 0.5);
        return o;
    }
    
    inline float Area2(float2 dx, float2 dy)
    {
        return abs(dx.x * dy.y - dx.y * dy.x);
    }
    
    half4 frag_caustic(v2f_caustic i) : SV_Target
    {
        float oldArea = length(ddx(i.oldPos.xyz)) * length(ddy(i.oldPos.xyz));
        float newArea = length(ddx(i.newPos.xyz)) * length(ddy(i.newPos.xyz));
        float causticDiff = oldArea / newArea;
        float colorY = causticDiff * KWS_CAUSTIC_MULTIPLIER;

         #ifdef KWS_USE_CAUSTIC_DISPERSION
            oldArea = length(ddx(i.oldPos.xyz)) * length(ddy(i.oldPos.xyz));
            newArea = length(ddx(i.newPosX.xyz)) * length(ddy(i.newPosX.xyz));
            float colorX = oldArea / newArea * KWS_CAUSTIC_MULTIPLIER;

            oldArea = length(ddx(i.oldPos.xyz)) * length(ddy(i.oldPos.xyz));
            newArea = length(ddx(i.newPosZ.xyz)) * length(ddy(i.newPosZ.xyz));
            float colorZ = oldArea / newArea * KWS_CAUSTIC_MULTIPLIER;

            return float4(colorX, colorY, colorZ, 1-saturate(1 * causticDiff));
        #else
            return float4(colorY, colorY, colorY, 1);
        #endif
       
    }
    ENDHLSL

    SubShader
    {
        //Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

        Blend One One
        ZWrite Off
        ZTest Always
        Cull Off

        //pass 0 realtime 
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_caustic
            #pragma fragment frag_caustic

            #pragma multi_compile _ KWS_USE_CAUSTIC_FILTERING
            #pragma multi_compile _ KWS_USE_CAUSTIC_DISPERSION
            #pragma shader_feature _ KWS_CAUSTIC_BAKE_MODE
            ENDHLSL
        }
    }
}