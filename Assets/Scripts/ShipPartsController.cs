using UnityEngine;

public class ShipPartsController : MonoBehaviour
{
    [SerializeField] private float riseDepth = 8f;
    [SerializeField] private float riseDuration = 1.5f;
    [SerializeField] private float visibleLifetime = 60f;
    [SerializeField] private float sinkingDepth = 8f;
    [SerializeField] private float sinkingDuration = 1.5f;

    private PickupRingVisual ring;
    private FloatingPart[] parts;
    
    private enum Phase
    {
        Idle,
        Rising,
        Available,
        Sinking
    }

    private Phase phase;
    private float phaseEndTime;
    private PlayerShipParts playerInventory;
    private Collider playerHull;
    private bool collected;

    private void Awake()
    {
        ring = GetComponentInChildren<PickupRingVisual>();
        parts = GetComponentsInChildren<FloatingPart>();
    }
    
    public void BeginPickup()
    {
        ring.Hide();

        foreach (FloatingPart part in parts)
            part.BeginEmergence(riseDepth, riseDuration);

        phase = Phase.Rising;
        phaseEndTime = Time.time + riseDuration;
    }

    private void Update()
    {
        if (phase == Phase.Idle || Time.time < phaseEndTime)
            return;

        switch (phase)
        {
            case Phase.Rising:
                ring.Show();
                phase = Phase.Available;
                phaseEndTime = Time.time + visibleLifetime;
                break;

            case Phase.Available:
                ring.Hide();

                foreach (FloatingPart part in parts)
                    part.BeginSinking(sinkingDepth, sinkingDuration);

                phase = Phase.Sinking;
                phaseEndTime = Time.time + sinkingDuration;
                break;

            case Phase.Sinking:
                Destroy(gameObject);
                break;
        }
    }
    
    private void LateUpdate()
    {
        if (phase != Phase.Available || collected || Time.time >= phaseEndTime)
            return;

        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerShipParts>();
            if (playerInventory == null)
                return;
        }

        if (playerHull == null)
        {
            Transform hull = playerInventory.transform.Find("PhysicalHull");
            if (hull == null)
                return;

            playerHull = hull.GetComponent<Collider>();
            if (playerHull == null)
                return;
        }

        Bounds hullBounds = playerHull.bounds;
        Vector3 center = ring.WorldCenter;

        Vector3 bottom = new Vector3(center.x, hullBounds.min.y, center.z);
        Vector3 top = new Vector3(center.x, hullBounds.max.y, center.z);

        Collider[] overlaps = Physics.OverlapCapsule(
            bottom,
            top,
            ring.WorldRadius,
            LayerMask.GetMask("ShipPhysical"),
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider overlap in overlaps)
        {
            if (overlap != playerHull)
                continue;

            if (phase != Phase.Available || Time.time >= phaseEndTime)
                return;

            collected = true;
            ring.Hide();
            playerInventory.AddShipPart();
            Destroy(gameObject);
            return;
        }
    }
}