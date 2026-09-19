using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/// <summary>Contact-local stabilization for dedicated ship collision hulls only.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(ShipHealth))]
public sealed class ShipCollisionResponse : MonoBehaviour
{
    [SerializeField] private Transform collisionHull;
    private Rigidbody body;
    private ShipHealth health;
    private bool lastEligible;
    private readonly Dictionary<Collider, bool> previousFlags = new Dictionary<Collider, bool>();
    private static readonly HashSet<ShipCollisionResponse> instances = new HashSet<ShipCollisionResponse>();

    // Replace snapshots on the main thread; physics workers never access Unity objects.
    private static volatile Dictionary<int, bool> contactEligibility = new Dictionary<int, bool>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        Physics.ContactModifyEvent -= Modify;
        Physics.ContactModifyEventCCD -= Modify;
        instances.Clear();
        contactEligibility = new Dictionary<int, bool>();
    }

    private void OnEnable()
    {
        body = GetComponent<Rigidbody>();
        health = GetComponent<ShipHealth>();
        if (collisionHull == null) collisionHull = transform.Find("ShipCollisionHull");
        if (collisionHull == null) return;
        int layer = LayerMask.NameToLayer("ShipCollision");
        foreach (var c in collisionHull.GetComponentsInChildren<Collider>(true))
        {
            if (c.isTrigger || c.gameObject.layer != layer || c.attachedRigidbody != body) continue;
            previousFlags[c] = c.hasModifiableContacts;
            c.hasModifiableContacts = true;
        }
        if (instances.Count == 0)
        {
            Physics.ContactModifyEvent += Modify;
            Physics.ContactModifyEventCCD += Modify;
        }
        instances.Add(this);
        health.OnDeath += Publish;
        Publish();
    }

    private void FixedUpdate()
    {
        bool eligible = health != null && !health.IsDead && !body.isKinematic;
        if (eligible != lastEligible) Publish();
    }

    private void OnDisable()
    {
        if (health != null) health.OnDeath -= Publish;
        instances.Remove(this);
        foreach (var entry in previousFlags)
            if (entry.Key != null) entry.Key.hasModifiableContacts = entry.Value;
        previousFlags.Clear();
        if (instances.Count == 0)
        {
            Physics.ContactModifyEvent -= Modify;
            Physics.ContactModifyEventCCD -= Modify;
        }
        Publish();
    }

    private static void Publish()
    {
        var snapshot = new Dictionary<int, bool>();
        foreach (var instance in instances)
        {
            if (instance == null) continue;
            bool eligible = !instance.health.IsDead && !instance.body.isKinematic;
            instance.lastEligible = eligible;
            foreach (var entry in instance.previousFlags)
                if (entry.Key != null)
                    snapshot[entry.Key.GetInstanceID()] = eligible && entry.Key.enabled && entry.Key.gameObject.activeInHierarchy;
        }
        contactEligibility = snapshot;
    }

    private static void Modify(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
    {
        var snapshot = contactEligibility;
        for (int i = 0; i < pairs.Length; i++)
        {
            var pair = pairs[i];
            if (!snapshot.TryGetValue(pair.colliderInstanceID, out bool first) ||
                !snapshot.TryGetValue(pair.otherColliderInstanceID, out bool second)) continue;

            // Dedicated hulls do not obstruct scripted sinking; shoreline colliders are untouched.
            if (!first || !second)
            {
                for (int j = 0; j < pair.contactCount; j++) pair.IgnoreContact(j);
                continue;
            }
            var mass = pair.massProperties;
            mass.inverseInertiaScale = 0f;
            mass.otherInverseInertiaScale = 0f;
            pair.massProperties = mass;
            for (int j = 0; j < pair.contactCount; j++)
            {
                var normal = pair.GetNormal(j);
                normal.y = 0f;
                if (normal.sqrMagnitude < 0.0001f) { pair.IgnoreContact(j); continue; }
                pair.SetNormal(j, normal.normalized);
                pair.SetStaticFriction(j, 0f);
                pair.SetDynamicFriction(j, 0f);
                pair.SetBounciness(j, 0f);
            }
        }
    }
}
