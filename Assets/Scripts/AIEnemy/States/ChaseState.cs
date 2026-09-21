using UnityEngine;

public sealed class ChaseState : AIState
{
    public ChaseState(AIController controller) : base(controller) { }

    public override void Tick(Vector3 toTarget, float distance)
    {
        float angle = Vector3.SignedAngle(
            Controller.transform.forward,
            toTarget,
            Vector3.up
        );

        Controller.DesiredSteering = Controller.CalculateSteering(
            angle,
            Controller.AlignmentTolerance,
            Controller.FullSteeringAngle
        );

        float throttle;

        if (distance > Controller.SlowdownDistance)
        {
            throttle = 1f;
        }
        else if (distance > Controller.BrakingDistance)
        {
            throttle = Mathf.Lerp(
                Controller.MinimumApproachThrottle,
                1f,
                Mathf.InverseLerp(
                    Controller.BrakingDistance,
                    Controller.SlowdownDistance,
                    distance
                )
            );
        }
        else
        {
            throttle = -1f;
        }

        Controller.DesiredThrottle = throttle;

        if (distance <= Controller.MaxCannonRange &&
            Mathf.Abs(angle) <= Controller.FrontFireAngle &&
            !Controller.Weapons.IsOnCooldown(FiringDirection.Front))
        {
            Controller.Weapons.Fire(FiringDirection.Front);
        }
    }
}
