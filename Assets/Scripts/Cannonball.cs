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

    [SerializeField] private float impactSurfaceSearchDistance = 5f;
    [SerializeField] private float impactSurfaceOffset = 0.2f;

    [SerializeField] private float explosionLifetime = 2f;

    [Header("Water Impact")]
    [SerializeField] private GameObject waterSplashEffect;
    [SerializeField] private float waterSplashLifetime = 4f;

    [Header("Flight")]
    [SerializeField] private float upwardSpeed = 1.2f;
    [SerializeField] private float gravityStrength = 4.7f;

    [Header("Pool")]
    [SerializeField] private float maxLifetime = 8f;

    private Rigidbody rb;
    private CannonballPool pool;
    private float lifeTimer;

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

        rb.linearVelocity =
            direction.normalized * speed +
            Vector3.up * upwardSpeed;

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

        SpawnImpactEffects(hitPoint);

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

        if (waterSplashEffect == null)
            return;

        Vector3 splashPosition = transform.position;
        splashPosition.y = waterY;

        GameObject splash = Instantiate(
            waterSplashEffect,
            splashPosition,
            Quaternion.LookRotation(Vector3.up)
        );

        Destroy(splash, waterSplashLifetime);
    }

    private void SpawnImpactEffects(Vector3 hitPoint)
    {
        if (impactExplosionEffect != null)
        {
            GameObject explosion = Instantiate(
                impactExplosionEffect,
                hitPoint,
                Quaternion.identity
            );

            Destroy(explosion, explosionLifetime);
        }

        if (impactAftermathEffect != null)
        {
            Instantiate(
                impactAftermathEffect,
                hitPoint,
                Quaternion.identity
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
