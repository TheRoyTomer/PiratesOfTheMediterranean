using UnityEngine;

[RequireComponent(typeof(ShipController))]
[RequireComponent(typeof(WeaponSystem))]
public class AIController : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Steering")]
    [SerializeField] private float alignmentTolerance = 3f;
    [SerializeField] private float fullSteeringAngle = 30f;

    [Header("Movement")]
    [SerializeField] private float enterBroadsideRange = 325f;
    [SerializeField] private float exitBroadsideRange = 400f;
    [SerializeField] private float brakingDistance = 325f;
    [SerializeField] private float slowdownDistance = 450f;

    [Header("Firing")]
    [SerializeField] private float maxCannonRange = 600f;
    [SerializeField] private float frontFireAngle = 5f;
    [SerializeField] private float broadsideFireAngle = 1f;

    [Header("Combat Timing")]
    [SerializeField] private float minimumChaseTimeAfterBroadside = 3f;

    [Header("Broadside Selection")]
    [SerializeField] private float broadsideSwitchThreshold = 15f;

    private ShipController ship;
    private WeaponSystem weapons;

    private CombatMode combatMode = CombatMode.Chase;

    // Front means "no broadside has been chosen yet"
    private FiringDirection selectedBroadside = FiringDirection.Front;

    private float chaseLockTimer = 0f;

    private enum CombatMode
    {
        Chase,
        Broadside
    }

    private void Awake()
    {
        ship = GetComponent<ShipController>();
        weapons = GetComponent<WeaponSystem>();
    }

    private void FixedUpdate()
    {
        if (target == null)
            return;

        if (chaseLockTimer > 0f)
        {
            chaseLockTimer -= Time.fixedDeltaTime;
        }

        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        if (combatMode == CombatMode.Chase)
        {
            if (chaseLockTimer <= 0f &&
                distance <= enterBroadsideRange)
            {
                combatMode = CombatMode.Broadside;
                ChooseBroadside(toTarget);
            }
        }
        else if (combatMode == CombatMode.Broadside)
        {
            if (distance >= exitBroadsideRange)
            {
                combatMode = CombatMode.Chase;
            }
        }

        if (combatMode == CombatMode.Chase)
        {
            Chase(toTarget, distance);
        }
        else
        {
            Broadside(toTarget, distance);
        }
    }

    private void Chase(Vector3 toTarget, float distance)
    {
        float angle = Vector3.SignedAngle(
            transform.forward,
            toTarget,
            Vector3.up
        );

        float steering = CalculateSteering(angle);
        ship.SetSteering(steering);

        float throttle;

        if (distance > slowdownDistance)
        {
            throttle = 1f;
        }
        else if (distance > brakingDistance)
        {
            throttle = Mathf.InverseLerp(
                brakingDistance,
                slowdownDistance,
                distance
            );
        }
        else
        {
            throttle = -1f;
        }

        ship.SetThrottle(throttle);

        if (distance <= maxCannonRange &&
            Mathf.Abs(angle) <= frontFireAngle &&
            !weapons.IsOnCooldown(FiringDirection.Front))
        {
            weapons.Fire(FiringDirection.Front);
        }
    }

    private void ChooseBroadside(Vector3 toTarget)
    {
        float rightAngle = Vector3.Angle(
            transform.right,
            toTarget
        );

        float leftAngle = Vector3.Angle(
            -transform.right,
            toTarget
        );

        // First broadside choice
        if (selectedBroadside == FiringDirection.Front)
        {
            if (rightAngle < leftAngle)
            {
                selectedBroadside = FiringDirection.Right;
            }
            else
            {
                selectedBroadside = FiringDirection.Left;
            }

            return;
        }

        // Stay with the current side unless the other side
        // is clearly better by at least broadsideSwitchThreshold degrees.
        if (selectedBroadside == FiringDirection.Right)
        {
            if (leftAngle + broadsideSwitchThreshold < rightAngle)
            {
                selectedBroadside = FiringDirection.Left;
            }
        }
        else if (selectedBroadside == FiringDirection.Left)
        {
            if (rightAngle + broadsideSwitchThreshold < leftAngle)
            {
                selectedBroadside = FiringDirection.Right;
            }
        }
    }

    private void Broadside(Vector3 toTarget, float distance)
    {
        Vector3 sideDirection;

        if (selectedBroadside == FiringDirection.Right)
        {
            sideDirection = transform.right;
        }
        else
        {
            sideDirection = -transform.right;
        }

        float sideAngle = Vector3.SignedAngle(
            sideDirection,
            toTarget,
            Vector3.up
        );

        float steering = CalculateSteering(sideAngle);

        ship.SetSteering(steering);
        ship.SetThrottle(-1f);

        if (distance <= maxCannonRange &&
            Mathf.Abs(sideAngle) <= broadsideFireAngle &&
            !weapons.IsOnCooldown(selectedBroadside))
        {
            weapons.Fire(selectedBroadside);

            combatMode = CombatMode.Chase;
            chaseLockTimer = minimumChaseTimeAfterBroadside;
        }
    }

    private float CalculateSteering(float angle)
    {
        if (Mathf.Abs(angle) <= alignmentTolerance)
        {
            return 0f;
        }

        float magnitude = Mathf.InverseLerp(
            alignmentTolerance,
            fullSteeringAngle,
            Mathf.Abs(angle)
        );

        return Mathf.Sign(angle) * magnitude;
    }
}