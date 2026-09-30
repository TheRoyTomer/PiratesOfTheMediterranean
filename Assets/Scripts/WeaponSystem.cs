using UnityEngine;

[RequireComponent(typeof(ShipConfiguration))]
public class WeaponSystem : MonoBehaviour
{
    public void SetCannonballPool(CannonballPool pool)
    {
        cannonballPool = pool;
    }

    private ShipConfig config;

    [Header("Effects")]
    [SerializeField] private GameObject cannonFireEffect;


    private ShipHealth shipHealth;
    private BarrelAmmo barrelAmmo;

    private float frontCooldown;
    private float leftCooldown;
    private float rightCooldown;

    [Header("Fire Points")]
    [SerializeField] private Transform[] frontFirePoints;
    [SerializeField] private Transform[] leftFirePoints;
    [SerializeField] private Transform[] rightFirePoints;

    [Header("Cannon Settings")]
    [SerializeField] private CannonballPool cannonballPool;
    
    [Header("Explosive Barrels")]
    [SerializeField] private BarrelStrikeController barrelsGroupPrefab;
    [SerializeField] private Transform barrelsReleasePoint;

    private Rigidbody shipRigidbody;
    private float nextBarrelReleaseTime;
    
    public float FrontCooldown => frontCooldown;
    public float LeftCooldown => leftCooldown;
    public float RightCooldown => rightCooldown;

    public Transform BarrelsReleasePoint => barrelsReleasePoint;
    public bool CanDeployBarrels =>
        (shipHealth == null || (!shipHealth.IsDead && shipHealth.CurrentHealth > 0f)) &&
        Time.time >= nextBarrelReleaseTime &&
        barrelsGroupPrefab != null && barrelsReleasePoint != null &&
        barrelAmmo != null && barrelAmmo.Count > 0;

    private void Awake()
    {
        config = GetComponent<ShipConfiguration>().Config;
        shipHealth = GetComponent<ShipHealth>();
        shipRigidbody = GetComponent<Rigidbody>();
        barrelAmmo = GetComponent<BarrelAmmo>();
    }

    private void Update()
    {
        frontCooldown = UpdateCooldown(frontCooldown);
        leftCooldown = UpdateCooldown(leftCooldown);
        rightCooldown = UpdateCooldown(rightCooldown);

    }

    public void Fire(FiringDirection direction)
    {
        if (shipHealth != null && shipHealth.IsDead)
            return;

        if (IsOnCooldown(direction))
            return;

        Transform[] firePoints = GetFirePoints(direction);
        Vector3 volleyCenter = Vector3.zero;
        int fired = 0;
        foreach (Transform firePoint in firePoints)
        {
            if (firePoint == null) continue;
            FireCannonball(firePoint);
            volleyCenter += firePoint.position;
            fired++;
        }

        // One sound per volley avoids stacking identical full-volume cannon reports.
        if (fired > 0)
            GameAudio.Play(GameSound.CannonFire, volleyCenter / fired);

        StartCooldown(direction);
    }
    
    public void DeployBarrels()
    {
        if (!CanDeployBarrels)
            return;

        BarrelStrikeController strike = Instantiate(
            barrelsGroupPrefab,
            barrelsReleasePoint.position,
            barrelsReleasePoint.rotation
        );

        Vector3 shipVelocity = shipRigidbody != null
            ? shipRigidbody.linearVelocity
            : Vector3.zero;

        strike.BeginRelease(shipVelocity, shipHealth);
        barrelAmmo.TryUseOne();
        nextBarrelReleaseTime = Time.time + 1f;
    }

    private float UpdateCooldown(float cooldown)
    {
        return Mathf.Max(0f, cooldown - Time.deltaTime);
    }

    public bool IsOnCooldown(FiringDirection direction)
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
            ? config.Combat.CooldownDuration + config.Combat.ExtraCooldownPenalty
            : config.Combat.CooldownDuration;

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
            frontCooldown += config.Combat.ExtraCooldownPenalty;

        if (direction != FiringDirection.Left && leftCooldown > 0f)
            leftCooldown += config.Combat.ExtraCooldownPenalty;

        if (direction != FiringDirection.Right && rightCooldown > 0f)
            rightCooldown += config.Combat.ExtraCooldownPenalty;
    }

    public Transform[] GetFirePoints(FiringDirection direction)
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

        Vector3 firingDirection = GetFiringDirection(firePoint);

        cannonball.Launch(
            cannonballPool,
            firingDirection,
            config.Combat.CannonballSpeed,
            shipHealth
        );
    }

    public static Vector3 GetFiringDirection(Transform firePoint) =>
        Vector3.ProjectOnPlane(firePoint.forward, Vector3.up).normalized;

    public bool TryPredictWaterImpact(Transform firePoint, float waterLevel, out Vector3 impact)
    {
        impact = default;
        return config != null && cannonballPool != null && firePoint != null &&
            cannonballPool.Projectile != null && cannonballPool.Projectile.TryPredictWaterImpact(
                firePoint.position, GetFiringDirection(firePoint), config.Combat.CannonballSpeed,
                waterLevel, Time.fixedDeltaTime, out impact);
    }
}
