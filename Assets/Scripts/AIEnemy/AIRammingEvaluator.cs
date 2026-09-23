using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AIObstacleAvoidance))]
[RequireComponent(typeof(AIRammingEvaluator))]
public sealed class AIRammingEvaluator : MonoBehaviour
{
    [Header("Ramming Decision")]
    [SerializeField] private float predictionTime = 6f;
    [SerializeField] private float interceptDistance = 40f;
    [SerializeField] private float headOnExclusionAngle = 35f;
    [SerializeField] private float decisionInterval = 7f;
    [SerializeField, Range(0f, 1f)] private float ramChance = 0.25f;

    private Rigidbody rb;
    private AIObstacleAvoidance obstacleAvoidance;

    private float decisionTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        obstacleAvoidance = GetComponent<AIObstacleAvoidance>();
    }

    public void Tick(float deltaTime)
    {
        if (decisionTimer > 0f)
        {
            decisionTimer -= deltaTime;
        }
    }

    public bool TryChooseRamming(
        Transform target,
        out Vector3 interceptPoint)
    {
        interceptPoint = Vector3.zero;

        if (decisionTimer > 0f)
            return false;

        if (!TryGetRammingOpportunity(
                target,
                out interceptPoint))
        {
            return false;
        }

        if (Random.value > ramChance)
        {
            decisionTimer = decisionInterval;
            return false;
        }

        return true;
    }

    public void NotifyRammingEnded()
    {
        decisionTimer = decisionInterval;
    }

    private bool TryGetRammingOpportunity(
        Transform target,
        out Vector3 interceptPoint)
    {
        interceptPoint = Vector3.zero;

        if (target == null || rb == null)
            return false;

        Rigidbody targetRb =
            target.GetComponentInParent<Rigidbody>();

        if (targetRb == null)
            return false;

        Vector3 aiPosition = transform.position;
        Vector3 playerPosition = target.position;

        Vector3 aiVelocity = rb.linearVelocity;
        Vector3 playerVelocity = targetRb.linearVelocity;

        aiPosition.y = 0f;
        playerPosition.y = 0f;
        aiVelocity.y = 0f;
        playerVelocity.y = 0f;

        Vector3 relativePosition =
            playerPosition - aiPosition;

        Vector3 relativeVelocity =
            playerVelocity - aiVelocity;

        float relativeSpeedSquared =
            relativeVelocity.sqrMagnitude;

        if (relativeSpeedSquared <= 0.001f)
            return false;

        float rawClosestTime =
            -Vector3.Dot(
                relativePosition,
                relativeVelocity
            ) / relativeSpeedSquared;

        float closestTime =
            Mathf.Clamp(
                rawClosestTime,
                0f,
                predictionTime
            );

        Vector3 predictedAI =
            aiPosition +
            aiVelocity * closestTime;

        Vector3 predictedPlayer =
            playerPosition +
            playerVelocity * closestTime;

        float closestDistance =
            Vector3.Distance(
                predictedAI,
                predictedPlayer
            );

        if (closestDistance > interceptDistance)
            return false;

        Vector3 playerForward =
            Vector3.ProjectOnPlane(
                target.forward,
                Vector3.up
            ).normalized;

        Vector3 playerToAI =
            predictedAI - predictedPlayer;

        playerToAI.y = 0f;

        if (playerToAI.sqrMagnitude <= 0.001f)
            return false;

        float impactAngle =
            Vector3.Angle(
                playerForward,
                playerToAI.normalized
            );

        if (impactAngle <= headOnExclusionAngle)
            return false;

        interceptPoint =
            (predictedAI + predictedPlayer) * 0.5f;

        Vector3 toIntercept =
            interceptPoint - transform.position;

        toIntercept.y = 0f;

        if (toIntercept.sqrMagnitude <= 0.001f)
            return false;

        if (obstacleAvoidance.IsDirectionBlocked(toIntercept))
            return false;

        return true;
    }
}