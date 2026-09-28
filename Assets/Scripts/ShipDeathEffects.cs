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

    [Header("Death Fire")]
    [UnityEngine.Serialization.FormerlySerializedAs("deathSmokePrefab")]
    [SerializeField] private GameObject deathFirePrefab;

    [Header("Explosion Points (1 = Bow, 2 = Middle, 3 = Stern)")]
    [SerializeField] private Transform bowExplosionPoint;
    [SerializeField] private Transform middleExplosionPoint;
    [SerializeField] private Transform sternExplosionPoint;

    private ShipHealth shipHealth;
    private bool sequenceStarted;
    private readonly GameObject[] deathExplosions = new GameObject[3];
    private readonly GameObject[] deathFires = new GameObject[3];

    public bool ExplosionPhaseFinished { get; private set; }
    public bool HasFinalExplosionStarted { get; private set; }
    public event System.Action FinalExplosionStarted;

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
        StopAllCoroutines();
        DestroyEffects(deathFires);
        DestroyEffects(deathExplosions);
    }

    private void PlayDeathEffects()
    {
        if (sequenceStarted)
            return;

        if (deathExplosionPrefab == null || bowExplosionPoint == null ||
            middleExplosionPoint == null || sternExplosionPoint == null)
        {
            Debug.LogError($"{name}: ShipDeathEffects requires a death explosion prefab and all three explosion points.", this);
            ExplosionPhaseFinished = true;
            SignalFinalExplosionStarted();
            return;
        }

        sequenceStarted = true;
        if (deathFirePrefab == null)
            Debug.LogError($"{name}: ShipDeathEffects requires a death fire prefab. Fire will be skipped; explosions will still play.", this);

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
        var points = new[] { first, second, third };
        var positions = new Vector3[3];
        var rotations = new Quaternion[3];
        var explosions = deathExplosions;
        var delay = new WaitForSeconds(Mathf.Max(0f, config.DeathEffects.ExplosionDelay));
        for (int i = 0; i < points.Length; i++)
        {
            positions[i] = points[i].position;
            rotations[i] = points[i].rotation;
            explosions[i] = Instantiate(deathExplosionPrefab, positions[i], rotations[i]);
            GameAudio.Play(GameSound.HeavyExplosion, positions[i], 0.8f);
            if (i == points.Length - 1)
                SignalFinalExplosionStarted();
            if (deathFirePrefab != null)
                StartCoroutine(SpawnFireAfterExplosion(points[i], i));
            if (i < points.Length - 1)
                yield return delay;
        }

        // Let newly spawned systems initialize; include surviving child particles.
        yield return null;
        while (EffectsAreAlive(explosions))
            yield return null;

        DestroyEffects(explosions);
        ExplosionPhaseFinished = true;
    }

    private IEnumerator SpawnFireAfterExplosion(Transform point, int index)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, config.DeathEffects.FireStartAfterExplosion));

        if (deathFirePrefab == null || point == null ||
            !point.gameObject.activeInHierarchy)
            yield break;

        GameObject fire = Instantiate(
            deathFirePrefab,
            point.position,
            point.rotation * deathFirePrefab.transform.localRotation
        );

        fire.transform.SetParent(point, true);
        deathFires[index] = fire;
    }

    private static void DestroyEffects(GameObject[] effects)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] != null)
                Destroy(effects[i]);

            effects[i] = null;
        }
    }

    private void SignalFinalExplosionStarted()
    {
        HasFinalExplosionStarted = true;
        FinalExplosionStarted?.Invoke();
    }

    private static bool EffectsAreAlive(GameObject[] effects)
    {
        foreach (var effect in effects)
        {
            // AutoDestruct may already have destroyed or deactivated the effect.
            if (effect == null || !effect.activeInHierarchy)
                continue;
            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>())
                if (particles.IsAlive(true))
                    return true;
        }
        return false;
    }
}
