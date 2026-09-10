using UnityEngine;

namespace RenderWave.Examples
{
    /// <summary>
    /// Simple demo autopilot that drives a Rigidbody boat along a looping path.
    /// This is sample/demo content only - it is not part of the RenderWave runtime.
    ///
    /// Movement is Rigidbody-compatible:
    /// - horizontal thrust via AddForce in FixedUpdate
    /// - turning via AddTorque in FixedUpdate
    /// - never writes to transform.position or transform.rotation
    /// - fully compatible with BuoyancyBody (which handles vertical forces)
    /// - fully compatible with WakeEmitter (which reads transform.position)
    /// </summary>
    [AddComponentMenu("RenderWave/Examples/Demo Boat Autopilot")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DemoBoatAutopilot : MonoBehaviour
    {
        [Header("Path")]
        [Tooltip("World-space center of the patrol loop.")]
        [SerializeField] private Vector3 pathCenter = Vector3.zero;

        [Tooltip("Horizontal radius of the patrol ellipse on the X axis.")]
        [SerializeField, Min(5f)] private float pathRadiusX = 80f;

        [Tooltip("Horizontal radius of the patrol ellipse on the Z axis.")]
        [SerializeField, Min(5f)] private float pathRadiusZ = 80f;

        [Tooltip("Optional waypoints to follow instead of the ellipse. If populated, the ellipse path is ignored.")]
        [SerializeField] private Vector3[] waypoints;

        [Header("Speed")]
        [Tooltip("Target cruising speed in world units per second.")]
        [SerializeField, Min(0.5f)] private float targetSpeed = 8f;

        [Tooltip("Forward thrust force applied to reach target speed.")]
        [SerializeField, Min(1f)] private float thrustForce = 12f;

        [Header("Steering")]
        [Tooltip("Torque applied to turn toward the next path target.")]
        [SerializeField, Min(0.5f)] private float steeringTorque = 6f;

        [Tooltip("How far ahead on the path the boat aims, in normalized angle (ellipse) or distance (waypoints).")]
        [SerializeField, Min(0.01f)] private float lookAheadDistance = 15f;

        [Tooltip("Distance to a waypoint before advancing to the next one.")]
        [SerializeField, Min(1f)] private float waypointArrivalDistance = 10f;

        [Header("Limits")]
        [Tooltip("Maximum angular velocity in degrees per second. Prevents spinning.")]
        [SerializeField, Min(10f)] private float maxAngularSpeed = 90f;

        private Rigidbody body;
        private float pathAngle;
        private int currentWaypointIndex;

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

            // Initialize path angle from current position so the boat doesn't snap.
            var offset = transform.position - pathCenter;
            pathAngle = Mathf.Atan2(offset.z / Mathf.Max(pathRadiusZ, 1f), offset.x / Mathf.Max(pathRadiusX, 1f));

            currentWaypointIndex = 0;
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            var target = GetCurrentTarget();
            ApplyThrust(target);
            ApplySteering(target);
            ClampAngularVelocity();
            AdvancePath();
        }

        private Vector3 GetCurrentTarget()
        {
            if (waypoints != null && waypoints.Length >= 2)
            {
                return waypoints[currentWaypointIndex % waypoints.Length];
            }

            // Ellipse path: aim ahead of current angle.
            var lookAheadAngle = lookAheadDistance / Mathf.Max(pathRadiusX, pathRadiusZ);
            var aheadAngle = pathAngle + lookAheadAngle;
            return new Vector3(
                pathCenter.x + Mathf.Cos(aheadAngle) * pathRadiusX,
                transform.position.y,
                pathCenter.z + Mathf.Sin(aheadAngle) * pathRadiusZ
            );
        }

        private void ApplyThrust(Vector3 target)
        {
            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            forward.Normalize();

            // Only thrust when reasonably pointed toward target.
            var toTarget = target - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f)
            {
                return;
            }

            var alignment = Vector3.Dot(forward, toTarget.normalized);

            // Reduce thrust when pointing away from target (alignment < 0 = facing wrong way).
            var thrustFactor = Mathf.Clamp01(alignment * 0.5f + 0.5f);

            // Scale force to approach target speed without exceeding it.
            var currentSpeed = Vector3.Dot(body.linearVelocity, forward);
            var speedDeficit = targetSpeed - currentSpeed;
            if (speedDeficit <= 0f)
            {
                return;
            }

            var force = forward * (thrustForce * thrustFactor * Mathf.Clamp01(speedDeficit / targetSpeed));
            body.AddForce(force, ForceMode.Acceleration);
        }

        private void ApplySteering(Vector3 target)
        {
            var toTarget = target - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f)
            {
                return;
            }

            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            forward.Normalize();
            toTarget.Normalize();

            // Signed angle: positive = turn right (clockwise from above).
            var cross = Vector3.Cross(forward, toTarget).y;
            var dot = Vector3.Dot(forward, toTarget);

            // Proportional steering: more torque the further off-axis the target is.
            // The sign of cross gives us the turn direction.
            var steerAmount = Mathf.Clamp(cross, -1f, 1f);

            // Boost steering when nearly opposite (dot < 0) to avoid stalling.
            if (dot < 0f)
            {
                steerAmount = Mathf.Sign(steerAmount) * Mathf.Max(Mathf.Abs(steerAmount), 0.5f);
            }

            body.AddTorque(Vector3.up * (steerAmount * steeringTorque), ForceMode.Acceleration);
        }

        private void ClampAngularVelocity()
        {
            var angVel = body.angularVelocity;
            var maxRad = maxAngularSpeed * Mathf.Deg2Rad;
            if (angVel.sqrMagnitude > maxRad * maxRad)
            {
                body.angularVelocity = angVel.normalized * maxRad;
            }
        }

        private void AdvancePath()
        {
            if (waypoints != null && waypoints.Length >= 2)
            {
                var target = waypoints[currentWaypointIndex % waypoints.Length];
                var distXZ = HorizontalDistance(transform.position, target);
                if (distXZ < waypointArrivalDistance)
                {
                    currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                }

                return;
            }

            // Ellipse path: advance angle based on actual XZ position.
            var offset = transform.position - pathCenter;
            pathAngle = Mathf.Atan2(offset.z / Mathf.Max(pathRadiusZ, 1f), offset.x / Mathf.Max(pathRadiusX, 1f));
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw ellipse path.
            if (waypoints != null && waypoints.Length >= 2)
            {
                Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
                for (var i = 0; i < waypoints.Length; i++)
                {
                    var a = waypoints[i];
                    var b = waypoints[(i + 1) % waypoints.Length];
                    Gizmos.DrawLine(a, b);
                    Gizmos.DrawWireSphere(a, waypointArrivalDistance * 0.3f);
                }

                if (Application.isPlaying && waypoints.Length > 0)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(waypoints[currentWaypointIndex % waypoints.Length], waypointArrivalDistance);
                }

                return;
            }

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
            const int segments = 64;
            var prev = new Vector3(pathCenter.x + pathRadiusX, pathCenter.y, pathCenter.z);
            for (var i = 1; i <= segments; i++)
            {
                var angle = (i / (float)segments) * Mathf.PI * 2f;
                var next = new Vector3(
                    pathCenter.x + Mathf.Cos(angle) * pathRadiusX,
                    pathCenter.y,
                    pathCenter.z + Mathf.Sin(angle) * pathRadiusZ
                );
                Gizmos.DrawLine(prev, next);
                prev = next;
            }

            // Draw center.
            Gizmos.color = new Color(1f, 1f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(pathCenter, 2f);
        }
#endif
    }
}
