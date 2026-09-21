using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/// <summary>Shoreline-only, torque-free horizontal contact response.</summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class ShipShorelineResponse : MonoBehaviour
{
    [Tooltip("Optional scene-specific boundaries. When empty, use all shoreline paths in this ship's scene.")]
    public List<ShorelineBoundaryPath> boundaries = new List<ShorelineBoundaryPath>();
    Rigidbody body;
    // Published once on enable, then read only by the physics worker callback.
    HashSet<int> hullIds;
    HashSet<int> wallIds;
    readonly Dictionary<Collider, bool> previousFlags = new Dictionary<Collider, bool>();

    void OnEnable()
    {
        body = GetComponent<Rigidbody>();
        hullIds = new HashSet<int>(); wallIds = new HashSet<int>();
        var resolvedBoundaries = boundaries;
        if (resolvedBoundaries == null || resolvedBoundaries.Count == 0)
        {
            // Prefab assets cannot reference scene objects. Resolve once per enable,
            // including inactive paths just as explicit scene references would.
            resolvedBoundaries = new List<ShorelineBoundaryPath>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
                resolvedBoundaries.AddRange(root.GetComponentsInChildren<ShorelineBoundaryPath>(true));
        }
        if (resolvedBoundaries.Count == 0) return;
        int layer = LayerMask.NameToLayer("ShipPhysical");
        foreach (var c in GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger || c.gameObject.layer != layer || c.attachedRigidbody != body) continue;
            hullIds.Add(c.GetInstanceID()); previousFlags[c] = c.hasModifiableContacts;
            c.hasModifiableContacts = true;
        }
        int wallLayer = LayerMask.NameToLayer("ShorelineBoundary");
        foreach (var boundary in resolvedBoundaries)
        {
            if (!boundary) continue;
            foreach (var c in boundary.GetComponentsInChildren<Collider>())
                if (!c.isTrigger && c.gameObject.layer == wallLayer) wallIds.Add(c.GetInstanceID());
        }
        Physics.ContactModifyEvent += Modify;
        Physics.ContactModifyEventCCD += Modify;
    }

    void OnDisable()
    {
        Physics.ContactModifyEvent -= Modify;
        Physics.ContactModifyEventCCD -= Modify;
        foreach (var entry in previousFlags) if (entry.Key) entry.Key.hasModifiableContacts = entry.Value;
        previousFlags.Clear();
        // Do not mutate the ID sets: an already-dispatched worker may still read them.
    }

    void Modify(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
    {
        // No Unity object access here: Unity can invoke this on a physics worker.
        for (int i = 0; i < pairs.Length; i++)
        {
            var pair = pairs[i];
            bool first = hullIds.Contains(pair.colliderInstanceID) && wallIds.Contains(pair.otherColliderInstanceID);
            bool second = hullIds.Contains(pair.otherColliderInstanceID) && wallIds.Contains(pair.colliderInstanceID);
            if (!first && !second) continue;
            var mass = pair.massProperties;
            // Disable angular impulse only for this contact pair, not the Rigidbody.
            if (first) mass.inverseInertiaScale = 0; else mass.otherInverseInertiaScale = 0;
            pair.massProperties = mass;
            for (int j = 0; j < pair.contactCount; j++)
            {
                var n = pair.GetNormal(j); n.y = 0;
                if (n.sqrMagnitude < .0001f) { pair.IgnoreContact(j); continue; }
                pair.SetNormal(j, n.normalized);
                pair.SetBounciness(j, 0);
                pair.SetStaticFriction(j, 0);
                pair.SetDynamicFriction(j, 0);
            }
        }
    }

    void OnCollisionEnter(Collision collision) => Resolve(collision);
    void OnCollisionStay(Collision collision) => Resolve(collision);

    void Resolve(Collision collision)
    {
        bool shoreline = false;
        var velocity = body.linearVelocity;
        float vertical = velocity.y;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            bool first = hullIds.Contains(contact.thisCollider.GetInstanceID()) && wallIds.Contains(contact.otherCollider.GetInstanceID());
            bool second = hullIds.Contains(contact.otherCollider.GetInstanceID()) && wallIds.Contains(contact.thisCollider.GetInstanceID());
            if (!first && !second) continue;
            var normal = first ? contact.normal : -contact.normal; normal.y = 0;
            if (normal.sqrMagnitude < .0001f) continue;
            normal.Normalize(); shoreline = true;
            float inward = Vector3.Dot(velocity, normal);
            if (inward < 0) velocity -= normal * inward;
        }
        if (!shoreline) return;
        velocity.y = vertical;
        body.linearVelocity = velocity;
        // Contact-local stabilization only; leave yaw and all free-sailing rotation alone.
        var angular = body.angularVelocity;
        body.angularVelocity = new Vector3(0, angular.y, 0);
    }
}
