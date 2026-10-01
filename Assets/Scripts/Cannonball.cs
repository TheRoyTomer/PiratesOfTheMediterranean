using UnityEngine;
using KWS;

[RequireComponent(typeof(Rigidbody))]
public class Cannonball : MonoBehaviour
{
    private ShipHealth owner;
    private bool hasHit;
    private bool hasSplashed;
    [Header("Damage")]
    [SerializeField] private float damage = 5f;

    [Header("Impact Effects")]
    [SerializeField] private GameObject impactExplosionEffect;
    [SerializeField] private GameObject impactAftermathEffect;
    [SerializeField, Min(0f)] private float impactSmokeDelay = 0.5f;

    [SerializeField] private float impactSurfaceSearchDistance = 5f;
    [SerializeField] private float impactSurfaceOffset = 0.2f;

    [SerializeField] private float explosionLifetime = 4f;

    [Header("Water Impact")]
    [SerializeField] private GameObject waterSplashEffect;
    [SerializeField] private float waterSplashLifetime = 4f;

    [Header("Flight")]
    [SerializeField] private float upwardSpeed = 3.037f;
    [SerializeField] private float gravityStrength = 2.305f;

    [Header("Pool")]
    [SerializeField] private float maxLifetime = 8f;
    [Tooltip("Shared scene VFX targets for five ships firing up to 70 shots per combined volley.")]
    [SerializeField, Min(0)] private int initialExplosionPoolSize = 80;
    [SerializeField, Min(0)] private int initialSplashPoolSize = 80;
    [Tooltip("Smoke can last about 12 seconds: reserve roughly three volleys plus headroom.")]
    [SerializeField, Min(0)] private int initialSmokePoolSize = 240;

    private Rigidbody rb;
    private CannonballPool pool;
    private float lifeTimer;

    public void PrewarmEffects(VfxPool effects)
    {
        effects.Prewarm(impactExplosionEffect, initialExplosionPoolSize);
        effects.Prewarm(impactAftermathEffect, initialSmokePoolSize);
        effects.Prewarm(waterSplashEffect, initialSplashPoolSize);
    }

    public Vector3 GetLaunchVelocity(Vector3 direction, float speed) =>
        direction.normalized * speed + Vector3.up * upwardSpeed;

    public bool TryPredictWaterImpact(Vector3 origin, Vector3 direction, float speed,
        float waterLevel, float step, out Vector3 impact)
    {
        impact = origin;
        if (step <= 0f) return false;
        Vector3 velocity = GetLaunchVelocity(direction, speed);
        // Match FixedUpdate: test water/lifetime before applying gravity and simulating.
        // Unity uses semi-implicit integration; a continuous parabola lands too far away.
        var body = GetComponent<Rigidbody>();
        float damping = body != null ? body.linearDamping : 0f;
        float dampingFactor = Mathf.Max(0f, 1f - damping * step);
        float elapsed = 0f;
        int steps = Mathf.CeilToInt(maxLifetime / step) + 1;
        for (int i = 0; i < steps; i++)
        {
            elapsed += step;
            if (impact.y <= waterLevel) return true;
            if (elapsed >= maxLifetime) return false;
            velocity += Vector3.down * gravityStrength * step;
            velocity *= dampingFactor;
            impact += velocity * step;
        }
        return false;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    public void Launch(
        CannonballPool cannonballPool,
        Vector3 direction,
        float speed,
        ShipHealth cannonballOwner)
    {
        pool = cannonballPool;
        owner = cannonballOwner;

        lifeTimer = 0f;
        hasHit = false;
        hasSplashed = false;

        rb.linearVelocity = GetLaunchVelocity(direction, speed);

        rb.angularVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        lifeTimer += Time.fixedDeltaTime;

        rb.AddForce(
            Vector3.down * gravityStrength,
            ForceMode.Acceleration
        );

        if (!hasHit && !hasSplashed && WaterSystem.Instance != null)
        {
            float waterY = WaterSystem.Instance.WaterLevel;

            if (transform.position.y <= waterY)
            {
                SpawnWaterSplash(waterY);
                ReturnToPool();
                return;
            }
        }

        if (lifeTimer >= maxLifetime)
        {
            ReturnToPool();
            return;
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit || hasSplashed)
            return;

        ShipHealth shipHealth = other.GetComponentInParent<ShipHealth>();

        if (shipHealth == null || shipHealth == owner)
            return;

        hasHit = true;

        Vector3 incomingDirection = rb.linearVelocity.normalized;

        Vector3 outsidePoint =
            transform.position -
            incomingDirection * impactSurfaceSearchDistance;

        Vector3 hitPoint =
            other.ClosestPoint(outsidePoint);

        hitPoint -=
            incomingDirection * impactSurfaceOffset;

        SpawnImpactEffects(hitPoint, shipHealth.transform);

        shipHealth.TakeDamage(damage, hitPoint);
        ReturnToPool();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit || hasSplashed)
            return;

        int shorelineLayer = LayerMask.NameToLayer("ShorelineBoundary");
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            Collider other = contact.otherCollider;
            if (other == null || other.isTrigger ||
                other.gameObject.layer == shorelineLayer ||
                other.GetComponentInParent<ShipHealth>() != null)
                continue;

            hasHit = true;
            Vector3 hitPoint = contact.point + contact.normal * 0.05f;
            SpawnImpactEffects(hitPoint);
            ReturnToPool();
            return;
        }
    }

    private void SpawnWaterSplash(float waterY)
    {
        hasSplashed = true;
        GameAudio.Play(GameSound.CannonWaterSplash, new Vector3(transform.position.x, waterY, transform.position.z));

        if (waterSplashEffect == null)
            return;

        Vector3 splashPosition = transform.position;
        splashPosition.y = waterY;

        pool.Effects.Play(
            waterSplashEffect,
            splashPosition,
            Quaternion.LookRotation(Vector3.up),
            waterSplashLifetime
        );
    }

    private void SpawnImpactEffects(Vector3 hitPoint, Transform hitParent = null)
    {
        GameAudio.Play(GameSound.CannonImpact, hitPoint);
        if (impactExplosionEffect != null)
        {
            pool.Effects.Play(
                impactExplosionEffect,
                hitPoint,
                Quaternion.identity,
                explosionLifetime
            );
        }

        if (impactAftermathEffect != null)
        {
            pool.Effects.Play(
                impactAftermathEffect,
                hitPoint,
                Quaternion.identity,
                follow: hitParent,
                delay: impactSmokeDelay
            );
        }
    }

    private void ReturnToPool()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        pool.ReturnCannonball(gameObject);
    }
}
