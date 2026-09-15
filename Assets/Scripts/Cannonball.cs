using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Cannonball : MonoBehaviour
{
    private ShipHealth owner;
    private bool hasHit;

    [Header("Damage")]
    [SerializeField] private float damage = 10f;

    [Header("Impact Effects")]
    [SerializeField] private GameObject impactExplosionEffect;
    [SerializeField] private GameObject impactAftermathEffect;

    [SerializeField] private float impactSurfaceSearchDistance = 5f;
    [SerializeField] private float impactSurfaceOffset = 0.2f;

    [SerializeField] private float explosionLifetime = 2f;
    [SerializeField] private float aftermathEmissionDuration = 10f;
    [SerializeField] private float aftermathFadeDuration = 5f;

    [Header("Flight")]
    [SerializeField] private float upwardSpeed = 1.2f;
    [SerializeField] private float gravityStrength = 4.7f;

    [Header("Pool")]
    [SerializeField] private float waterDeathHeight = -3f;
    [SerializeField] private float maxLifetime = 8f;

    private Rigidbody rb;
    private CannonballPool pool;
    private float lifeTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // We control gravity ourselves.
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

        if (transform.position.y <= waterDeathHeight)
        {
            ReturnToPool();
        }
        else if (lifeTimer >= maxLifetime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
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
            GameObject aftermath = Instantiate(
                impactAftermathEffect,
                hitPoint,
                Quaternion.identity
            );

            StartCoroutine(FadeOutAftermath(aftermath));
        }
    }

    private IEnumerator FadeOutAftermath(GameObject aftermath)
    {
        yield return new WaitForSeconds(aftermathEmissionDuration);

        ParticleSystem[] particleSystems =
            aftermath.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Stop(
                false,
                ParticleSystemStopBehavior.StopEmitting
            );
        }

        yield return new WaitForSeconds(aftermathFadeDuration);

        Destroy(aftermath);
    }

    private void ReturnToPool()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        pool.ReturnCannonball(gameObject);
    }
}
