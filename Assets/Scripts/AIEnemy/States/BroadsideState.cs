using UnityEngine;

public sealed class BroadsideState : AIState
{
    public BroadsideState(AIController controller) : base(controller) { }

    public override void Tick(Vector3 toTarget, float distance)
    {
        Vector3 sideDirection;

        if (Controller.SelectedBroadside == FiringDirection.Right)
        {
            sideDirection = Controller.transform.right;
        }
        else
        {
            sideDirection = -Controller.transform.right;
        }

        float sideAngle = Vector3.SignedAngle(
            sideDirection,
            toTarget,
            Vector3.up
        );

        Controller.DesiredSteering = Controller.CalculateSteering(
            sideAngle,
            Controller.BroadsideAlignmentTolerance,
            Controller.BroadsideFullSteeringAngle
        );

        Controller.DesiredThrottle = Controller.BroadsideBrakeThrottle;

        if (distance <= Controller.MaxCannonRange &&
            Mathf.Abs(sideAngle) <= Controller.BroadsideFireAngle &&
            !Controller.Weapons.IsOnCooldown(Controller.SelectedBroadside))
        {
            Controller.Weapons.Fire(Controller.SelectedBroadside);

            Controller.SelectedBroadside = FiringDirection.Front;
            Controller.ChangeState(AIController.CombatMode.Reposition);
        }
    }
}
