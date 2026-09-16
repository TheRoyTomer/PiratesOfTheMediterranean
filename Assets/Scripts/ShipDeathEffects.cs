using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ShipHealth))]
[RequireComponent(typeof(ShipConfiguration))]
public class ShipDeathEffects : MonoBehaviour
{
    private ShipConfig config;
    [Header("Death Explosions")]
    [SerializeField] private GameObject deathExplosionPrefab;

    [Header("Death Smoke")]
    [SerializeField] private GameObject deathSmokePrefab;

    [Header("Explosion Points (1 = Bow, 2 = Middle, 3 = Stern)")]
    [SerializeField] private Transform bowExplosionPoint;
    [SerializeField] private Transform middleExplosionPoint;
    [SerializeField] private Transform sternExplosionPoint;

    private ShipHealth shipHealth;
    private bool sequenceStarted;

    private void Awake()
    {
        config = GetComponent<ShipConfiguration>().Config;
        shipHealth = GetComponent<ShipHealth>();
    }

    private void OnEnable()
    {
        shipHealth.OnDeath += PlayDeathEffects;

        if (shipHealth.IsDead)
            PlayDeathEffects();
    }

    private void OnDisable()
    {
        shipHealth.OnDeath -= PlayDeathEffects;
    }

    private void PlayDeathEffects()
    {
        if (sequenceStarted)
            return;

        if (deathExplosionPrefab == null || bowExplosionPoint == null ||
            middleExplosionPoint == null || sternExplosionPoint == null)
        {
            Debug.LogError($"{name}: ShipDeathEffects requires a death explosion prefab and all three explosion points.", this);
            return;
        }

        sequenceStarted = true;
        if (deathSmokePrefab == null)
            Debug.LogError($"{name}: ShipDeathEffects requires a death smoke prefab. Smoke will be skipped; explosions will still play.", this);

        Vector3 hitPosition = shipHealth.FinalHitPosition;
        float bowDistance = (bowExplosionPoint.position - hitPosition).sqrMagnitude;
        float middleDistance = (middleExplosionPoint.position - hitPosition).sqrMagnitude;
        float sternDistance = (sternExplosionPoint.position - hitPosition).sqrMagnitude;

        // <= breaks equal-distance ties in favor of the lower-numbered point.
        if (bowDistance <= middleDistance && bowDistance <= sternDistance)
            StartCoroutine(ExplodeInOrder(bowExplosionPoint, middleExplosionPoint, sternExplosionPoint));
        else if (middleDistance <= sternDistance)
            StartCoroutine(ExplodeInOrder(middleExplosionPoint, bowExplosionPoint, sternExplosionPoint));
        else
            StartCoroutine(ExplodeInOrder(sternExplosionPoint, middleExplosionPoint, bowExplosionPoint));
    }

    private IEnumerator ExplodeInOrder(Transform first, Transform second, Transform third)
    {
        var delay = new WaitForSeconds(Mathf.Max(0f, config.DeathEffects.ExplosionDelay));
        SpawnExplosion(first);
        yield return delay;
        SpawnExplosion(second);
        yield return delay;
        SpawnExplosion(third);
    }

    private void SpawnExplosion(Transform point)
    {
        Vector3 position = point.position;
        Quaternion rotation = point.rotation;
        Instantiate(deathExplosionPrefab, position, rotation);
        StartCoroutine(SpawnSmokeAfterDelay(position, rotation));
    }

    private IEnumerator SpawnSmokeAfterDelay(Vector3 position, Quaternion rotation)
    {
        if (deathSmokePrefab == null)
            yield break;

        yield return new WaitForSeconds(Mathf.Max(0f, config.DeathEffects.SmokeDelay));

        if (deathSmokePrefab != null)
        {
            // Retain the prefab's authored orientation and scale at this point.
            Instantiate(deathSmokePrefab, position,
                rotation * deathSmokePrefab.transform.localRotation);
        }
    }
}
