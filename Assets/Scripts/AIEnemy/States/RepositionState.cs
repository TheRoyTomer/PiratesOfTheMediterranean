using UnityEngine;

public sealed class RepositionState : AIState
{
    private const float RepositionTimeout = 8f;

    private float repositionTimer;

    public RepositionState(AIController controller) : base(controller) { }

    public void Begin()
    {
        repositionTimer = 0f;
    }

    public override void Tick(Vector3 toTarget, float distance)
    {
        repositionTimer += Time.fixedDeltaTime;

        Controller.DesiredSteering = 0f;
        Controller.DesiredThrottle = 1f;

        if (distance >= Controller.ExitBroadsideRange ||
            repositionTimer >= RepositionTimeout)
        {
            Controller.ChangeState(
                AIController.CombatMode.Chase
            );
        }
    }
}