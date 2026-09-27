using UnityEngine;

public sealed class SearchState : AIState
{
    private float approachTimer;
    private float noProgressTimer;
    private float bestDistance;

    public SearchState(AIController controller) : base(controller) { }

    public void Begin()
    {
        approachTimer = 0f;
        noProgressTimer = 0f;
        bestDistance = float.PositiveInfinity;
    }

    private void EndSearch()
    {
        Controller.SearchTimer = 0f;
        Controller.ReachedSearchArea = false;
        Controller.HasLastKnownTargetPosition = false;
        Controller.ChooseSearchReturnPatrolPoint();
        Controller.DesiredSteering = 0f;
        Controller.DesiredThrottle = 0f;
        Controller.ChangeState(AIController.CombatMode.Patrol);
    }

    public override void Tick(Vector3 targetOffset, float targetDistance)
    {
        if (Controller.Perception.CanSeeTarget(Controller.Target, Controller.Perception.DetectionRange))   
        {
            Controller.LostTargetTimer = 0f;
            Controller.SearchTimer = 0f;
            Controller.ReachedSearchArea = false;

            Controller.ChangeState(AIController.CombatMode.Chase);
            return;
        }

        if (!Controller.HasLastKnownTargetPosition)
        {
            EndSearch();
            return;
        }

        Vector3 toLastKnownPosition =
            Controller.LastKnownTargetPosition - Controller.transform.position;

        toLastKnownPosition.y = 0f;

        float distance = toLastKnownPosition.magnitude;

        if (!Controller.ReachedSearchArea)
        {
            if (distance <= Controller.SearchArrivalDistance)
            {
                Controller.ReachedSearchArea = true;
                Controller.SearchTimer = 0f;

                Controller.DesiredSteering = 0f;
                Controller.DesiredThrottle = 0.2f;
                return;
            }

            approachTimer += Time.fixedDeltaTime;
            noProgressTimer += Time.fixedDeltaTime;
            if (distance <= bestDistance - Controller.SearchProgressDistance)
            {
                bestDistance = distance;
                noProgressTimer = 0f;
            }

            if (approachTimer >= Controller.SearchApproachTimeout ||
                noProgressTimer >= Controller.SearchNoProgressTimeout)
            {
                EndSearch();
                return;
            }

            float angle = Vector3.SignedAngle(
                Controller.transform.forward,
                toLastKnownPosition,
                Vector3.up
            );

            Controller.DesiredSteering = Controller.CalculateSteering(
                angle,
                Controller.AlignmentTolerance,
                Controller.FullSteeringAngle
            );

            Controller.DesiredThrottle = 1f;
            return;
        }

        Controller.SearchTimer += Time.fixedDeltaTime;

        if (Controller.SearchTimer >= Controller.SearchTimeout)
        {
            EndSearch();
            return;
        }

        Controller.DesiredSteering = 0f;
        Controller.DesiredThrottle = 0.2f;
    }
}
