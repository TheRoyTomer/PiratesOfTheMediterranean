Shader "RenderWave/URP/OceanSurface"
{
    Properties
    {
        [Header(Colors)]
        _SurfaceColor("Shallow Color", Color) = (0.11, 0.55, 0.68, 1.0)
        _DeepColor("Deep Color", Color) = (0.03, 0.12, 0.19, 1.0)
        _HighlightColor("Highlight Color", Color) = (0.72, 0.9, 0.98, 1.0)

        [Header(Modes)]
        [Enum(Classic,0,Enhanced,1)] _VisualMode("Visual Mode", Float) = 1
        [Enum(Low,0,Medium,1,High,2)] _QualityMode("Quality Mode", Float) = 2

        [Header(DepthBlend)]
        _DepthBlendDistance("Depth Blend Distance", Range(0.25, 50.0)) = 10.0
        _DepthBlendBias("Depth Blend Bias", Range(-1.0, 1.0)) = 0.0

        [Header(ContactFoam)]
        [Toggle] _ContactFoamEnabled("Enable Contact Foam", Float) = 0
        _ContactFoamColor("Foam Color", Color) = (0.9, 0.96, 1.0, 1.0)
        _ContactFoamDistance("Foam Distance", Range(0.02, 4.0)) = 0.55
        _ContactFoamSoftness("Foam Softness", Range(0.01, 2.0)) = 0.22
        _ContactFoamIntensity("Foam Intensity", Range(0.0, 2.0)) = 0.9
        _ContactFoamBreakupScale("Foam Breakup Scale", Range(0.01, 1.5)) = 0.18
        _ContactFoamBreakupStrength("Foam Breakup Strength", Range(0.0, 1.0)) = 0.35
        _ContactFoamEdgeSensitivity("Foam Edge Sensitivity", Range(0.0, 12.0)) = 6.0

        [Header(NormalMotion)]
        [Normal] _NormalMapA("Normal Map A", 2D) = "bump" {}
        [Normal] _NormalMapB("Normal Map B", 2D) = "bump" {}
        _NormalTilingA("Normal Tiling A", Range(0.002, 0.25)) = 0.04
        _NormalTilingB("Normal Tiling B", Range(0.002, 0.25)) = 0.085
        _NormalSpeedA("Normal Speed A", Range(0.0, 4.0)) = 0.25
        _NormalSpeedB("Normal Speed B", Range(0.0, 4.0)) = 0.5
        _NormalStrength("Normal Strength", Range(0.0, 2.0)) = 0.75

        [Header(Lighting)]
        _FresnelPower("Fresnel Power", Range(1.0, 8.0)) = 4.0
        _FresnelStrength("Fresnel Strength", Range(0.0, 1.5)) = 0.45
        _DiffuseStrength("Diffuse Strength", Range(0.0, 1.5)) = 0.85
        _SpecularStrength("Specular Strength", Range(0.0, 2.0)) = 0.5
        _SpecularExponent("Specular Exponent", Range(4.0, 128.0)) = 48.0
        _VariationStrength("Variation Strength", Range(0.0, 1.0)) = 0.15

        [Header(WaveContract)]
        _LargeWaveDirection("Large Wave Direction", Vector) = (1, 0, 0, 0)
        _SmallWaveDirection("Small Wave Direction", Vector) = (0, 1, 0, 0)
        _LargeWaveData("Large Wave Data", Vector) = (0.65, 0.15, 0.35, 0)
        _SmallWaveData("Small Wave Data", Vector) = (0.15, 1.05, 1.3, 0)
        _RenderWaveTime("Wave Time", Float) = 0

        [Header(ZoneClipping)]
        _ZoneClipCenter("Zone Clip Center", Vector) = (0, 0, 0, 0)
        _ZoneClipHalfExtents("Zone Clip Half Extents", Vector) = (0, 0, 0, 0)
        _ZoneClipEnabled("Zone Clip Enabled", Float) = 0
        _ZoneBoundaryEpsilon("Zone Boundary Epsilon", Float) = 0

        [Header(Underside)]
        // Requires "Opaque Texture" enabled in the URP Render Pipeline Asset.
        // Set Refraction Blend to 0 if that setting is off.
        _UndersideTintColor("Underside Tint Color", Color) = (0.04, 0.18, 0.30, 1.0)
        _UndersideDistortionStrength("Distortion Strength", Range(0.0, 0.06)) = 0.018
        _UndersideRefractionBlend("Refraction Blend", Range(0.0, 1.0)) = 0.65
        _UndersideOpticalWindow("Optical Window Strength", Range(0.0, 1.0)) = 0.4

        [Header(Debug)]
        [Toggle] _ForceUndersideDebug("Force Underside View (Debug)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-100"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            // Cull Off enables the underside backface path at no extra draw call cost.
            // From above water, front faces are nearer to the camera and write depth
            // first; back faces are at the same depth so they are discarded without
            // producing any visible output or measurable overhead.
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_NormalMapA);
            SAMPLER(sampler_NormalMapA);
            TEXTURE2D(_NormalMapB);
            SAMPLER(sampler_NormalMapB);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                half3 geometricNormalWS : TEXCOORD2;
                half3 viewDirWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _SurfaceColor;
                half4 _DeepColor;
                half4 _HighlightColor;
                float _VisualMode;
                float _QualityMode;
                float _DepthBlendDistance;
                float _DepthBlendBias;
                float _ContactFoamEnabled;
                half4 _ContactFoamColor;
                float _ContactFoamDistance;
                float _ContactFoamSoftness;
                float _ContactFoamIntensity;
                float _ContactFoamBreakupScale;
                float _ContactFoamBreakupStrength;
                float _ContactFoamEdgeSensitivity;
                float _NormalTilingA;
                float _NormalTilingB;
                float _NormalSpeedA;
                float _NormalSpeedB;
                float _NormalStrength;
                float _FresnelPower;
                float _FresnelStrength;
                float _DiffuseStrength;
                float _SpecularStrength;
                float _SpecularExponent;
                float _VariationStrength;
                float4 _LargeWaveDirection;
                float4 _SmallWaveDirection;
                float4 _LargeWaveData;
                float4 _SmallWaveData;
                float _RenderWaveTime;
                float4 _ZoneClipCenter;
                float4 _ZoneClipHalfExtents;
                float _ZoneClipEnabled;
                float _ZoneBoundaryEpsilon;
                half4 _UndersideTintColor;
                float _UndersideDistortionStrength;
                float _UndersideRefractionBlend;
                float _UndersideOpticalWindow;
                float _ForceUndersideDebug;
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

            float3 BuildSurfaceBasisTangent(float3 geometricNormalWS)
            {
                float3 tangent = normalize(float3(_LargeWaveDirection.x, 0.0, _LargeWaveDirection.y));
                tangent = normalize(tangent - geometricNormalWS * dot(geometricNormalWS, tangent));

                if (dot(tangent, tangent) < 0.0001)
                {
                    tangent = normalize(float3(1.0, 0.0, 0.0) - geometricNormalWS * geometricNormalWS.x);
                }

                return tangent;
            }

            float3 SampleDetailNormal(float3 worldPos, float3 geometricNormalWS, float qualityMode)
            {
                float3 tangentWS = BuildSurfaceBasisTangent(geometricNormalWS);
                float3 bitangentWS = normalize(cross(geometricNormalWS, tangentWS));

                float2 uvA = worldPos.xz * _NormalTilingA;
                uvA += _LargeWaveDirection.xy * (_RenderWaveTime * _NormalSpeedA);

                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMapA, sampler_NormalMapA, uvA), _NormalStrength);

                if (qualityMode > 0.5)
                {
                    float2 uvB = worldPos.xz * _NormalTilingB;
                    uvB += _SmallWaveDirection.xy * (_RenderWaveTime * _NormalSpeedB);

                    float strengthB = qualityMode > 1.5 ? _NormalStrength : _NormalStrength * 0.75;
                    float3 detailTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMapB, sampler_NormalMapB, uvB), strengthB);
                    normalTS.xy += detailTS.xy;
                    normalTS.z = saturate(normalTS.z * detailTS.z);
                    normalTS = normalize(normalTS);
                }

                float3x3 tbn = float3x3(tangentWS, bitangentWS, geometricNormalWS);
                return normalize(mul(normalTS, tbn));
            }

            float ComputeWaterDepth(float4 screenPos, float3 worldPos)
            {
                float2 screenUV = screenPos.xy / max(screenPos.w, 0.0001);
                float rawSceneDepth = SampleSceneDepth(screenUV);

                #if UNITY_REVERSED_Z
                    if (rawSceneDepth <= 0.00001)
                    {
                        return _DepthBlendDistance;
                    }
                #else
                    if (rawSceneDepth >= 0.99999)
                    {
                        return _DepthBlendDistance;
                    }
                #endif

                float sceneEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float waterEyeDepth = LinearEyeDepth(screenPos.z / max(screenPos.w, 0.0001), _ZBufferParams);
                return max(0.0, sceneEyeDepth - waterEyeDepth);
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 local = frac(uv);
                float2 smoothLocal = local * local * (3.0 - 2.0 * local);

                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));

                return lerp(lerp(a, b, smoothLocal.x), lerp(c, d, smoothLocal.x), smoothLocal.y);
            }

            float EvaluateContactFoam(float waterDepth, float3 worldPos, float qualityMode)
            {
                if (_ContactFoamEnabled < 0.5)
                {
                    return 0.0;
                }

                float foamDistance = max(_ContactFoamDistance, 0.001);
                float foamSoftness = max(_ContactFoamSoftness, 0.001);
                float fadeOut = 1.0 - smoothstep(foamDistance, foamDistance + foamSoftness, waterDepth);
                if (fadeOut <= 0.0001)
                {
                    return 0.0;
                }

                float contact01 = saturate(1.0 - (waterDepth / foamDistance));
                float bodyMask = contact01 * contact01;

                float rimWidth = min(foamDistance * 0.35 + foamSoftness * 0.5, foamDistance);
                rimWidth = max(rimWidth, foamSoftness * 0.5);
                float rimMask = 1.0 - smoothstep(0.0, rimWidth, waterDepth);

                float depthGradient = length(float2(ddx(waterDepth), ddy(waterDepth)));
                float edgeResponse = saturate(depthGradient * _ContactFoamEdgeSensitivity);
                edgeResponse = smoothstep(0.03, 0.8, edgeResponse);

                // The rim anchors the foam to the actual contact edge. The body only
                // survives when there is enough depth variation, which avoids broad
                // "foam everywhere in shallow water" behavior.
                float contactMask = max(rimMask, bodyMask * edgeResponse) * fadeOut;

                float breakup = 1.0;
                if (_ContactFoamBreakupStrength > 0.001)
                {
                    float2 noiseUv = worldPos.xz * _ContactFoamBreakupScale;
                    noiseUv += _LargeWaveDirection.xy * (_RenderWaveTime * 0.05);
                    noiseUv += _SmallWaveDirection.xy * (_RenderWaveTime * 0.08);

                    float noiseA = ValueNoise(noiseUv);
                    float noiseB = ValueNoise((noiseUv * 1.93) + float2(8.1, 3.7));
                    float combinedNoise = lerp(noiseA, noiseB, qualityMode > 1.5 ? 0.45 : 0.3);
                    float breakupMask = saturate(0.72 + combinedNoise * 0.5);
                    float breakupInfluence = _ContactFoamBreakupStrength * lerp(0.25, 1.0, bodyMask);
                    breakup = lerp(1.0, breakupMask, breakupInfluence);
                }

                return saturate(contactMask * breakup * _ContactFoamIntensity);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float2 gradient;
                worldPos.y += SampleWaveHeight(worldPos.xz, gradient);

                output.geometricNormalWS = normalize(half3(-gradient.x, 1.0, -gradient.y));
                output.positionCS = TransformWorldToHClip(worldPos);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.worldPos = worldPos;
                output.viewDirWS = SafeNormalize(GetCameraPositionWS() - worldPos);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                // Zone clipping applies to both faces for consistent waterzone masking.
                if (_ZoneClipEnabled > 0.5)
                {
                    float2 clipDelta = abs(input.worldPos.xz - _ZoneClipCenter.xy);
                    float2 clipDistance = (_ZoneClipHalfExtents.xy + _ZoneBoundaryEpsilon) - clipDelta;
                    clip(min(clipDistance.x, clipDistance.y));
                }

                float qualityMode = _QualityMode;
                float visualMode = _VisualMode;

                // Sample the wave normal once — used by both the topside and underside
                // shading paths.  On the underside it drives the Snell's window shimmer.
                float3 geomNormal = normalize(float3(input.geometricNormalWS));
                float3 surfaceNormalWS = SampleDetailNormal(input.worldPos, geomNormal, qualityMode);

                Light mainLight = GetMainLight();
                float3 lightDirWS = normalize(mainLight.direction);
                float3 viewDirWS = normalize(float3(input.viewDirWS));

                // =========================================================
                // UNDERSIDE PATH
                // Renders when the camera is below the water surface.
                // _ForceUndersideDebug forces this path from above for
                // editor validation without needing to submerge the camera.
                // =========================================================
                bool renderUnderside = !isFrontFace || _ForceUndersideDebug > 0.5;
                if (renderUnderside)
                {
                    float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 0.0001);

                    // --- Optical window (soft, view-dependent) ---
                    // viewDirWS points surface → camera. Camera below → viewDirWS.y < 0.
                    // -viewDirWS.y: 1 = camera directly below, 0 = looking horizontally.
                    // Squared falloff produces a gentle, not harsh, directional window.
                    float viewUp = saturate(-viewDirWS.y);
                    float optWindow = viewUp * viewUp;

                    // --- Diffuse illumination from above ---
                    // N·L using the upward normal: how much sun energy hits the
                    // surface from above, and therefore transmits through.
                    float ndotl = saturate(dot(surfaceNormalWS, lightDirWS));
                    float sunHeight = saturate(dot(lightDirWS, float3(0, 1, 0)));
                    // Mix direct N·L with sun-height ambient so the underside still
                    // reads correctly when the sun is overhead (ndotl near 1.0).
                    float illumination = ndotl * 0.55 + sunHeight * 0.45;

                    // --- Wave silhouette readability ---
                    // Normal deviation from flat geometry drives visible crest/trough
                    // contrast. Widened range makes wave shape clear from below.
                    float waveAlignment = dot(surfaceNormalWS, geomNormal);
                    float waveMask = lerp(0.55, 1.0, saturate(waveAlignment));

                    // --- Normal-driven UV distortion (lightweight refraction proxy) ---
                    // The horizontal component of the wave normal (xz) encodes surface tilt.
                    // Offsetting the screen UV by this tilt simulates the bending of
                    // light paths through the water surface without a refraction pipeline.
                    float2 refractOffset = surfaceNormalWS.xz * _UndersideDistortionStrength;
                    float2 refractUV = clamp(screenUV + refractOffset, 0.001, 0.999);

                    // --- Refracted scene colour (URP Opaque Texture) ---
                    // SampleSceneColor reads _CameraOpaqueTexture, which contains the
                    // fully rendered above-water world (boats, sky, shore) at the point
                    // before transparent objects were drawn. This is the cheapest way
                    // to show above-water objects through the water surface.
                    // NOTE: "Opaque Texture" must be ON in the URP Render Pipeline Asset.
                    //       Set _UndersideRefractionBlend = 0 if that setting is off.
                    float3 sceneAbove = SampleSceneColor(refractUV);

                    // Tint the sampled scene with the underside water colour.
                    // The ×2 boost compensates for the dark multiply so the result
                    // stays readable even with a saturated tint colour.
                    float3 waterTint = _UndersideTintColor.rgb;
                    float3 tintedScene = sceneAbove * (waterTint * 2.0 + 0.08);

                    // --- Blend tinted scene with pure water tint ---
                    // optWindow gates how much of the distorted scene bleeds through:
                    // looking straight up → clearest view of above-water world.
                    // looking oblique → dominated by the water tint colour.
                    float sceneBlend = optWindow * _UndersideRefractionBlend;
                    float3 undersideColor = lerp(waterTint, tintedScene, sceneBlend);

                    // Apply diffuse illumination and wave silhouette.
                    undersideColor *= lerp(0.22, 1.0, illumination) * waveMask;

                    // Soft optical window lift: additive brightening at the center.
                    // Avoids the harsh dark-edge look of a strict TIR simulation
                    // while still giving the eye a view-dependent bright spot overhead.
                    undersideColor += waterTint * (0.08 + sunHeight * 0.06)
                                    * optWindow * _UndersideOpticalWindow;

                    // Specular: sun point visible from below through the surface.
                    // Visibility scales with the optical window — brighter when
                    // looking more directly upward.
                    float specUnder = pow(saturate(ndotl), 28.0);
                    undersideColor += _HighlightColor.rgb * specUnder * 0.28
                                    * (0.35 + optWindow * 0.65);

                    undersideColor = MixFog(undersideColor, input.fogFactor);
                    return half4(undersideColor, 1.0);
                }

                // =========================================================
                // TOPSIDE PATH — original front-face shading, unchanged.
                // =========================================================

                if (visualMode < 0.5)
                {
                    surfaceNormalWS = normalize(lerp(geomNormal, surfaceNormalWS,
                                                     qualityMode > 0.5 ? 0.35 : 0.2));
                }

                float waterDepth = ComputeWaterDepth(input.screenPos, input.worldPos);
                float depth01 = saturate((waterDepth / max(_DepthBlendDistance, 0.0001)) + _DepthBlendBias);
                float contactFoam = EvaluateContactFoam(waterDepth, input.worldPos, qualityMode);

                float3 halfVectorWS = normalize(lightDirWS + viewDirWS);

                float ndotl = saturate(dot(surfaceNormalWS, lightDirWS));
                float fresnel = pow(1.0 - saturate(dot(surfaceNormalWS, viewDirWS)), _FresnelPower) * _FresnelStrength;
                float specular = pow(saturate(dot(surfaceNormalWS, halfVectorWS)), _SpecularExponent) * _SpecularStrength;

                float variation = sin(dot(input.worldPos.xz, float2(0.021, 0.017)) + (_RenderWaveTime * 0.35));
                variation = variation * _VariationStrength;

                float3 shallowColor = _SurfaceColor.rgb;
                float3 deepColor = _DeepColor.rgb;

                float3 baseColor = lerp(shallowColor, deepColor, depth01);

                if (visualMode < 0.5)
                {
                    float classicLight = smoothstep(0.08, 0.9, ndotl);
                    classicLight = lerp(0.72, 1.0, classicLight);
                    baseColor *= classicLight * (0.9 + variation * 0.25);
                    baseColor = lerp(baseColor, shallowColor, fresnel * 0.35);
                }
                else
                {
                    float enhancedLight = lerp(0.3, 1.0, ndotl * _DiffuseStrength);
                    float depthAccent = qualityMode > 1.5 ? saturate(depth01 * 1.25) : depth01;
                    float3 highlight = _HighlightColor.rgb * (specular * (0.35 + fresnel));
                    baseColor *= enhancedLight;
                    baseColor = lerp(baseColor, shallowColor, fresnel);
                    baseColor += variation * lerp(0.08, 0.18, depthAccent);
                    baseColor += highlight * mainLight.color;
                }

                if (contactFoam > 0.0001)
                {
                    float foamLighting = lerp(0.85, 1.05, ndotl);
                    float foamFresnel = saturate(fresnel * 1.35);
                    float foamBlend = saturate(contactFoam * (visualMode < 0.5 ? 0.85 : 1.0));
                    float3 foamColor = _ContactFoamColor.rgb * foamLighting;
                    foamColor += _HighlightColor.rgb * foamFresnel * 0.2;
                    baseColor = lerp(baseColor, foamColor, foamBlend);
                }

                baseColor += fresnel * _HighlightColor.rgb * 0.15;
                baseColor = MixFog(baseColor, input.fogFactor);
                return half4(baseColor, 1.0);
            }
            ENDHLSL
        }
    }
}
