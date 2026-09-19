using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One damage decision per unordered ship pair and separated contact episode.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(ShipHealth), typeof(ShipCollisionResponse))]
[DefaultExecutionOrder(1000)]
public sealed class ShipRammingDamage : MonoBehaviour
{
    [SerializeField] private Transform collisionHull;

    [Header("Ramming Damage")]
    [SerializeField, Min(0f)] private float minimumClosingSpeed = 5f;
    [SerializeField, Min(0f)] private float damageCoefficient = 0.03f;
    [SerializeField, Min(0f)] private float maxDamagePerImpact = 15f;
    [SerializeField, Range(0f, 1f)] private float rammerSelfDamageFraction = 0.25f;

    [Header("Ramming Cooldown")]
    [SerializeField, Min(0f)] private float pairCooldown = 1.5f;
    [SerializeField, Min(0f)] private float requiredSeparation = 0.25f;

    private Rigidbody body;
    private ShipHealth health;

    private Collider[] hullColliders;
    private readonly HashSet<Collider> hullSet = new HashSet<Collider>();

    private Vector3 preImpactVelocity;

    private readonly List<ulong> removalKeys = new List<ulong>();
    private readonly List<PairState> ownedPairs = new List<PairState>();

    private static readonly Dictionary<ulong, PairState> pairs =
        new Dictionary<ulong, PairState>();

    private sealed class PairState
    {
        public ShipRammingDamage a;
        public ShipRammingDamage b;

        public bool episodeConsumed;
        public bool pending;

        public float separatedSince = -1f;
        public float nextAllowedTime;

        public float closingSpeed;
        public float shareA;

        public Vector3 point;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        pairs.Clear();
    }

    private bool Eligible =>
        isActiveAndEnabled &&
        health != null &&
        !health.IsDead &&
        body != null &&
        !body.isKinematic;

    private void OnEnable()
    {
        body = GetComponent<Rigidbody>();
        health = GetComponent<ShipHealth>();

        if (collisionHull == null)
            collisionHull = transform.Find("ShipCollisionHull");

        hullColliders =
            collisionHull == null
                ? new Collider[0]
                : collisionHull.GetComponentsInChildren<Collider>(true);

        hullSet.Clear();

        int layer = LayerMask.NameToLayer("ShipCollision");

        foreach (Collider c in hullColliders)
        {
            if (!c.isTrigger &&
                c.gameObject.layer == layer &&
                c.attachedRigidbody == body)
            {
                hullSet.Add(c);
            }
        }

        preImpactVelocity = body.linearVelocity;

        health.OnDeath += ForgetPairs;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= ForgetPairs;

        ForgetPairs();
    }

    private void ForgetPairs()
    {
        removalKeys.Clear();

        foreach (var entry in pairs)
        {
            if (entry.Value.a == this || entry.Value.b == this)
                removalKeys.Add(entry.Key);
        }

        foreach (ulong key in removalKeys)
            pairs.Remove(key);
    }

    private void FixedUpdate()
    {
        // Work on a snapshot because TakeDamage may synchronously trigger death/cleanup.
        ownedPairs.Clear();

        foreach (PairState pair in pairs.Values)
        {
            if (pair.a == this)
                ownedPairs.Add(pair);
        }

        foreach (PairState pair in ownedPairs)
        {
            if (!pair.a.Eligible || !pair.b.Eligible)
                continue;

            if (pair.pending)
                ApplyPendingDamage(pair);

            if (!pair.a.Eligible || !pair.b.Eligible)
                continue;

            // Require real separation before another impact can damage.
            if (HullsMayTouch(pair.a, pair.b))
            {
                pair.separatedSince = -1f;
            }
            else if (pair.separatedSince < 0f)
            {
                pair.separatedSince = Time.fixedTime;
            }
            else if (
                Time.fixedTime - pair.separatedSince >=
                Mathf.Max(pair.a.requiredSeparation, pair.b.requiredSeparation)
                &&
                Time.fixedTime >= pair.nextAllowedTime)
            {
                pair.episodeConsumed = false;
            }
        }

        preImpactVelocity = body.linearVelocity;
    }

    private static bool HullsMayTouch(
        ShipRammingDamage a,
        ShipRammingDamage b)
    {
        foreach (Collider ca in a.hullSet)
        {
            if (ca == null ||
                !ca.enabled ||
                !ca.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds boundsA = ca.bounds;
            boundsA.Expand(2f * ca.contactOffset);

            foreach (Collider cb in b.hullSet)
            {
                if (cb == null ||
                    !cb.enabled ||
                    !cb.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Bounds boundsB = cb.bounds;
                boundsB.Expand(2f * cb.contactOffset);

                if (boundsA.Intersects(boundsB))
                    return true;
            }
        }

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        RecordImpact(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        RecordImpact(collision);
    }

    private void RecordImpact(Collision collision)
    {
        if (!Eligible || collision.rigidbody == null)
            return;

        ShipRammingDamage other =
            collision.rigidbody.GetComponent<ShipRammingDamage>();

        if (other == null ||
            other == this ||
            !other.Eligible)
        {
            return;
        }

        ShipRammingDamage a =
            GetInstanceID() < other.GetInstanceID()
                ? this
                : other;

        ShipRammingDamage b =
            a == this
                ? other
                : this;

        ulong key =
            ((ulong)(uint)a.GetInstanceID() << 32) |
            (uint)b.GetInstanceID();

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact =
                collision.GetContact(i);

            if (!hullSet.Contains(contact.thisCollider) ||
                !other.hullSet.Contains(contact.otherCollider))
            {
                continue;
            }

            if (!pairs.TryGetValue(key, out PairState pair))
            {
                pair = new PairState
                {
                    a = a,
                    b = b
                };

                pairs.Add(key, pair);
            }

            pair.separatedSince = -1f;

            if (pair.episodeConsumed && !pair.pending)
                continue;

            if (!pair.pending &&
                Time.fixedTime < pair.nextAllowedTime)
            {
                continue;
            }

            Vector3 normal =
                a == this
                    ? -contact.normal
                    : contact.normal;

            normal.y = 0f;

            if (normal.sqrMagnitude < 0.0001f)
                continue;

            normal.Normalize();

            Vector3 va = a.preImpactVelocity;
            va.y = 0f;

            Vector3 vb = b.preImpactVelocity;
            vb.y = 0f;

            float closing =
                Mathf.Max(
                    0f,
                    Vector3.Dot(va - vb, normal)
                );

            if (pair.pending &&
                closing <= pair.closingSpeed)
            {
                continue;
            }

            float approachA =
                Mathf.Max(
                    0f,
                    Vector3.Dot(va, normal)
                );

            float approachB =
                Mathf.Max(
                    0f,
                    Vector3.Dot(vb, -normal)
                );

            float sum =
                approachA + approachB;

            float shareA =
                sum > 0.0001f
                    ? approachA / sum
                    : 0.5f;

            if (!pair.pending)
            {
                // Weak contact does not consume the episode.
                float damageA =
                    CalculateDamage(
                        a,
                        closing,
                        1f - shareA
                    );

                float damageB =
                    CalculateDamage(
                        b,
                        closing,
                        shareA
                    );

                if (damageA <= 0f &&
                    damageB <= 0f)
                {
                    continue;
                }

                pair.episodeConsumed = true;
                pair.pending = true;

                pair.nextAllowedTime =
                    Time.fixedTime +
                    Mathf.Max(
                        a.pairCooldown,
                        b.pairCooldown
                    );
            }

            pair.closingSpeed = closing;
            pair.shareA = shareA;
            pair.point = contact.point;
        }
    }

    private static void ApplyPendingDamage(
        PairState pair)
    {
        pair.pending = false;

        float damageA =
            CalculateDamage(
                pair.a,
                pair.closingSpeed,
                1f - pair.shareA
            );

        float damageB =
            CalculateDamage(
                pair.b,
                pair.closingSpeed,
                pair.shareA
            );

        if (damageA > 0f)
        {
            pair.a.health.TakeDamage(
                damageA,
                pair.point
            );
        }

        if (damageB > 0f)
        {
            pair.b.health.TakeDamage(
                damageB,
                pair.point
            );
        }
    }

    private static float CalculateDamage(
        ShipRammingDamage ship,
        float speed,
        float otherShare)
    {
        float excess =
            Mathf.Max(
                0f,
                speed -
                Mathf.Max(
                    0f,
                    ship.minimumClosingSpeed
                )
            );

        float baseDamage =
            Mathf.Min(
                Mathf.Max(
                    0f,
                    ship.maxDamagePerImpact
                ),
                Mathf.Max(
                    0f,
                    ship.damageCoefficient
                ) *
                excess *
                excess
            );

        return baseDamage *
               Mathf.Lerp(
                   Mathf.Clamp01(
                       ship.rammerSelfDamageFraction
                   ),
                   1f,
                   Mathf.Clamp01(otherShare)
               );
    }
}