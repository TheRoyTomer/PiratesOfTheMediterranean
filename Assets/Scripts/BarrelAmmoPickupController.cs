using UnityEngine;

public class BarrelAmmoPickupController : MonoBehaviour
{
    [SerializeField] private float riseDepth = 8f;
    [SerializeField] private float riseDuration = 1.5f;
    [SerializeField] private float visibleLifetime = 90f;
    [SerializeField] private float sinkingDepth = 8f;
    [SerializeField] private float sinkingDuration = 1.5f;

    private enum Phase { Idle, Rising, Available, Sinking }

    private Phase phase;
    private float phaseEndTime;
    private FloatingPart[] parts;
    private PickupRingVisual ring;
    private PlayerInputController playerController;
    private BarrelAmmo barrelAmmo;
    private Collider playerHull;
    private ShipHealth playerHealth;
    private bool collected;

    private void Awake()
    {
        ring = GetComponentInChildren<PickupRingVisual>();
        parts = GetComponentsInChildren<FloatingPart>();
        ring.Hide();
    }

    public void BeginPickup()
    {
        if (phase != Phase.Idle)
            return;

        ring.Hide();
        foreach (FloatingPart part in parts)
        {
            part.BeginEmergence(riseDepth, riseDuration);
            // Start submerged before the first render; FloatingPart rebuilds
            // its authored anchor independently on subsequent frames.
            part.transform.position -= Vector3.up * riseDepth;
        }

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
        if (phase != Phase.Available || Time.time >= phaseEndTime ||
            collected || ring == null)
            return;

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerInputController>();
            if (playerController == null)
                return;

            barrelAmmo = playerController.GetComponent<BarrelAmmo>();
            playerHealth = playerController.GetComponent<ShipHealth>();
        }

        if (barrelAmmo == null || playerHealth == null ||
            playerHealth.IsDead || playerHealth.CurrentHealth <= 0f)
            return;

        if (playerHull == null)
        {
            Transform hull = playerController.transform.Find("PhysicalHull");
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

            collected = true;
            ring.Hide();
            barrelAmmo.Add(1);
            GameAudio.Play(GameSound.Pickup);
            Destroy(gameObject);
            return;
        }
    }
}
