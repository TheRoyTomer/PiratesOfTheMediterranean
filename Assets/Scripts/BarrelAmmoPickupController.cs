using UnityEngine;

public class BarrelAmmoPickupController : MonoBehaviour
{
    private PickupRingVisual ring;
    private PlayerInputController playerController;
    private BarrelAmmo barrelAmmo;
    private Collider playerHull;
    private ShipHealth playerHealth;
    private bool collected;

    private void Awake()
    {
        ring = GetComponentInChildren<PickupRingVisual>();
    }

    private void LateUpdate()
    {
        if (collected || ring == null)
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
            barrelAmmo.Add(1);
            Destroy(gameObject);
            return;
        }
    }
}