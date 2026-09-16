Shader "Hidden/KriptoFX/KWS/KWS_JumpFloodSDF"
{

    HLSLINCLUDE
    #include "../../Common/KWS_WaterHelpers.cginc"

    Texture2D _SourceRT;

    float4 _SourceRT_TexelSize;


    float2 KWS_StepSize;
    float SDF_WaterLevel;


    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    v2f vert(uint vertexID : SV_VertexID)
    {
        v2f o;
        o.vertex = GetTriangleVertexPosition(vertexID);
        o.uv = GetTriangleUVScaled(vertexID);
        return o;
    }


    float4 fragPrePass(v2f i) : SV_Target
    {
        //float near = KWS_OrthoDepthNearFarSize.x;
        //float far = KWS_OrthoDepthNearFarSize.y;
        //float terrainDepth = _SourceRT.SampleLevel(sampler_linear_clamp, i.uv, 0).r * (far - near) - far;
        //return saturate(-terrainDepth);
        float terrainDepth = GetWaterOrthoDepth(i.uv) - SDF_WaterLevel;

		if (abs(terrainDepth) < 0.5) return float4(i.uv, 0, 1);
		else return 1;
    }

    struct FragmentOutput
    {
        half2 dest0 : SV_Target0;
        half dest1 : SV_Target1;
    };

    bool IsBorder(float2 pos)
    {
        return pos.x <= 0.0 || pos.x >= 1.0 || pos.y <= 0.0 || pos.y >= 1.0;
    }

    FragmentOutput fragJumpFlood(v2f i)
    {
        FragmentOutput o = (FragmentOutput)0;

        float closest_dist = 9999999.9;
        float2 closest_pos = 0;

        for (float x = -1.0; x <= 1.0; x += 1.0)
        {
            for (float y = -1.0; y <= 1.0; y += 1.0)
            {
                float2 voffset = i.uv + float2(x, y) * _SourceRT_TexelSize.xy * KWS_StepSize;

                //if (IsOutsideUvBorders(voffset.xy))                            continue;

                float2 pos = _SourceRT.SampleLevel(sampler_point_clamp, voffset, 0).xy;
                float dist = distance(pos * _SourceRT_TexelSize.zw, i.uv * _SourceRT_TexelSize.zw);

                if (pos.x != 0.0 && pos.y != 0.0 && dist < closest_dist && !IsBorder(pos))
                {
                    closest_dist = dist;
                    closest_pos = pos;
                }
            }
        }
        o.dest0 = closest_pos;
        o.dest1 = closest_dist;
        return o;
    }

    float GetSignedDistanceFromClosestPos(float2 uv)
    {
        float2 closestPos = _SourceRT.SampleLevel(sampler_linear_clamp, uv, 0).xy;
        if (closestPos.x == 0.0 && closestPos.y == 0.0) return 0.0;

        float unsignedDist = distance(closestPos * _SourceRT_TexelSize.zw, uv * _SourceRT_TexelSize.zw);

        float terrainDepth = GetWaterOrthoDepth(uv) - SDF_WaterLevel;

        // Choose sign convention. Here:
        // water side -> positive
        // land side  -> negative
        float signVal = terrainDepth < 0.0 ? 1.0 : -1.0;

        return unsignedDist * signVal;
    }


    float4 fragFinalize(v2f i) : SV_Target
    {
        float2 texel = _SourceRT_TexelSize.xy;

        float sdC = GetSignedDistanceFromClosestPos(i.uv);
        float sdL = GetSignedDistanceFromClosestPos(i.uv - float2(texel.x, 0));
        float sdR = GetSignedDistanceFromClosestPos(i.uv + float2(texel.x, 0));
        float sdD = GetSignedDistanceFromClosestPos(i.uv - float2(0, texel.y));
        float sdU = GetSignedDistanceFromClosestPos(i.uv + float2(0, texel.y));

        float2 grad = float2(sdR - sdL, sdU - sdD);

        float gradLen = length(grad);
        float2 shoreNormal = gradLen > 1e-5 ? grad / gradLen : float2(0, 1);

        return float4(shoreNormal * 0.5 + 0.5, sdC, 1.0);
    }

    float GaussianWeight(float x, float sigma)
    {
        return exp(-(x * x) / max(2.0 * sigma * sigma, 1e-5));
    }


    float4 fragBlurDirectionHQ(v2f i) : SV_Target
    {
        float2 texel = _SourceRT_TexelSize.xy;

        float4 center = _SourceRT.SampleLevel(sampler_linear_clamp, i.uv, 0);
        float2 centerDir = SafeNormalize2(center.xy * 2.0 - 1.0);
        float centerSdf = center.z;

        float2 dirAccum = 0.0;
        float weightAccum = 0.0;

        const int radius = 4;
        const float spatialSigma = 2.25;
        const float sdfSigma = 2.0;
        const float nearshoreDistance = 24.0;


        float nearshoreMask = 1.0 - saturate(abs(centerSdf) / nearshoreDistance);
        nearshoreMask = nearshoreMask * nearshoreMask;


        float blurStrength = lerp(0.15, 1.0, nearshoreMask);

        [loop]
        for (int y = -radius; y <= radius; y++)
        {
            [loop]
            for (int x = -radius; x <= radius; x++)
            {
                float2 offset = float2(x, y);
                float2 uv = i.uv + offset * texel;

                float4 sampleData = _SourceRT.SampleLevel(sampler_linear_clamp, uv, 0);
                float2 sampleDir = SafeNormalize2(sampleData.xy * 2.0 - 1.0);
                float sampleSdf = sampleData.z;

                float spatialW = GaussianWeight(length(offset), spatialSigma);


                float sdfDiff = abs(sampleSdf - centerSdf);
                float sdfW = GaussianWeight(sdfDiff, sdfSigma);


                float sampleNearshore = 1.0 - saturate(abs(sampleSdf) / nearshoreDistance);
                float shoreW = lerp(1.0, sampleNearshore, 0.35);

                float w = spatialW * sdfW * shoreW;

                dirAccum += sampleDir * w;
                weightAccum += w;
            }
        }

        float2 finalDir = centerDir;
        if (weightAccum > 1e-5)
        {
            float2 blurredDir = dirAccum / weightAccum;
            blurredDir = SafeNormalize2(blurredDir);

            finalDir = SafeNormalize2(lerp(centerDir, blurredDir, blurStrength));
        }

        return float4(finalDir * 0.5 + 0.5, center.z, center.w);
    }
    ENDHLSL

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        //0 copy source color to depth
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragPrePass
            ENDHLSL
        }

        //1 jump flood pass
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragJumpFlood
            ENDHLSL
        }

        //2 - finalize packed sdf
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragFinalize
            ENDHLSL
        }

        //3 - blur normal
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragBlurDirectionHQ
            ENDHLSL
        }
    }
}