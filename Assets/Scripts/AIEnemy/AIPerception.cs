using UnityEngine;

public class AIPerception : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRange = 1100f;
    [SerializeField, Range(0f, 180f)] private float fieldOfViewHalfAngle = 120f;
    [SerializeField] private LayerMask visibilityOccluderMask;
    [SerializeField] private float visionOriginHeight = 10f;
    [SerializeField] private float visionTargetHeight = 10f;
    [SerializeField] private float loseTargetRange = 1300f;

    public float DetectionRange => detectionRange;
    public float LoseTargetRange => loseTargetRange;

    public bool CanSeeTarget(Transform target, float allowedRange)
    {
        if (!IsTargetValid(target))
            return false;

        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance > allowedRange)
            return false;

        if (!IsTargetInsideFov(target))
            return false;

        if (!HasClearLineOfSight(target))
            return false;

        return true;
    }

    private bool HasClearLineOfSight(Transform target)
    {
        if (target == null)
            return false;

        Vector3 origin =
            transform.position +
            Vector3.up * visionOriginHeight;

        Vector3 targetPoint =
            target.position +
            Vector3.up * visionTargetHeight;

        Vector3 direction = targetPoint - origin;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return true;

        direction /= distance;

        bool blocked = Physics.Raycast(
            origin,
            direction,
            distance,
            visibilityOccluderMask,
            QueryTriggerInteraction.Ignore
        );

        return !blocked;
    }

    private bool IsTargetInsideFov(Transform target)
    {
        if (target == null)
            return false;

        Vector3 flatForward = Vector3.ProjectOnPlane(
            transform.forward,
            Vector3.up
        );

        Vector3 flatToTarget =
            target.position - transform.position;

        flatToTarget.y = 0f;

        if (flatToTarget.sqrMagnitude <= 0.001f)
            return true;

        if (flatForward.sqrMagnitude <= 0.001f)
            return false;

        float angleToTarget = Vector3.Angle(
            flatForward,
            flatToTarget
        );

        return angleToTarget <= fieldOfViewHalfAngle;
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        ShipHealth targetHealth =
            target.GetComponentInParent<ShipHealth>();

        if (targetHealth != null && targetHealth.IsDead)
            return false;

        return true;
    }
}