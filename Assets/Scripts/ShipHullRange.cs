using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class ShipHullRange : MonoBehaviour
{
    private MeshCollider hullCollider;

    private void Awake()
    {
        hullCollider = GetComponent<MeshCollider>();
    }

    public bool IsWithinHorizontalRange(Vector3 point, float radius)
    {
        if (radius <= 0f || !hullCollider.enabled)
            return false;

        Bounds bounds = hullCollider.bounds;

        Vector3 bottom = new Vector3(point.x, bounds.min.y, point.z);
        Vector3 top = new Vector3(point.x, bounds.max.y, point.z);

        Collider[] hits = Physics.OverlapCapsule(
            bottom,
            top,
            radius,
            1 << gameObject.layer,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (hit == hullCollider)
                return true;
        }

        return false;
    }
}