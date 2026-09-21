using UnityEngine;

public sealed class RepositionState : AIState
{
    public RepositionState(AIController controller) : base(controller) { }

    public override void Tick(Vector3 toTarget, float distance)
    {
        Controller.DesiredSteering = 0f;
        Controller.DesiredThrottle = 1f;

        if (distance >= Controller.ExitBroadsideRange)
        {
            Controller.ChangeState(AIController.CombatMode.Chase);
        }
    }
}
