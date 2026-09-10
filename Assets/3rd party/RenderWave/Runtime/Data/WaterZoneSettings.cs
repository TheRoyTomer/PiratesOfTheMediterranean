using System;
using UnityEngine;

namespace RenderWave.Runtime.Data
{
    /// <summary>
    /// Authoritative runtime wave contract shared by CPU queries and shader parameter upload.
    /// </summary>
    public readonly struct WaterWaveParameters
    {
        public WaterWaveParameters(Vector2 largeDirection, Vector2 smallDirection, Vector4 largeWaveData, Vector4 smallWaveData)
        {
            LargeDirection = largeDirection;
            SmallDirection = smallDirection;
            LargeWaveData = largeWaveData;
            SmallWaveData = smallWaveData;
        }

        public Vector2 LargeDirection { get; }
        public Vector2 SmallDirection { get; }
        public Vector4 LargeWaveData { get; }
        public Vector4 SmallWaveData { get; }
        public float MaxVerticalDisplacement => Mathf.Abs(LargeWaveData.x) + Mathf.Abs(SmallWaveData.x);
        public float RecommendedMeshBoundsHeight => Mathf.Max(4f, (MaxVerticalDisplacement * 2f) + 4f);
    }

    /// <summary>
    /// Serialized runtime data shared by rendering and gameplay queries.
    /// </summary>
    [Serializable]
    public sealed class WaterZoneSettings
    {
        [SerializeField] private WaterZoneType zoneType = WaterZoneType.Ocean;
        [SerializeField] private float baseHeight;
        [SerializeField] private Vector2 flowDirection = new Vector2(1f, 0.15f);
        [SerializeField, Range(0f, 1.5f)] private float vertexDisplacementStrength = 1f;
        [SerializeField, Min(0f)] private float smallWaveIntensity = 0.15f;
        [SerializeField, Min(0.1f)] private float smallWaveLength = 6f;
        [SerializeField, Min(0f)] private float smallWaveSpeed = 1.3f;
        [SerializeField, Min(0f)] private float largeWaveIntensity = 0.65f;
        [SerializeField, Min(0.1f)] private float largeWaveLength = 42f;
        [SerializeField, Min(0f)] private float largeWaveSpeed = 0.35f;
        [SerializeField] private Color surfaceColor = new Color(0.08f, 0.42f, 0.55f, 1f);
        [SerializeField] private Color underwaterColor = new Color(0.02f, 0.16f, 0.22f, 1f);
        [SerializeField, Min(0f)] private float fogDensity = 0.025f;
        [SerializeField] private bool allowSwimming = true;
        [SerializeField] private bool underwaterEffectsEnabled = true;

        public WaterZoneType ZoneType => zoneType;
        public float BaseHeight => baseHeight;
        public Vector2 FlowDirection => flowDirection;
        public float VertexDisplacementStrength => vertexDisplacementStrength;
        public float SmallWaveIntensity => smallWaveIntensity;
        public float SmallWaveLength => smallWaveLength;
        public float SmallWaveSpeed => smallWaveSpeed;
        public float LargeWaveIntensity => largeWaveIntensity;
        public float LargeWaveLength => largeWaveLength;
        public float LargeWaveSpeed => largeWaveSpeed;
        public Color SurfaceColor => surfaceColor;
        public Color UnderwaterColor => underwaterColor;
        public float FogDensity => fogDensity;
        public bool AllowSwimming => allowSwimming;
        public bool UnderwaterEffectsEnabled => underwaterEffectsEnabled;
        public float MaxWaveDisplacement => (Mathf.Abs(smallWaveIntensity) + Mathf.Abs(largeWaveIntensity)) * vertexDisplacementStrength;

        public void Validate()
        {
            if (flowDirection.sqrMagnitude < 0.0001f)
            {
                flowDirection = Vector2.right;
            }

            vertexDisplacementStrength = Mathf.Clamp(vertexDisplacementStrength, 0f, 1.5f);
            smallWaveIntensity = Mathf.Max(0f, smallWaveIntensity);
            smallWaveLength = Mathf.Max(0.1f, smallWaveLength);
            smallWaveSpeed = Mathf.Max(0f, smallWaveSpeed);
            largeWaveIntensity = Mathf.Max(0f, largeWaveIntensity);
            largeWaveLength = Mathf.Max(0.1f, largeWaveLength);
            largeWaveSpeed = Mathf.Max(0f, largeWaveSpeed);
            fogDensity = Mathf.Max(0f, fogDensity);
        }
    }
}
