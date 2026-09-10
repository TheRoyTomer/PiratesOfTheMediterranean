using RenderWave.Runtime.Core;
using RenderWave.Runtime.Data;
using UnityEngine;

namespace RenderWave.Runtime.Buoyancy
{
    /// <summary>
    /// Lightweight buoyancy component.
    /// Applies vertical forces so a Rigidbody floats on the RenderWave water surface.
    /// This component is fully optional and has no dependencies on OceanManager or any
    /// internal RenderWave system - it talks exclusively to WaterLevelQueryService.
    /// </summary>
    [AddComponentMenu("RenderWave/Buoyancy Body")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BuoyancyBody : MonoBehaviour
    {
        [SerializeField]
        private BuoyancySettings settings = new BuoyancySettings();

        private const float AlignmentMaxAngleDegrees = 45f;
        private static readonly Vector2[] FourPointSigns =
        {
            new Vector2(-1f, -1f),
            new Vector2(-1f, 1f),
            new Vector2(1f, -1f),
            new Vector2(1f, 1f)
        };

        private Rigidbody body;
        private float activationTime;
        private bool hasWater;
        private int lastActiveSampleCount;
        private float lastSurfaceHeight;
        private float lastSignedDistance;
        private Vector3 lastSurfaceNormal;

        /// <summary>Current buoyancy settings. Never null at runtime.</summary>
        public BuoyancySettings Settings => settings;

        /// <summary>True if the last FixedUpdate found valid water at one or more sample positions.</summary>
        public bool HasWater => hasWater;

        /// <summary>Number of sample points that found valid water on the last FixedUpdate.</summary>
        public int LastActiveSampleCount => lastActiveSampleCount;

        /// <summary>Water surface height from the last successful query set.</summary>
        public float LastSurfaceHeight => lastSurfaceHeight;

        /// <summary>Signed distance to surface from the last successful query set.
        /// Positive = above water, negative = below water.</summary>
        public float LastSignedDistance => lastSignedDistance;

        /// <summary>Surface normal from the last successful query set.</summary>
        public Vector3 LastSurfaceNormal => lastSurfaceNormal;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            activationTime = Time.time;
            ClearWaterState();
        }

        private void FixedUpdate()
        {
            if (body == null || body.isKinematic)
            {
                return;
            }

            var rampFactor = 1f;
            if (settings.ForceRampDuration > 0f)
            {
                var elapsed = Time.time - activationTime;
                rampFactor = Mathf.Clamp01(elapsed / settings.ForceRampDuration);
            }

            if (settings.SampleMode == BuoyancySampleMode.FourPoint)
            {
                ApplyFourPointBuoyancy(rampFactor);
                return;
            }

            ApplySinglePointBuoyancy(rampFactor);
        }

        private void ApplySinglePointBuoyancy(float rampFactor)
        {
            var sampleWorld = transform.TransformPoint(settings.SamplePointOffset);

            if (!WaterLevelQueryService.TryGetWaterQuery(sampleWorld, out var query) || !query.IsInsideZone)
            {
                ClearWaterState();
                return;
            }

            hasWater = true;
            lastActiveSampleCount = 1;
            lastSurfaceHeight = query.SurfaceHeight;
            lastSignedDistance = query.SignedDistanceToSurface;
            lastSurfaceNormal = query.SurfaceNormal;

            ApplyBuoyancyAtPoint(sampleWorld, query, rampFactor, 1f);

            if (settings.AlignmentStrength > 0f)
            {
                ApplyAlignmentTorque(query.SurfaceNormal, rampFactor);
            }
        }

        private void ApplyFourPointBuoyancy(float rampFactor)
        {
            var centerOffset = settings.SamplePointOffset;
            var footprint = settings.SampleFootprintHalfExtents;
            var validWaterCount = 0;
            var surfaceHeightSum = 0f;
            var signedDistanceSum = 0f;
            var surfaceNormalSum = Vector3.zero;

            for (var i = 0; i < FourPointSigns.Length; i++)
            {
                var sign = FourPointSigns[i];
                var localOffset = centerOffset + new Vector3(sign.x * footprint.x, 0f, sign.y * footprint.y);
                var sampleWorld = transform.TransformPoint(localOffset);

                if (!WaterLevelQueryService.TryGetWaterQuery(sampleWorld, out var query) || !query.IsInsideZone)
                {
                    continue;
                }

                validWaterCount++;
                surfaceHeightSum += query.SurfaceHeight;
                signedDistanceSum += query.SignedDistanceToSurface;
                surfaceNormalSum += query.SurfaceNormal;

                ApplyBuoyancyAtPoint(sampleWorld, query, rampFactor, 0.25f);
            }

            hasWater = validWaterCount > 0;
            lastActiveSampleCount = validWaterCount;

            if (!hasWater)
            {
                ClearWaterState();
                return;
            }

            lastSurfaceHeight = surfaceHeightSum / validWaterCount;
            lastSignedDistance = signedDistanceSum / validWaterCount;
            lastSurfaceNormal = surfaceNormalSum.sqrMagnitude > 0.0001f
                ? surfaceNormalSum.normalized
                : Vector3.up;

            if (settings.AlignmentStrength > 0f)
            {
                ApplyAlignmentTorque(lastSurfaceNormal, rampFactor);
            }
        }

        private void ApplyBuoyancyAtPoint(Vector3 sampleWorld, WaterQueryResult query, float rampFactor, float forceScale)
        {
            var targetHeight = query.SurfaceHeight + settings.SurfaceOffset;
            var submersion = targetHeight - sampleWorld.y;

            if (submersion <= 0f)
            {
                return;
            }

            var clampedSubmersion = Mathf.Min(submersion, settings.SubmersionDepthClamp);
            var buoyancyForce = clampedSubmersion * settings.BuoyancyForce * rampFactor * forceScale;
            body.AddForceAtPosition(Vector3.up * buoyancyForce, sampleWorld, ForceMode.Acceleration);

            if (settings.VerticalDamping > 0f)
            {
                var pointVerticalVelocity = body.GetPointVelocity(sampleWorld).y;
                var dampingForce = -pointVerticalVelocity * settings.VerticalDamping * forceScale;
                body.AddForceAtPosition(Vector3.up * dampingForce, sampleWorld, ForceMode.Acceleration);
            }
        }

        private void ApplyAlignmentTorque(Vector3 surfaceNormal, float rampFactor)
        {
            var currentUp = transform.up;
            var cross = Vector3.Cross(currentUp, surfaceNormal);
            var sinAngle = cross.magnitude;

            if (sinAngle < 0.0001f)
            {
                ApplyAngularDamping();
                return;
            }

            var cosAngle = Vector3.Dot(currentUp, surfaceNormal);
            var angleRad = Mathf.Atan2(sinAngle, cosAngle);
            angleRad = Mathf.Min(angleRad, AlignmentMaxAngleDegrees * Mathf.Deg2Rad);

            var axis = cross / sinAngle;
            body.AddTorque(axis * (angleRad * settings.AlignmentStrength * rampFactor), ForceMode.Acceleration);

            ApplyAngularDamping();
        }

        private void ApplyAngularDamping()
        {
            if (settings.AlignmentDamping > 0f)
            {
                body.AddTorque(-body.angularVelocity * settings.AlignmentDamping, ForceMode.Acceleration);
            }
        }

        private void ClearWaterState()
        {
            hasWater = false;
            lastActiveSampleCount = 0;
            lastSurfaceHeight = 0f;
            lastSignedDistance = 0f;
            lastSurfaceNormal = Vector3.up;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (settings != null && settings.SampleMode == BuoyancySampleMode.FourPoint)
            {
                var footprint = settings.SampleFootprintHalfExtents;
                Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.75f);

                for (var i = 0; i < FourPointSigns.Length; i++)
                {
                    var sign = FourPointSigns[i];
                    var cornerLocal = settings.SamplePointOffset + new Vector3(sign.x * footprint.x, 0f, sign.y * footprint.y);
                    var cornerWorld = transform.TransformPoint(cornerLocal);
                    Gizmos.DrawWireSphere(cornerWorld, 0.12f);
                }
            }
            else
            {
                var sampleWorld = transform.TransformPoint(settings != null ? settings.SamplePointOffset : Vector3.zero);
                Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.9f);
                Gizmos.DrawWireSphere(sampleWorld, 0.15f);
            }

            if (Application.isPlaying && hasWater)
            {
                var centerSampleWorld = transform.TransformPoint(settings != null ? settings.SamplePointOffset : Vector3.zero);
                var surfacePoint = new Vector3(centerSampleWorld.x, lastSurfaceHeight, centerSampleWorld.z);
                Gizmos.color = lastSignedDistance < 0f
                    ? new Color(0.2f, 1f, 0.4f, 0.7f)
                    : new Color(1f, 0.6f, 0.2f, 0.7f);
                Gizmos.DrawLine(centerSampleWorld, surfacePoint);
                Gizmos.DrawWireSphere(surfacePoint, 0.1f);

                if (settings.AlignmentStrength > 0f)
                {
                    Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.6f);
                    Gizmos.DrawRay(surfacePoint, lastSurfaceNormal * 1.5f);
                }
            }
        }
#endif
    }
}
