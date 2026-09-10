using RenderWave.Runtime.Core;
using RenderWave.Runtime.Data;
using UnityEngine;

namespace RenderWave.Runtime.Wakes
{
    /// <summary>
    /// Registers a moving transform as a wake source for the active wake system.
    /// </summary>
    [AddComponentMenu("RenderWave/Wake Emitter")]
    [DisallowMultipleComponent]
    public sealed class WakeEmitter : MonoBehaviour
    {
        private const float MinimumMovementDistance = 0.00001f;
        private const float MinimumEmissionSpacing = 0.08f;
        private const float DirectionSmoothingSharpness = 14f;
        private const float EmissionDirectionBlend = 0.6f;
        private const float ContinuityLengthOverlapFactor = 0.8f;
        private const float TrailResetDelay = 0.2f;

        [SerializeField] private WakeSystem wakeSystem;
        [SerializeField] private WakeEmitterSettings settings = new WakeEmitterSettings();
        [SerializeField] private WakeEmissionDebugBlockReason debugBlockReason;
        [SerializeField] private float debugDetectedSpeed;
        [SerializeField] private float debugAccumulatedDistance;
        [SerializeField] private float debugEmissionSpacing;
        [SerializeField] private float debugSignedDistanceToSurface;
        [SerializeField] private int debugSpawnedSegments;

        private Vector3 lastPosition;
        private Vector3 smoothedDirection;
        private Vector3 lastEmittedPosition;
        private Vector3 lastEmittedDirection;
        private float accumulatedDistance;
        private float stationaryTime;
        private bool hasLastPosition;
        private bool hasSmoothedDirection;
        private bool hasLastEmittedSample;
        private bool isRegistered;

        public WakeEmitterSettings Settings => settings;
        public Vector3 CurrentWorldPosition => transform.position;

        private void OnEnable()
        {
            settings ??= new WakeEmitterSettings();
            settings.Validate();
            ResetSamplingState();
            TryRegister();
        }

        private void OnDisable()
        {
            Unregister();
            ResetSamplingState();
        }

        private void OnValidate()
        {
            settings ??= new WakeEmitterSettings();
            settings.Validate();
        }

        private void Update()
        {
            if (isRegistered && (wakeSystem == null || !wakeSystem.isActiveAndEnabled))
            {
                isRegistered = false;
            }

            if (!isRegistered)
            {
                TryRegister();
            }
        }

        internal void Tick(WakeSystem system, float deltaTime)
        {
            debugDetectedSpeed = 0f;
            debugEmissionSpacing = 0f;
            debugSignedDistanceToSurface = 0f;
            debugSpawnedSegments = 0;

            if (deltaTime <= 0f)
            {
                SetDebugState(WakeEmissionDebugBlockReason.InvalidDeltaTime, accumulatedDistance);
                return;
            }

            var currentPosition = transform.position;
            if (!hasLastPosition)
            {
                lastPosition = currentPosition;
                hasLastPosition = true;
                SetDebugState(WakeEmissionDebugBlockReason.WaitingForInitialSample, accumulatedDistance);
                return;
            }

            var delta = currentPosition - lastPosition;
            delta.y = 0f;

            var distance = delta.magnitude;
            if (distance <= MinimumMovementDistance)
            {
                lastPosition = currentPosition;
                stationaryTime += deltaTime;
                if (stationaryTime >= TrailResetDelay)
                {
                    ResetTrailContinuity();
                }

                SetDebugState(WakeEmissionDebugBlockReason.NoMovement, accumulatedDistance);
                return;
            }

            var speed = distance / deltaTime;
            debugDetectedSpeed = speed;
            if (speed < settings.MinSpeed || settings.EmissionRate <= 0f || settings.Intensity <= 0f)
            {
                accumulatedDistance = 0f;
                lastPosition = currentPosition;
                stationaryTime += deltaTime;
                if (stationaryTime >= TrailResetDelay)
                {
                    ResetTrailContinuity();
                }

                SetDebugState(WakeEmissionDebugBlockReason.BelowMinimumSpeed, accumulatedDistance);
                return;
            }

            stationaryTime = 0f;
            if (!TryGetSurfaceContact(currentPosition, out _, out var currentSignedDistance))
            {
                accumulatedDistance = 0f;
                lastPosition = currentPosition;
                ResetTrailContinuity();
                SetDebugState(WakeEmissionDebugBlockReason.OutsideSurfaceContactBand, accumulatedDistance, currentSignedDistance);
                return;
            }

            var rawDirection = delta / distance;
            var direction = UpdateSmoothedDirection(rawDirection, deltaTime);
            var spacing = Mathf.Max(MinimumEmissionSpacing, speed / settings.EmissionRate);
            var distanceSinceLastEmission = accumulatedDistance + distance;
            var distanceUntilNext = spacing - accumulatedDistance;
            var spawnedSegments = 0;
            var anySpawnedOnWater = false;

            while (distanceUntilNext <= distance + MinimumMovementDistance)
            {
                var samplePosition = lastPosition + (rawDirection * distanceUntilNext);
                if (!TryGetSurfaceContact(samplePosition, out var sampleWaterQuery, out var sampleSignedDistance))
                {
                    ResetTrailContinuity();
                    distanceUntilNext += spacing;
                    spawnedSegments++;
                    debugSignedDistanceToSurface = sampleSignedDistance;
                    continue;
                }

                var segmentDirection = GetEmissionDirection(direction);
                var segmentCenter = samplePosition;
                var emissionLength = Mathf.Max(settings.Length, settings.Width * 1.15f);

                if (TryBuildContinuousSegment(samplePosition, spacing, ref segmentCenter, ref segmentDirection, ref emissionLength))
                {
                    emissionLength = Mathf.Max(emissionLength, spacing * 1.35f);
                }
                else
                {
                    emissionLength = Mathf.Max(emissionLength, spacing * 1.6f);
                }

                if (system.EmitWake(segmentCenter, sampleWaterQuery, segmentDirection, settings.Width, emissionLength, settings.Lifetime, settings.FadeStart, settings.Intensity, settings.SurfaceOffset))
                {
                    anySpawnedOnWater = true;
                    lastEmittedPosition = samplePosition;
                    lastEmittedDirection = segmentDirection;
                    hasLastEmittedSample = true;
                    debugSignedDistanceToSurface = sampleSignedDistance;
                }
                else
                {
                    ResetTrailContinuity();
                }

                spawnedSegments++;
                distanceUntilNext += spacing;
            }

            accumulatedDistance = distanceSinceLastEmission - (spawnedSegments * spacing);
            accumulatedDistance = Mathf.Clamp(accumulatedDistance, 0f, spacing);
            lastPosition = currentPosition;

            debugEmissionSpacing = spacing;
            debugSpawnedSegments = spawnedSegments;

            if (spawnedSegments > 0)
            {
                SetDebugState(anySpawnedOnWater ? WakeEmissionDebugBlockReason.EmittedSegments : WakeEmissionDebugBlockReason.OutsideSurfaceContactBand, accumulatedDistance, debugSignedDistanceToSurface);
                return;
            }

            SetDebugState(WakeEmissionDebugBlockReason.WaitingForSpacing, accumulatedDistance, debugSignedDistanceToSurface);
        }

        private void TryRegister()
        {
            if (isRegistered)
            {
                return;
            }

            if (wakeSystem == null)
            {
                wakeSystem = WakeSystem.Default != null ? WakeSystem.Default : FindFirstObjectByType<WakeSystem>();
            }

            if (wakeSystem == null || !wakeSystem.isActiveAndEnabled)
            {
                return;
            }

            wakeSystem.RegisterEmitter(this);
            isRegistered = true;
        }

        private void Unregister()
        {
            if (!isRegistered || wakeSystem == null)
            {
                return;
            }

            wakeSystem.UnregisterEmitter(this);
            isRegistered = false;
        }

        private void ResetSamplingState()
        {
            lastPosition = default;
            smoothedDirection = Vector3.zero;
            lastEmittedPosition = default;
            lastEmittedDirection = Vector3.zero;
            accumulatedDistance = 0f;
            stationaryTime = 0f;
            hasLastPosition = false;
            hasSmoothedDirection = false;
            hasLastEmittedSample = false;
        }

        private void ResetTrailContinuity()
        {
            accumulatedDistance = 0f;
            lastEmittedPosition = default;
            lastEmittedDirection = Vector3.zero;
            hasLastEmittedSample = false;
        }

        private Vector3 UpdateSmoothedDirection(Vector3 rawDirection, float deltaTime)
        {
            rawDirection.y = 0f;
            if (rawDirection.sqrMagnitude <= MinimumMovementDistance)
            {
                return hasSmoothedDirection ? smoothedDirection : Vector3.forward;
            }

            rawDirection.Normalize();
            if (!hasSmoothedDirection)
            {
                smoothedDirection = rawDirection;
                hasSmoothedDirection = true;
                return smoothedDirection;
            }

            var blendFactor = 1f - Mathf.Exp(-DirectionSmoothingSharpness * deltaTime);
            smoothedDirection = Vector3.Slerp(smoothedDirection, rawDirection, blendFactor);
            smoothedDirection.y = 0f;

            if (smoothedDirection.sqrMagnitude <= MinimumMovementDistance)
            {
                smoothedDirection = rawDirection;
            }
            else
            {
                smoothedDirection.Normalize();
            }

            return smoothedDirection;
        }

        private Vector3 GetEmissionDirection(Vector3 preferredDirection)
        {
            var direction = preferredDirection;
            if (hasLastEmittedSample && lastEmittedDirection.sqrMagnitude > MinimumMovementDistance)
            {
                direction = Vector3.Slerp(lastEmittedDirection, preferredDirection, EmissionDirectionBlend);
            }

            direction.y = 0f;
            if (direction.sqrMagnitude <= MinimumMovementDistance)
            {
                return Vector3.forward;
            }

            return direction.normalized;
        }

        private bool TryBuildContinuousSegment(Vector3 samplePosition, float spacing, ref Vector3 segmentCenter, ref Vector3 segmentDirection, ref float emissionLength)
        {
            if (!hasLastEmittedSample)
            {
                return false;
            }

            var bridge = samplePosition - lastEmittedPosition;
            bridge.y = 0f;

            var bridgeDistance = bridge.magnitude;
            if (bridgeDistance <= MinimumMovementDistance)
            {
                return false;
            }

            var maxBridgeDistance = Mathf.Max(settings.Length * 2f, spacing * 3f, settings.Width * 2.5f);
            if (bridgeDistance > maxBridgeDistance)
            {
                return false;
            }

            var bridgeDirection = bridge / bridgeDistance;
            segmentDirection = GetEmissionDirection(bridgeDirection);
            segmentCenter = lastEmittedPosition + (bridge * 0.5f);
            emissionLength = Mathf.Max(emissionLength, bridgeDistance + (settings.Width * ContinuityLengthOverlapFactor));
            return true;
        }

        private bool TryGetSurfaceContact(Vector3 worldPosition, out WaterQueryResult waterQuery, out float signedDistanceToSurface)
        {
            if (!WaterLevelQueryService.TryGetWaterQuery(worldPosition, out waterQuery))
            {
                signedDistanceToSurface = 0f;
                return false;
            }

            signedDistanceToSurface = waterQuery.SignedDistanceToSurface;
            return Mathf.Abs(signedDistanceToSurface) <= settings.SurfaceContactTolerance;
        }

        private void SetDebugState(WakeEmissionDebugBlockReason blockReason, float currentAccumulatedDistance, float signedDistanceToSurface = 0f)
        {
            debugBlockReason = blockReason;
            debugAccumulatedDistance = currentAccumulatedDistance;
            debugSignedDistanceToSurface = signedDistanceToSurface;
        }

        [ContextMenu("Log Wake Debug State")]
        private void LogDebugState()
        {
            Debug.Log(
                $"RenderWave WakeEmitter Debug | Object: {name} | Registered: {isRegistered} | Speed: {debugDetectedSpeed:0.###} | AccumulatedDistance: {debugAccumulatedDistance:0.###} | EmissionSpacing: {debugEmissionSpacing:0.###} | SignedDistanceToSurface: {debugSignedDistanceToSurface:0.###} | SpawnedSegments: {debugSpawnedSegments} | BlockReason: {debugBlockReason}",
                this);
        }

        private enum WakeEmissionDebugBlockReason
        {
            None = 0,
            WaitingForInitialSample = 1,
            InvalidDeltaTime = 2,
            NoMovement = 3,
            BelowMinimumSpeed = 4,
            WaitingForSpacing = 5,
            NoWaterAtEmissionPoint = 6,
            EmittedSegments = 7,
            OutsideSurfaceContactBand = 8
        }
    }
}
