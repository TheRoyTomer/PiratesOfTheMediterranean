using System;
using UnityEngine;

namespace RenderWave.Runtime.Underwater
{
    /// <summary>
    /// Inspector-facing visual controls for the lightweight underwater overlay.
    /// </summary>
    [Serializable]
    public sealed class UnderwaterVisualSettings
    {
        [SerializeField] private bool visualsEnabled = true;
        [SerializeField] private Color tintColor = new Color(0.08f, 0.34f, 0.4f, 1f);
        [SerializeField, Min(0.25f)] private float visibilityDistance = 18f;
        [SerializeField, Range(0f, 1f)] private float maxOpacity = 0.7f;
        [SerializeField, Range(0f, 2f)] private float tintStrength = 1f;
        [SerializeField, Range(0f, 1f)] private float nearSurfaceLight = 0.15f;
        [SerializeField] private bool enhancedWaterline;
        [SerializeField, Range(0.001f, 0.15f)] private float waterlineEdgeSoftness = 0.035f;
        [SerializeField, Range(0f, 1f)] private float waterlineHighlightStrength = 0.25f;
        [SerializeField, Range(0.01f, 0.25f)] private float waterlineBandThickness = 0.06f;
        [SerializeField, Range(1f, 25f)] private float waterlineNoiseScale = 8f;
        [SerializeField, Range(0f, 0.02f)] private float distortionStrength = 0.008f;
        [SerializeField, Range(0f, 0.5f)] private float underwaterVignetteStrength = 0.15f;

        [SerializeField] private bool causticsEnabled;
        [SerializeField] private Texture2D causticsTexture;
        [SerializeField] private Color causticColor = new Color(0.72f, 0.93f, 0.88f, 1f);
        [SerializeField, Range(0f, 1f)] private float causticIntensity = 0.2f;
        [SerializeField, Min(0.1f)] private float causticScale = 1.5f;
        [SerializeField] private Vector2 causticSpeed = new Vector2(0.06f, 0.08f);

        [SerializeField, Range(0f, 3f)] private float absorptionStrength = 1f;
        [SerializeField] private Color deepScatterColor = new Color(0.01f, 0.08f, 0.22f, 1f);
        [SerializeField, Range(2f, 40f)] private float depthColorShiftRange = 14f;
        [SerializeField, Range(0f, 4f)] private float depthOcclusionStrength = 1.5f;
        [SerializeField, Range(0f, 2.5f)] private float abyssStrength = 1.15f;
        [SerializeField, Range(0.35f, 0.85f)] private float ambientTransmissionFloor = 0.55f;

        public bool VisualsEnabled => visualsEnabled;
        public Color TintColor => tintColor;
        public float VisibilityDistance => visibilityDistance;
        public float MaxOpacity => maxOpacity;
        public float TintStrength => tintStrength;
        public float NearSurfaceLight => nearSurfaceLight;
        public bool EnhancedModeEnabled => enhancedWaterline;
        public bool EnhancedWaterline => enhancedWaterline;
        public float WaterlineEdgeSoftness => waterlineEdgeSoftness;
        public float WaterlineHighlightStrength => waterlineHighlightStrength;
        public float WaterlineBandThickness => waterlineBandThickness;
        public float WaterlineNoiseScale => waterlineNoiseScale;
        public float DistortionStrength => distortionStrength;
        public float UnderwaterVignetteStrength => underwaterVignetteStrength;
        public bool CausticsEnabled => causticsEnabled;
        public Texture2D CausticsTexture => causticsTexture;
        public Color CausticColor => causticColor;
        public float CausticIntensity => causticIntensity;
        public float CausticScale => causticScale;
        public Vector2 CausticSpeed => causticSpeed;
        public float AbsorptionStrength => absorptionStrength;
        public Color DeepScatterColor => deepScatterColor;
        public float DepthColorShiftRange => depthColorShiftRange;
        public float DepthOcclusionStrength => depthOcclusionStrength;
        public float AbyssStrength => abyssStrength;
        public float AmbientTransmissionFloor => ambientTransmissionFloor;

        public void Validate()
        {
            visibilityDistance = Mathf.Max(0.25f, visibilityDistance);
            maxOpacity = Mathf.Clamp01(maxOpacity);
            tintStrength = Mathf.Clamp(tintStrength, 0f, 2f);
            nearSurfaceLight = Mathf.Clamp01(nearSurfaceLight);
            causticIntensity = Mathf.Clamp01(causticIntensity);
            causticScale = Mathf.Max(0.1f, causticScale);
            absorptionStrength = Mathf.Clamp(absorptionStrength, 0f, 3f);
            depthColorShiftRange = Mathf.Clamp(depthColorShiftRange, 2f, 40f);
            depthOcclusionStrength = Mathf.Clamp(depthOcclusionStrength, 0f, 4f);
            abyssStrength = Mathf.Clamp(abyssStrength, 0f, 2.5f);
            ambientTransmissionFloor = Mathf.Clamp(ambientTransmissionFloor, 0.35f, 0.85f);
            waterlineEdgeSoftness = Mathf.Clamp(waterlineEdgeSoftness, 0.001f, 0.15f);
            waterlineHighlightStrength = Mathf.Clamp01(waterlineHighlightStrength);
            waterlineBandThickness = Mathf.Clamp(waterlineBandThickness, 0.01f, 0.25f);
            waterlineNoiseScale = Mathf.Clamp(waterlineNoiseScale, 1f, 25f);
            distortionStrength = Mathf.Clamp(distortionStrength, 0f, 0.02f);
            underwaterVignetteStrength = Mathf.Clamp(underwaterVignetteStrength, 0f, 0.5f);
        }
    }
}
