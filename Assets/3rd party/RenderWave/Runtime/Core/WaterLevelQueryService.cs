using RenderWave.Runtime.Data;
using UnityEngine;

namespace RenderWave.Runtime.Core
{
    /// <summary>
    /// Stable entry point for gameplay systems that need water information.
    /// </summary>
    public static class WaterLevelQueryService
    {
        public static bool TryGetWaterQuery(Vector3 worldPosition, out WaterQueryResult result)
        {
            return TryGetWaterQuery(worldPosition, Application.isPlaying ? Time.time : 0f, out result);
        }

        public static bool TryGetWaterQuery(Vector3 worldPosition, float time, out WaterQueryResult result)
        {
            return WaterZoneRegistry.TryQuery(worldPosition, time, out result);
        }

        public static bool TryGetSurfaceHeight(Vector3 worldPosition, out float surfaceHeight)
        {
            if (TryGetWaterQuery(worldPosition, out var result))
            {
                surfaceHeight = result.SurfaceHeight;
                return true;
            }

            surfaceHeight = 0f;
            return false;
        }

        public static bool IsUnderwater(Vector3 worldPosition)
        {
            return TryGetWaterQuery(worldPosition, out var result) && result.IsUnderwater;
        }

        public static bool TryGetSignedDistanceToSurface(Vector3 worldPosition, out float signedDistanceToSurface)
        {
            if (TryGetWaterQuery(worldPosition, out var result))
            {
                signedDistanceToSurface = result.SignedDistanceToSurface;
                return true;
            }

            signedDistanceToSurface = 0f;
            return false;
        }
    }
}
