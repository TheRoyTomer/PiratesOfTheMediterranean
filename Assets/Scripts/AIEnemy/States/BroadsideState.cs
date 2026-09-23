using UnityEngine;

public sealed class BroadsideState : AIState
{
    private const float BroadsideTimeout = 5.5f;
    private const float AimLockForwardSpeed = 2f;

    private readonly Rigidbody rb;

    private float broadsideTimer;

    private Vector3 aimDirection;
    private bool aimLocked;

    public BroadsideState(AIController controller)
        : base(controller)
    {
        rb = controller.GetComponent<Rigidbody>();
    }

    public void Begin(Vector3 toTarget)
    {
        broadsideTimer = 0f;
        aimLocked = false;

        UpdateAimDirection(toTarget);
    }

    public override void Tick(
        Vector3 toTarget,
        float distance)
    {
        broadsideTimer += Time.fixedDeltaTime;

        if (broadsideTimer >= BroadsideTimeout)
        {
            ExitToReposition();
            return;
        }

        if (!aimLocked)
        {
            UpdateAimDirection(toTarget);

            if (GetForwardSpeed() <= AimLockForwardSpeed)
            {
                aimLocked = true;
            }
        }

        if (aimDirection.sqrMagnitude <= 0.001f)
        {
            ExitToReposition();
            return;
        }

        Vector3 sideDirection;

        if (Controller.SelectedBroadside ==
            FiringDirection.Right)
        {
            sideDirection =
                Controller.transform.right;
        }
        else
        {
            sideDirection =
                -Controller.transform.right;
        }

        float sideAngle =
            Vector3.SignedAngle(
                sideDirection,
                aimDirection,
                Vector3.up
            );

        Controller.DesiredSteering =
            Controller.CalculateSteering(
                sideAngle,
                Controller.BroadsideAlignmentTolerance,
                Controller.BroadsideFullSteeringAngle
            );

        Controller.DesiredThrottle =
            Controller.BroadsideBrakeThrottle;

        if (distance <= Controller.MaxCannonRange &&
            Mathf.Abs(sideAngle) <=
            Controller.BroadsideFireAngle &&
            !Controller.Weapons.IsOnCooldown(
                Controller.SelectedBroadside))
        {
            Controller.Weapons.Fire(
                Controller.SelectedBroadside
            );

            ExitToReposition();
        }
    }

    private void UpdateAimDirection(Vector3 toTarget)
    {
        Vector3 flatTarget = toTarget;
        flatTarget.y = 0f;

        if (flatTarget.sqrMagnitude <= 0.001f)
            return;

        aimDirection = flatTarget.normalized;
    }

    private float GetForwardSpeed()
    {
        if (rb == null)
            return 0f;

        Vector3 forward =
            Vector3.ProjectOnPlane(
                Controller.transform.forward,
                Vector3.up
            );

        if (forward.sqrMagnitude <= 0.001f)
            return 0f;

        forward.Normalize();

        return Mathf.Max(
            0f,
            Vector3.Dot(
                rb.linearVelocity,
                forward
            )
        );
    }

    private void ExitToReposition()
    {
        Controller.SelectedBroadside =
            FiringDirection.Front;

        Controller.ChangeState(
            AIController.CombatMode.Reposition
        );
    }
}