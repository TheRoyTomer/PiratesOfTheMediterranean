using UnityEngine;

public sealed class SearchState : AIState
{
    public SearchState(AIController controller) : base(controller) { }

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
            Controller.DesiredSteering = 0f;
            Controller.DesiredThrottle = 0f;
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
                Controller.DesiredThrottle = 0f;
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
            Controller.SearchTimer = 0f;
            Controller.ReachedSearchArea = false;
            Controller.HasLastKnownTargetPosition = false;

            Controller.ChangeState(AIController.CombatMode.Patrol);
            return;
        }

        Controller.DesiredSteering = 0f;
        Controller.DesiredThrottle = 0f;
    }
}
