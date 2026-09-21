using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIObstacleAvoidance : MonoBehaviour
{
    [Header("Obstacle Detection")]
    [SerializeField] private float minimumObstacleLookAhead = 80f;
    [SerializeField] private float maximumObstacleLookAhead = 300f;
    [SerializeField] private float brakeAcceleration = 3f;
    [SerializeField] private float reactionTime = 3f;
    [SerializeField] private float obstacleCapsuleRadius = 10f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float avoidanceThrottle = 0.3f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public bool ApplyAvoidance(
        ref float desiredSteering,
        ref float desiredThrottle)
    {
        Vector3 movementDirection = GetMovementDirection();

        Vector3 leftDirection =
            RotateDirection(movementDirection, -45f);

        Vector3 rightDirection =
            RotateDirection(movementDirection, 45f);

        bool obstacleAhead =
            IsObstacleAhead(movementDirection);

        bool obstacleLeft =
            IsObstacleAhead(leftDirection);

        bool obstacleRight =
            IsObstacleAhead(rightDirection);

        if (!obstacleAhead)
            return false;

        desiredThrottle = Mathf.Min(
            desiredThrottle,
            avoidanceThrottle
        );

        if (!obstacleLeft && obstacleRight)
        {
            desiredSteering = -1f;
            return true;
        }

        if (!obstacleRight && obstacleLeft)
        {
            desiredSteering = 1f;
            return true;
        }

        if (!obstacleLeft && !obstacleRight)
        {
            if (desiredSteering < 0f)
                desiredSteering = -1f;
            else
                desiredSteering = 1f;

            return true;
        }

        desiredSteering = 0f;
        desiredThrottle = -1f;

        return true;
    }

    public bool IsDirectionBlocked(Vector3 worldDirection)
    {
        Vector3 flatDirection =
            Vector3.ProjectOnPlane(
                worldDirection,
                Vector3.up
            );

        if (flatDirection.sqrMagnitude <= 0.001f)
            return false;

        flatDirection.Normalize();

        return IsObstacleAhead(flatDirection);
    }

    private Vector3 GetMovementDirection()
    {
        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(
                rb.linearVelocity,
                Vector3.up
            );

        if (horizontalVelocity.magnitude > 1f)
        {
            return horizontalVelocity.normalized;
        }

        Vector3 forward =
            Vector3.ProjectOnPlane(
                transform.forward,
                Vector3.up
            );

        if (forward.sqrMagnitude <= 0.001f)
            return Vector3.forward;

        return forward.normalized;
    }

    private bool IsObstacleAhead(
        Vector3 movementDirection)
    {
        if (movementDirection.sqrMagnitude <= 0.001f)
            return false;

        movementDirection.Normalize();

        Vector3 center =
            transform.position + Vector3.up * 5f;

        Vector3 localBack =
            transform.TransformDirection(
                new Vector3(0f, 0f, -28f)
            );

        Vector3 localFront =
            transform.TransformDirection(
                new Vector3(0f, 0f, 38f)
            );

        Vector3 point1 = center + localBack;
        Vector3 point2 = center + localFront;

        float castDistance =
            GetObstacleLookAheadDistance();

        return Physics.CapsuleCast(
            point1,
            point2,
            obstacleCapsuleRadius,
            movementDirection,
            castDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private Vector3 RotateDirection(
        Vector3 direction,
        float angle)
    {
        return Quaternion.Euler(
            0f,
            angle,
            0f
        ) * direction;
    }

    private float GetObstacleLookAheadDistance()
    {
        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(
                rb.linearVelocity,
                Vector3.up
            );

        float speed =
            horizontalVelocity.magnitude;

        float safeBrakeAcceleration =
            Mathf.Max(
                brakeAcceleration,
                0.1f
            );

        float brakingDistance =
            (speed * speed) /
            (2f * safeBrakeAcceleration);

        float reactionDistance =
            reactionTime * speed;

        float calculatedLookAhead =
            reactionDistance +
            brakingDistance;

        return Mathf.Clamp(
            calculatedLookAhead,
            minimumObstacleLookAhead,
            maximumObstacleLookAhead
        );
    }
}