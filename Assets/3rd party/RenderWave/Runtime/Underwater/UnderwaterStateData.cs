using RenderWave.Runtime.Data;
using UnityEngine;

namespace RenderWave.Runtime.Underwater
{
    /// <summary>
    /// Stable underwater state payload exposed to gameplay and visuals.
    /// </summary>
    public readonly struct UnderwaterStateData
    {
        public UnderwaterStateData(
            bool hasWaterQuery,
            bool isUnderwater,
            bool visualsAllowed,
            float signedDistanceToSurface,
            float surfaceHeight,
            float transitionWeight,
            WaterQueryResult waterQuery)
        {
            HasWaterQuery = hasWaterQuery;
            IsUnderwater = isUnderwater;
            VisualsAllowed = visualsAllowed;
            SignedDistanceToSurface = signedDistanceToSurface;
            SurfaceHeight = surfaceHeight;
            TransitionWeight = transitionWeight;
            WaterQuery = waterQuery;
        }

        public bool HasWaterQuery { get; }
        public bool IsUnderwater { get; }
        public bool VisualsAllowed { get; }
        public float SignedDistanceToSurface { get; }
        public float SurfaceHeight { get; }
        public float TransitionWeight { get; }
        public WaterQueryResult WaterQuery { get; }
        public float DepthBelowSurface => Mathf.Max(0f, -SignedDistanceToSurface);
        public bool VisualsActive => VisualsAllowed && TransitionWeight > 0.0001f;
        public bool IsTransitioning => TransitionWeight > 0.0001f && TransitionWeight < 0.9999f;
    }
}
