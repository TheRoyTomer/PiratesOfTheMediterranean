using UnityEngine;

public sealed class BreakSteerState : AIState
{
    private const float PursuitDistance = 500f;
    private const float PursuitRearArc = 100f;
    private const float HeadingTolerance = 60f;
    private const float PursuitDuration = 6f;

    private const float TargetTurnAngle = 90f;
    private const float TurnCompletionTolerance = 10f;
    private const float EscapeRunDuration = 3f;

    private float pursuitTimer;

    private Vector3 escapeDirection;

    private bool escapeRunStarted;
    private float escapeRunTimer;

    public BreakSteerState(AIController controller)
        : base(controller)
    {
    }

    public bool ShouldEnter =>
        pursuitTimer >= PursuitDuration;

    public void UpdatePursuit(
        Vector3 toTarget,
        float distance,
        bool eligible)
    {
        if (!eligible ||
            !IsCloseParallelPursuit(
                toTarget,
                distance))
        {
            pursuitTimer = 0f;
            return;
        }

        pursuitTimer += Time.fixedDeltaTime;
    }

    public bool Begin(Vector3 toTarget)
    {
        pursuitTimer = 0f;

        escapeRunStarted = false;
        escapeRunTimer = 0f;

        Vector3 flatForward =
            Vector3.ProjectOnPlane(
                Controller.transform.forward,
                Vector3.up
            ).normalized;

        if (flatForward.sqrMagnitude <= 0.001f)
            return false;

        Vector3 rightDirection =
            Quaternion.Euler(
                0f,
                TargetTurnAngle,
                0f
            ) * flatForward;

        Vector3 leftDirection =
            Quaternion.Euler(
                0f,
                -TargetTurnAngle,
                0f
            ) * flatForward;

        bool rightBlocked =
            Controller.ObstacleAvoidance
                .IsDirectionBlocked(
                    rightDirection
                );

        bool leftBlocked =
            Controller.ObstacleAvoidance
                .IsDirectionBlocked(
                    leftDirection
                );

        if (rightBlocked && leftBlocked)
            return false;

        if (rightBlocked)
        {
            escapeDirection = leftDirection;
            return true;
        }

        if (leftBlocked)
        {
            escapeDirection = rightDirection;
            return true;
        }

        Vector3 flatToPlayer = toTarget;
        flatToPlayer.y = 0f;

        if (flatToPlayer.sqrMagnitude <= 0.001f)
        {
            escapeDirection =
                Random.value < 0.5f
                    ? leftDirection
                    : rightDirection;

            return true;
        }

        float rightSeparation =
            Vector3.Angle(
                rightDirection,
                flatToPlayer
            );

        float leftSeparation =
            Vector3.Angle(
                leftDirection,
                flatToPlayer
            );

        escapeDirection =
            rightSeparation >= leftSeparation
                ? rightDirection
                : leftDirection;

        return true;
    }

    public override void Tick(
        Vector3 toTarget,
        float distance)
    {
        if (!escapeRunStarted)
        {
            TickTurn();
            return;
        }

        TickEscapeRun();
    }

    private void TickTurn()
    {
        float angle =
            Vector3.SignedAngle(
                Controller.transform.forward,
                escapeDirection,
                Vector3.up
            );

        Controller.DesiredSteering =
            Controller.CalculateSteering(
                angle,
                Controller.AlignmentTolerance,
                Controller.FullSteeringAngle
            );

        Controller.DesiredThrottle = 1f;

        if (Mathf.Abs(angle) <=
            TurnCompletionTolerance)
        {
            escapeRunStarted = true;
            escapeRunTimer = 0f;
        }
    }

    private void TickEscapeRun()
    {
        escapeRunTimer +=
            Time.fixedDeltaTime;

        float angle =
            Vector3.SignedAngle(
                Controller.transform.forward,
                escapeDirection,
                Vector3.up
            );

        Controller.DesiredSteering =
            Controller.CalculateSteering(
                angle,
                Controller.AlignmentTolerance,
                Controller.FullSteeringAngle
            );

        Controller.DesiredThrottle = 1f;

        if (escapeRunTimer >=
            EscapeRunDuration)
        {
            Controller.ReturnFromBreakSteer();
        }
    }

    private bool IsCloseParallelPursuit(
        Vector3 toTarget,
        float distance)
    {
        if (Controller.Target == null)
            return false;

        if (distance > PursuitDistance)
            return false;

        Vector3 flatToPlayer = toTarget;
        flatToPlayer.y = 0f;

        if (flatToPlayer.sqrMagnitude <= 0.001f)
            return false;

        Vector3 flatForward =
            Vector3.ProjectOnPlane(
                Controller.transform.forward,
                Vector3.up
            ).normalized;

        float rearArcAngle =
            Vector3.Angle(
                -flatForward,
                flatToPlayer.normalized
            );

        if (rearArcAngle > PursuitRearArc)
            return false;

        Vector3 playerForward =
            Vector3.ProjectOnPlane(
                Controller.Target.forward,
                Vector3.up
            ).normalized;

        if (playerForward.sqrMagnitude <= 0.001f)
            return false;

        float headingDifference =
            Vector3.Angle(
                flatForward,
                playerForward
            );

        if (headingDifference >
            HeadingTolerance)
        {
            return false;
        }

        return true;
    }

    public void Cancel()
    {
        pursuitTimer = 0f;

        escapeDirection = Vector3.zero;

        escapeRunStarted = false;
        escapeRunTimer = 0f;
    }
}