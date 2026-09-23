using UnityEngine;

public sealed class EvadeState : AIState
{
    private PatrolPoint evadeTarget;
    private bool breakAngleCompleted;

    public EvadeState(AIController controller) : base(controller) { }

    public override void Tick(Vector3 toTarget, float distance)
    {
        Controller.EvadeTimer += Time.fixedDeltaTime;

        if (evadeTarget == null)
        {
            ChooseNewEvadeTarget();

            if (evadeTarget == null)
            {
                Controller.ChangeState(AIController.CombatMode.Chase);
                return;
            }
        }

        bool minimumTimePassed =
            Controller.EvadeTimer >= Controller.EvadeMinimumTime;

        bool noRecentDamage =
            Time.time - Controller.LastDamageTime >=
            Controller.EvadeNoDamageExitTime;

        bool maximumTimeReached =
            Controller.EvadeTimer >= Controller.EvadeMaximumTime;

        if (maximumTimeReached)
        {
            Reset();

            Controller.ChangeState(
                Controller.ShouldChaseAfterEvade()
                    ? AIController.CombatMode.Chase
                    : AIController.CombatMode.Patrol
            );

            return;
        }

        if (minimumTimePassed && noRecentDamage)
        {
            Reset();

            Controller.ChangeState(
                Controller.ShouldChaseAfterEvade()
                    ? AIController.CombatMode.Chase
                    : AIController.CombatMode.Patrol
            );

            return;
        }

        if (!breakAngleCompleted)
        {
            BreakAwayFromPlayer(toTarget);
            return;
        }

        SailToEvadeTarget();
    }

    private void BreakAwayFromPlayer(Vector3 toTarget)
    {
        Vector3 awayFromPlayer = -toTarget;
        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude <= 0.001f)
        {
            breakAngleCompleted = true;
            return;
        }

        float angle = Vector3.SignedAngle(
            Controller.transform.forward,
            awayFromPlayer,
            Vector3.up
        );

        Controller.DesiredSteering =
            Controller.CalculateSteering(
                angle,
                Controller.AlignmentTolerance,
                Controller.FullSteeringAngle
            );

        Controller.DesiredThrottle = 1f;

        if (Mathf.Abs(angle) <= 25f)
        {
            breakAngleCompleted = true;
        }
    }

    private void SailToEvadeTarget()
    {
        Vector3 toDestination =
            evadeTarget.transform.position -
            Controller.transform.position;

        toDestination.y = 0f;

        float destinationDistance =
            toDestination.magnitude;

        if (destinationDistance <=
            Controller.PatrolArrivalDistance)
        {
            ChooseNewEvadeTarget();
            return;
        }

        float angle = Vector3.SignedAngle(
            Controller.transform.forward,
            toDestination,
            Vector3.up
        );

        Controller.DesiredSteering =
            Controller.CalculateSteering(
                angle,
                Controller.AlignmentTolerance,
                Controller.FullSteeringAngle
            );

        Controller.DesiredThrottle = 1f;
    }

    private void ChooseNewEvadeTarget()
    {
        evadeTarget =
            Controller.ChooseEvadePatrolPoint();

        if (evadeTarget != null)
        {
            Controller.PatrolTarget = evadeTarget;
        }
    }

    public void Reset()
    {
        evadeTarget = null;
        breakAngleCompleted = false;
    }
}