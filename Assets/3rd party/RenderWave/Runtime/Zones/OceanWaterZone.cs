using RenderWave.Runtime.Core;
using RenderWave.Runtime.Data;
using RenderWave.Runtime.Utilities;
using UnityEngine;

namespace RenderWave.Runtime.Zones
{
    /// <summary>
    /// Zone implementation for RenderWave water coverage and queries.
    /// Supports ocean and contained-water profiles on the same core runtime.
    /// </summary>
    [AddComponentMenu("RenderWave/Ocean Water Zone")]
    [DisallowMultipleComponent]
    public sealed class OceanWaterZone : MonoBehaviour, IWaterZone
    {
        private const float CoverageEpsilon = 0.001f;

        [SerializeField] private int priority;
        [SerializeField] private OceanZoneCoverageMode coverageMode = OceanZoneCoverageMode.Infinite;
        [SerializeField] private Vector2 rectangleSize = new Vector2(4096f, 4096f);
        [SerializeField] private WaterZoneSettings settings = new WaterZoneSettings();

        private WaterWaveParameters waveParameters;

        public int Priority => priority;
        public OceanZoneCoverageMode CoverageMode => coverageMode;
        public Vector2 RectangleSize => rectangleSize;
        public WaterZoneSettings Settings => settings;
        public WaterWaveParameters WaveParameters => waveParameters;
        public Vector3 Center => transform.position;
        public float MaxVerticalDisplacement => waveParameters.MaxVerticalDisplacement;
        public float CoveragePadding => CoverageEpsilon;

        private void Reset()
        {
            ValidateData();
        }

        private void OnEnable()
        {
            ValidateData();
            WaterZoneRegistry.Register(this);
        }

        private void OnDisable()
        {
            WaterZoneRegistry.Unregister(this);
        }

        private void OnValidate()
        {
            ValidateData();
        }

        public bool ContainsHorizontalPosition(Vector3 worldPosition)
        {
            if (coverageMode == OceanZoneCoverageMode.Infinite)
            {
                return true;
            }

            var halfExtents = (rectangleSize * 0.5f) + new Vector2(CoverageEpsilon, CoverageEpsilon);
            var localOffset = worldPosition - transform.position;

            return Mathf.Abs(localOffset.x) <= halfExtents.x && Mathf.Abs(localOffset.z) <= halfExtents.y;
        }

        public bool IntersectsChunk(float worldCenterX, float worldCenterZ, float chunkSize)
        {
            if (coverageMode == OceanZoneCoverageMode.Infinite)
            {
                return true;
            }

            var halfZone = (rectangleSize * 0.5f) + new Vector2(CoverageEpsilon, CoverageEpsilon);
            var halfChunk = chunkSize * 0.5f;
            var zoneCenter = transform.position;

            var deltaX = Mathf.Abs(worldCenterX - zoneCenter.x);
            var deltaZ = Mathf.Abs(worldCenterZ - zoneCenter.z);

            return deltaX <= halfZone.x + halfChunk && deltaZ <= halfZone.y + halfChunk;
        }

        public float GetSurfaceHeight(Vector3 worldPosition, float time)
        {
            return settings.BaseHeight + WaterWaveMath.SampleHeightOffset(worldPosition, waveParameters, time);
        }

        public Vector3 GetSurfaceNormal(Vector3 worldPosition, float time)
        {
            return WaterWaveMath.SampleNormal(worldPosition, waveParameters, time);
        }

        public bool TryQuery(Vector3 worldPosition, float time, out WaterQueryResult result)
        {
            if (!ContainsHorizontalPosition(worldPosition))
            {
                result = default;
                return false;
            }

            var surfaceHeight = GetSurfaceHeight(worldPosition, time);
            var signedDistanceToSurface = worldPosition.y - surfaceHeight;
            result = new WaterQueryResult(
                true,
                surfaceHeight,
                signedDistanceToSurface,
                GetSurfaceNormal(worldPosition, time),
                settings.ZoneType,
                settings.AllowSwimming,
                settings.UnderwaterEffectsEnabled,
                settings.SurfaceColor,
                settings.UnderwaterColor,
                settings.FogDensity);

            return true;
        }

        private void ValidateData()
        {
            if (settings == null)
            {
                settings = new WaterZoneSettings();
            }

            rectangleSize.x = Mathf.Max(1f, rectangleSize.x);
            rectangleSize.y = Mathf.Max(1f, rectangleSize.y);
            settings.Validate();
            waveParameters = WaterWaveMath.BuildParameters(settings);
        }
    }
}
