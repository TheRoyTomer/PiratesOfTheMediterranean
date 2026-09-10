using UnityEngine;

namespace RenderWave.Runtime.Data
{
    /// <summary>
    /// Allocation-free result returned by the water query API.
    /// </summary>
    public readonly struct WaterQueryResult
    {
        public WaterQueryResult(
            bool isInsideZone,
            float surfaceHeight,
            float signedDistanceToSurface,
            Vector3 surfaceNormal,
            WaterZoneType zoneType,
            bool allowSwimming,
            bool underwaterEffectsEnabled,
            Color surfaceColor,
            Color underwaterColor,
            float fogDensity)
        {
            IsInsideZone = isInsideZone;
            SurfaceHeight = surfaceHeight;
            SignedDistanceToSurface = signedDistanceToSurface;
            SurfaceNormal = surfaceNormal;
            ZoneType = zoneType;
            AllowSwimming = allowSwimming;
            UnderwaterEffectsEnabled = underwaterEffectsEnabled;
            SurfaceColor = surfaceColor;
            UnderwaterColor = underwaterColor;
            FogDensity = fogDensity;
        }

        public bool IsInsideZone { get; }
        public float SurfaceHeight { get; }
        public float SignedDistanceToSurface { get; }
        public Vector3 SurfaceNormal { get; }
        public WaterZoneType ZoneType { get; }
        public bool AllowSwimming { get; }
        public bool UnderwaterEffectsEnabled { get; }
        public Color SurfaceColor { get; }
        public Color UnderwaterColor { get; }
        public float FogDensity { get; }
        public bool IsUnderwater => SignedDistanceToSurface < 0f;
        public bool IsAtOrBelowSurface => SignedDistanceToSurface <= 0f;
        public float DepthBelowSurface => Mathf.Max(0f, -SignedDistanceToSurface);
    }
}
