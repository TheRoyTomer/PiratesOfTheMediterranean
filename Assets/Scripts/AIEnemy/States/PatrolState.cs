using UnityEngine;

public sealed class PatrolState : AIState
{
    public PatrolState(AIController controller) : base(controller) { }

    public override void Tick(Vector3 targetOffset, float targetDistance)
    {
        if (Controller.PatrolTarget == null)
        {
            Controller.DesiredSteering = 0f;
            Controller.DesiredThrottle = 0f;
            return;
        }

        Vector3 toDestination =
            Controller.PatrolTarget.transform.position - Controller.transform.position;

        toDestination.y = 0f;

        float distance = toDestination.magnitude;

        if (distance <= Controller.PatrolArrivalDistance)
        {
            ChooseNextPatrolPoint();
            return;
        }

        float angle = Vector3.SignedAngle(
            Controller.transform.forward,
            toDestination,
            Vector3.up
        );

        Controller.DesiredSteering = Controller.CalculateSteering(
            angle,
            Controller.AlignmentTolerance,
            Controller.FullSteeringAngle
        );

        Controller.DesiredThrottle = 1f;
    }

    private void ChooseNextPatrolPoint()
    {
        if (Controller.PatrolTarget == null)
            return;

        PatrolPoint[] connections = Controller.PatrolTarget.ConnectedPoints;

        if (connections == null || connections.Length == 0)
            return;

        PatrolPoint nextPoint = null;

        if (connections.Length == 1)
        {
            nextPoint = connections[0];
        }
        else
        {
            do
            {
                int randomIndex = Random.Range(0, connections.Length);
                nextPoint = connections[randomIndex];
            }
            while (nextPoint == Controller.PreviousPatrolPoint);
        }

        Controller.PreviousPatrolPoint = Controller.PatrolTarget;
        Controller.PatrolTarget = nextPoint;
    }
}
