using UnityEngine;

public class WeaponSystem : MonoBehaviour
{

    [Header("Effects")]
    [SerializeField] private GameObject cannonFireEffect;

    [Header("Cooldown")]
    [SerializeField] private float cooldownDuration = 2.5f;
    [SerializeField] private float extraCooldownPenalty = 2f;

    private float frontCooldown;
    private float leftCooldown;
    private float rightCooldown;

    [Header("Fire Points")]
    [SerializeField] private Transform[] frontFirePoints;
    [SerializeField] private Transform[] leftFirePoints;
    [SerializeField] private Transform[] rightFirePoints;

    [Header("Cannon Settings")]
    [SerializeField] private CannonballPool cannonballPool;
    [SerializeField] private float cannonballSpeed = 150f;
    
    public float FrontCooldown => frontCooldown;
    public float LeftCooldown => leftCooldown;
    public float RightCooldown => rightCooldown;

    private void Update()
    {
        frontCooldown = UpdateCooldown(frontCooldown);
        leftCooldown = UpdateCooldown(leftCooldown);
        rightCooldown = UpdateCooldown(rightCooldown);

    }

    public void Fire(FiringDirection direction)
    {
        if (IsOnCooldown(direction))
            return;

        foreach (Transform firePoint in GetFirePoints(direction))
        {
            FireCannonball(firePoint);
        }

        StartCooldown(direction);
    }

    private float UpdateCooldown(float cooldown)
    {
        return Mathf.Max(0f, cooldown - Time.deltaTime);
    }

    private bool IsOnCooldown(FiringDirection direction)
    {
        return direction switch
        {
            FiringDirection.Front => frontCooldown > 0f,
            FiringDirection.Left => leftCooldown > 0f,
            FiringDirection.Right => rightCooldown > 0f,
            _ => true
        };
    }

    private void StartCooldown(FiringDirection direction)
    {
        bool anotherCooldownActive = HasOtherActiveCooldown(direction);

        if (anotherCooldownActive)
        {
            AddPenaltyToOtherActiveCooldowns(direction);
        }

        float newCooldown = anotherCooldownActive
            ? cooldownDuration + extraCooldownPenalty
            : cooldownDuration;

        switch (direction)
        {
            case FiringDirection.Front:
                frontCooldown = newCooldown;
                break;

            case FiringDirection.Left:
                leftCooldown = newCooldown;
                break;

            case FiringDirection.Right:
                rightCooldown = newCooldown;
                break;
        }
    }

    private bool HasOtherActiveCooldown(FiringDirection direction)
    {
        return direction switch
        {
            FiringDirection.Front => leftCooldown > 0f || rightCooldown > 0f,
            FiringDirection.Left => frontCooldown > 0f || rightCooldown > 0f,
            FiringDirection.Right => frontCooldown > 0f || leftCooldown > 0f,
            _ => false
        };
    }

    private void AddPenaltyToOtherActiveCooldowns(FiringDirection direction)
    {
        if (direction != FiringDirection.Front && frontCooldown > 0f)
            frontCooldown += extraCooldownPenalty;

        if (direction != FiringDirection.Left && leftCooldown > 0f)
            leftCooldown += extraCooldownPenalty;

        if (direction != FiringDirection.Right && rightCooldown > 0f)
            rightCooldown += extraCooldownPenalty;
    }

    private Transform[] GetFirePoints(FiringDirection direction)
    {
        return direction switch
        {
            FiringDirection.Front => frontFirePoints,
            FiringDirection.Left => leftFirePoints,
            FiringDirection.Right => rightFirePoints,
            _ => System.Array.Empty<Transform>()
        };
    }

    private void FireCannonball(Transform firePoint)
    {
        GameObject cannonballObject = cannonballPool.GetCannonball();
        Cannonball cannonball = cannonballObject.GetComponent<Cannonball>();

        cannonballObject.transform.position = firePoint.position;
        cannonballObject.transform.rotation = firePoint.rotation;

        Instantiate(
            cannonFireEffect,
            firePoint.position,
            firePoint.rotation
        );

        Vector3 firingDirection =
            Vector3.ProjectOnPlane(firePoint.forward, Vector3.up).normalized;

        cannonball.Launch(
            cannonballPool,
            firingDirection,
            cannonballSpeed,
            GetComponent<ShipHealth>()
        );
    }
}