using RenderWave.Runtime.Data;
using UnityEngine;

namespace RenderWave.Runtime.Utilities
{
    /// <summary>
    /// Shared CPU wave sampling used by both the query system and shader parameter setup.
    /// </summary>
    public static class WaterWaveMath
    {
        private const float Tau = Mathf.PI * 2f;

        public static void BuildWaveDirections(Vector2 flowDirection, out Vector2 largeDirection, out Vector2 smallDirection)
        {
            largeDirection = flowDirection.sqrMagnitude > 0.0001f ? flowDirection.normalized : Vector2.right;
            smallDirection = new Vector2(-largeDirection.y, largeDirection.x);
        }

        public static WaterWaveParameters BuildParameters(WaterZoneSettings settings)
        {
            BuildWaveDirections(settings.FlowDirection, out var largeDirection, out var smallDirection);

            var largeWaveData = new Vector4(
                settings.LargeWaveIntensity * settings.VertexDisplacementStrength,
                Tau / settings.LargeWaveLength,
                settings.LargeWaveSpeed,
                0f);

            var smallWaveData = new Vector4(
                settings.SmallWaveIntensity * settings.VertexDisplacementStrength,
                Tau / settings.SmallWaveLength,
                settings.SmallWaveSpeed,
                0f);

            return new WaterWaveParameters(largeDirection, smallDirection, largeWaveData, smallWaveData);
        }

        public static float SampleHeightOffset(Vector3 worldPosition, WaterWaveParameters parameters, float time)
        {
            var worldXZ = new Vector2(worldPosition.x, worldPosition.z);

            var largePhase = Vector2.Dot(worldXZ, parameters.LargeDirection) * parameters.LargeWaveData.y;
            largePhase += time * parameters.LargeWaveData.z;

            var smallPhase = Vector2.Dot(worldXZ, parameters.SmallDirection) * parameters.SmallWaveData.y;
            smallPhase += time * parameters.SmallWaveData.z;

            return (Mathf.Sin(largePhase) * parameters.LargeWaveData.x) + (Mathf.Sin(smallPhase) * parameters.SmallWaveData.x);
        }

        public static Vector3 SampleNormal(Vector3 worldPosition, WaterWaveParameters parameters, float time)
        {
            var worldXZ = new Vector2(worldPosition.x, worldPosition.z);

            var largePhase = Vector2.Dot(worldXZ, parameters.LargeDirection) * parameters.LargeWaveData.y;
            largePhase += time * parameters.LargeWaveData.z;

            var smallPhase = Vector2.Dot(worldXZ, parameters.SmallDirection) * parameters.SmallWaveData.y;
            smallPhase += time * parameters.SmallWaveData.z;

            var dhdx = Mathf.Cos(largePhase) * parameters.LargeWaveData.x * parameters.LargeWaveData.y * parameters.LargeDirection.x;
            dhdx += Mathf.Cos(smallPhase) * parameters.SmallWaveData.x * parameters.SmallWaveData.y * parameters.SmallDirection.x;

            var dhdz = Mathf.Cos(largePhase) * parameters.LargeWaveData.x * parameters.LargeWaveData.y * parameters.LargeDirection.y;
            dhdz += Mathf.Cos(smallPhase) * parameters.SmallWaveData.x * parameters.SmallWaveData.y * parameters.SmallDirection.y;

            return new Vector3(-dhdx, 1f, -dhdz).normalized;
        }
    }
}
