Shader "RenderWave/URP/UnderwaterOverlay"
{
    Properties
    {
        [Header(TintGroup)]
        _TintColor("Tint Color", Color) = (0.08, 0.34, 0.4, 1.0)
        _VisibilityDistance("Visibility Distance", Range(0.25, 100.0)) = 18.0
        _MaxOpacity("Max Opacity", Range(0.0, 1.0)) = 0.7
        _TintStrength("Tint Strength", Range(0.0, 2.0)) = 1.0
        _NearSurfaceLight("Near Surface Light", Range(0.0, 1.0)) = 0.15

        [Header(StateGroup)]
        _TransitionWeight("Transition Weight", Range(0.0, 1.0)) = 0.0
        _SignedDistanceToSurface("Signed Distance", Float) = 0.0
        _DepthBelowSurface("Depth Below Surface", Float) = 0.0
        _EnhancedModeEnabled("Enhanced Mode Enabled", Float) = 0.0

        [Header(WaterlineGroup)]
        _WaterlineEnabled("Waterline Enabled", Float) = 0
        _WaterlineLeftY("Waterline Left Y", Float) = 0.5
        _WaterlineCenterY("Waterline Center Y", Float) = 0.5
        _WaterlineRightY("Waterline Right Y", Float) = 0.5
        _WaterlineEdgeSoftness("Waterline Edge Softness", Range(0.001, 0.15)) = 0.035
        _WaterlineHighlightStrength("Waterline Highlight", Range(0.0, 1.0)) = 0.25
        _WaterlineBandThickness("Waterline Band Thickness", Range(0.01, 0.25)) = 0.06
        _WaterlineNoiseScale("Waterline Noise Scale", Range(1.0, 25.0)) = 8.0

        [Header(EnhancedUnderwaterGroup)]
        _DistortionStrength("Distortion Strength", Range(0.0, 0.02)) = 0.008
        _VignetteStrength("Vignette Strength", Range(0.0, 0.5)) = 0.15

        [Header(CausticsGroup)]
        [NoScaleOffset] _CausticsTex("Caustics Texture", 2D) = "white" {}
        _CausticsEnabled("Caustics Enabled", Float) = 0
        _CausticColor("Caustic Color", Color) = (0.72, 0.93, 0.88, 1.0)
        _CausticIntensity("Caustic Intensity", Range(0.0, 1.0)) = 0.2
        _CausticScale("Caustic Scale", Range(0.1, 8.0)) = 1.5
        _CausticSpeed("Caustic Speed", Vector) = (0.06, 0.08, 0, 0)

        [Header(DepthAbsorptionGroup)]
        // Controls how fog density and tint shift as the camera descends.
        // AbsorptionStrength: how fast fog density grows as the camera descends (multiplicative on depthDensity).
        // DeepScatterColor: the colour the tint transitions toward at depth (blue-dominant).
        // ColorShiftRange: camera depth in metres at which the tint fully shifts to DeepScatterColor.
        // DepthOcclusionStrength: non-linear power applied to the fog curve at depth.
        //   0 = standard exponential (same as topside fog).
        //   1.5 = recommended: near geometry stays clear, mid/far collapses into murk quickly.
        //   3+ = extremely murky — anything past 5m underwater nearly invisible.
        _AbsorptionStrength("Absorption Strength", Range(0.0, 3.0)) = 1.0
        _DeepScatterColor("Deep Scatter Color", Color) = (0.01, 0.08, 0.22, 1.0)
        _DepthColorShiftRange("Color Shift Range (m)", Range(2.0, 40.0)) = 14.0
        _DepthOcclusionStrength("Depth Occlusion Strength", Range(0.0, 4.0)) = 1.5
        _AbyssStrength("Abyss Strength", Range(0.0, 2.5)) = 1.15
        _AmbientTransmissionFloor("Ambient Transmission Floor", Range(0.35, 0.85)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Overlay"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_CausticsTex);
            SAMPLER(sampler_CausticsTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
                float _VisibilityDistance;
                float _MaxOpacity;
                float _TintStrength;
                float _NearSurfaceLight;
                float _TransitionWeight;
                float _SignedDistanceToSurface;
                float _DepthBelowSurface;
                float _EnhancedModeEnabled;
                float _WaterlineEnabled;
                float _WaterlineLeftY;
                float _WaterlineCenterY;
                float _WaterlineRightY;
                float _WaterlineEdgeSoftness;
                float _WaterlineHighlightStrength;
                float _WaterlineBandThickness;
                float _WaterlineNoiseScale;
                float _DistortionStrength;
                float _VignetteStrength;
                float _CausticsEnabled;
                half4 _CausticColor;
                float _CausticIntensity;
                float _CausticScale;
                float4 _CausticSpeed;
                float _AbsorptionStrength;
                half4 _DeepScatterColor;
                float _DepthColorShiftRange;
                float _DepthOcclusionStrength;
                float _AbyssStrength;
                float _AmbientTransmissionFloor;
            CBUFFER_END

            float SampleSceneEyeDepth(float2 screenUV, out float hasSceneDepthHit)
            {
                float rawDepth = SampleSceneDepth(screenUV);

                #if UNITY_REVERSED_Z
                    if (rawDepth <= 0.00001)
                    {
                        hasSceneDepthHit = 0.0;
                        return _VisibilityDistance;
                    }
                #else
                    if (rawDepth >= 0.99999)
                    {
                        hasSceneDepthHit = 0.0;
                        return _VisibilityDistance;
                    }
                #endif

                hasSceneDepthHit = 1.0;
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(worldPos);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.uv = input.uv;
                output.worldPos = worldPos;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 0.0001);
                float transition = saturate(_TransitionWeight);
                float enhancedMode = saturate(_EnhancedModeEnabled);

                // --- Enhanced waterline: compute distorted boundary before depth sampling ---
                // The waterline mask and decorations are computed first so that edgeDist
                // and noise remain consistent between the mask and the highlight passes.
                float waterlineMask = 1.0;
                float waterlineHighlight = 0.0;
                float waterlineFoam = 0.0;

                if (enhancedMode > 0.5 && _WaterlineEnabled > 0.5)
                {
                    // Quadratic Bézier curve through three projected viewport-Y samples
                    // gives a gently curved horizon that tracks actual wave shape.
                    float invX = 1.0 - screenUV.x;
                    float baseWaterlineY = (invX * invX * _WaterlineLeftY)
                                        + (2.0 * invX * screenUV.x * _WaterlineCenterY)
                                        + (screenUV.x * screenUV.x * _WaterlineRightY);

                    // Three-octave sine noise — irrational frequency ratios prevent
                    // visible repetition across screen width.
                    float t = _Time.y;
                    float nx = screenUV.x * _WaterlineNoiseScale;
                    float noise = sin(nx * 6.2831 + t * 1.8) * 0.45
                                + sin(nx * 2.37 * 6.2831 - t * 2.5) * 0.35
                                + sin(nx * 0.43 * 6.2831 + t * 0.9) * 0.20;

                    float bandHalf = _WaterlineBandThickness * 0.5;
                    float softness = max(_WaterlineEdgeSoftness, 0.001);
                    float outerEdge = bandHalf + softness;
                    float waterlineY = baseWaterlineY + noise * _WaterlineBandThickness * 0.5;

                    // Smooth 0→1 mask: 1 below waterline, 0 above.
                    waterlineMask = smoothstep(waterlineY + outerEdge, waterlineY - outerEdge, screenUV.y);

                    // Highlight: blend of a broad diffuse glow and a sharp Gaussian
                    // peak for a readable bright edge that follows the organic contour.
                    float edgeDist = abs(screenUV.y - waterlineY);
                    float broadGlow = 1.0 - smoothstep(0.0, outerEdge * 1.5, edgeDist);
                    float sharpPeak = exp(-edgeDist * edgeDist /
                                          max(softness * softness * 0.4, 0.00001));
                    waterlineHighlight = lerp(broadGlow, sharpPeak, 0.5)
                                       * _WaterlineHighlightStrength * transition;

                    // Foam stipple: hash-based dots break the smooth gradient
                    // and suggest surface foam/froth at the crossing zone.
                    float hashSeed = frac(sin(nx * 127.1 + floor(t * 3.0) * 43.7) * 43758.5453);
                    float foamBand = smoothstep(outerEdge, outerEdge * 0.15, edgeDist);
                    waterlineFoam = foamBand * step(0.62, hashSeed)
                                  * _WaterlineHighlightStrength * 0.35 * transition;
                }

                // --- Screen-space distortion (underwater shimmer) ---
                // Animated sine offsets on the depth-sample UV create a refractive
                // shimmer: the tint varies with distorted geometry reads, not with
                // a separate texture.  Strength scales with transition so it fades
                // in/out alongside the rest of the overlay.
                float2 sampleUV = screenUV;
                if (enhancedMode > 0.5 && _DistortionStrength > 0.0001)
                {
                    float t = _Time.y;
                    sampleUV.x += sin(screenUV.y * 30.0 + t * 1.4) * _DistortionStrength * transition;
                    sampleUV.y += cos(screenUV.x * 25.0 + t * 1.1) * _DistortionStrength * 0.7 * transition;
                    sampleUV = clamp(sampleUV, 0.001, 0.999);
                }

                float sceneDepthHit;
                float sceneEyeDepth = SampleSceneEyeDepth(sampleUV, sceneDepthHit);
                float3 viewRayWS = SafeNormalize(input.worldPos - GetCameraPositionWS());
                float upwardView = saturate(viewRayWS.y);
                float downwardView = saturate(-viewRayWS.y);

                // --- Depth-driven fog density ---
                // depthDensity increases with camera submersion depth, compressing the
                // effective absorption length as the camera descends.
                float depthBelow = max(0.0, _DepthBelowSurface);
                float depthT = saturate(depthBelow / max(_DepthColorShiftRange, 0.001));
                float depthDensity = 1.0 + (depthT * _AbsorptionStrength * 0.75);
                float abyssDepth = smoothstep(0.15, 0.9, depthT);
                float downwardAbyss = downwardView * downwardView * abyssDepth;
                float openWaterAbyss = downwardAbyss * (1.0 - sceneDepthHit);
                float abyssPseudoDepth = _VisibilityDistance * (1.0 + _AbyssStrength * (0.85 + depthT * 1.15));
                sceneEyeDepth = lerp(sceneEyeDepth, abyssPseudoDepth, openWaterAbyss);

                // --- Non-linear scene depth attenuation (stretched-exponential fog) ---
                // _VisibilityDistance is treated as the practical readable range, not the
                // 1/e absorption length. The 0.72 factor keeps that range meaningfully murky
                // (~75% occlusion at visibilityDistance before additional shaping) instead of
                // reading as a weak exponential fog. absorptionLength shrinks further as the
                // camera descends via depthDensity.
                float absorptionLength = (_VisibilityDistance * 0.72) / max(depthDensity, 0.5);
                float abyssExtinctionMultiplier = 1.0 + _AbyssStrength
                                                      * ((downwardAbyss * 0.4) + (openWaterAbyss * 0.6));
                float fogArg = min((sceneEyeDepth / max(absorptionLength, 0.1)) * abyssExtinctionMultiplier, 10.0);

                // Stretched-exponential (Weibull) fog: power > 1 keeps near-camera
                // geometry clear while causing a steeper collapse at medium/far distances.
                // Power stays near 1.0 close to the surface, rises with camera submersion,
                // and gets an extra bump when the camera looks downward into deeper space.
                //   fogPower 1.0  — linear falloff, gentle.
                //   fogPower 2.5  — nearby clear, mid foggy, far black.
                //   fogPower 4.0  — only objects within ~3 m survive past murk.
                float fogPower = 1.0 + (_DepthOcclusionStrength * 0.75
                                        * smoothstep(0.0, 1.0, depthT))
                                      + (_AbyssStrength * downwardAbyss * 0.35);
                float depthFactor = 1.0 - exp(-pow(fogArg, fogPower));
                float surfaceTransmission = upwardView
                                          * saturate(1.0 - depthBelow
                                                         / max(_DepthColorShiftRange * 1.35, 0.001));
                depthFactor *= lerp(1.0, 0.82, surfaceTransmission);

                // --- Depth-driven minimum opacity ---
                // Even nearby objects carry a base tint at depth, simulating ambient
                // back-scatter from the water column above the camera.
                // Grows to 30% at depthColorShiftRange × 1.5 metres depth.
                float depthBaseOpacity = pow(depthT, 1.25) * 0.18 * _TintStrength;
                float alpha = saturate(saturate(depthFactor * _TintStrength) * _MaxOpacity
                                       + depthBaseOpacity) * transition;

                // Waterline masks tint alpha: pixels above the projected water surface
                // receive no overlay tint while the camera crosses the boundary.
                if (enhancedMode > 0.5 && _WaterlineEnabled > 0.5)
                {
                    alpha *= waterlineMask;
                }

                // --- Depth-driven colour shift ---
                // Water selectively absorbs wavelengths: red absorbed first, then green,
                // blue penetrates deepest. Tint transitions from the surface hue to
                // _DeepScatterColor (dark blue) as the camera descends.
                // Squared depthT gives a slow start followed by accelerating shift.
                float3 tint = lerp(_TintColor.rgb, _DeepScatterColor.rgb, depthT * depthT);

                // Near-surface brightening: meaningful only in the first few metres.
                float nearSurfaceFactor = saturate(1.0 - (depthBelow / max(_VisibilityDistance, 0.0001)));
                tint *= lerp(1.0, 1.0 + _NearSurfaceLight, nearSurfaceFactor);

                // --- Ambient depth darkening ---
                // Simulates cumulative light loss through the water column above the camera.
                // Independent of per-pixel scene depth — applies even to nearby geometry.
                // Reaches 65% darkening at depthColorShiftRange × 2.5 metres.
                float lightLoss = saturate(depthBelow / max(_DepthColorShiftRange * 3.0, 1.0));
                tint *= lerp(1.0, _AmbientTransmissionFloor, lightLoss);

                // Downward abyss tint: looking into lower space should feel colder and deeper
                // than looking across or upward, especially when the depth buffer has no bottom hit.
                float3 abyssColor = _DeepScatterColor.rgb * float3(0.7, 0.8, 1.05);
                float abyssTint = saturate(_AbyssStrength * ((downwardAbyss * 0.22) + (openWaterAbyss * 0.3)));
                tint = lerp(tint, abyssColor, abyssTint);

                // --- Underwater vignette (volume absorption at oblique angles) ---
                // Screen-edge darkening simulates the increased water path length
                // at peripheral viewing angles.  Adds a subtle dark ring that makes
                // the full-screen tint feel volumetric rather than flat.
                if (enhancedMode > 0.5 && _VignetteStrength > 0.0001)
                {
                    float2 vigUV = screenUV - 0.5;
                    float vigDist = dot(vigUV, vigUV);
                    float vignetteMask = smoothstep(0.15, 0.85, vigDist);
                    alpha = saturate(alpha + vignetteMask * _VignetteStrength * transition);
                    tint *= lerp(1.0, 0.65, vignetteMask * _VignetteStrength);
                }

                // --- Upward ceiling glow ---
                // Brightens the tint toward the upper screen region when looking up,
                // giving the characteristic bright-ceiling sensation of underwater views.
                if (enhancedMode > 0.5)
                {
                    float ceilingMask = upwardView * upwardView;
                    float ceilingStrength = nearSurfaceFactor * ceilingMask * transition;
                    tint += _TintColor.rgb * (0.15 + _NearSurfaceLight * 1.2) * ceilingStrength;
                }

                // --- Caustics ---
                float3 caustics = 0.0;
                if (_CausticsEnabled > 0.5 && _CausticIntensity > 0.0001)
                {
                    float2 causticUV = (screenUV * _CausticScale) + (_CausticSpeed.xy * _CausticSpeed.z);
                    float causticSampleA = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, causticUV).r;
                    float causticSampleB = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex,
                                           (causticUV * 1.7) + float2(0.17, 0.23)).g;
                    float causticMask = saturate((causticSampleA + causticSampleB) - 0.85);
                    caustics = _CausticColor.rgb * causticMask * _CausticIntensity
                             * nearSurfaceFactor * transition;
                }

                // Waterline decorations applied last so they sit on top of tint.
                if (enhancedMode > 0.5 && _WaterlineEnabled > 0.5)
                {
                    tint += waterlineHighlight;
                    tint += waterlineFoam;
                }

                return half4(tint + caustics, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
