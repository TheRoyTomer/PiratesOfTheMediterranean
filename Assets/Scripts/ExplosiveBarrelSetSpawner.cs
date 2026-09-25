using UnityEngine;
using System.Collections;

public class ExplosiveBarrelSetSpawner : MonoBehaviour
{
    [SerializeField]
    private BarrelAmmoPickupController pickupPrefab;

    [Tooltip(
        "Candidate ring-center markers. Assign existing patrol-point " +
        "transforms and separate barrel-only transforms.")]
    [SerializeField]
    private Transform[] spawnPoints = new Transform[0];

    [SerializeField, Min(0f)]
    private float shipClearance = 150f;

    [SerializeField, Range(0f, 100f)]
    private float spawnChancePercent = 17f;

    private IEnumerator Start()
    {
        while (!TrySpawnOne())
            yield return new WaitForSeconds(2f);

        while (true)
        {
            yield return new WaitForSeconds(30f);

            int activePickupCount =
                FindObjectsByType<BarrelAmmoPickupController>(
                    FindObjectsSortMode.None).Length;

            if (activePickupCount >= 2)
                continue;

            if (Random.value < spawnChancePercent / 100f)
                TrySpawnOne();
        }
    }

    private bool IsFarEnoughFromShips(Vector3 ringCenter, float pickupRadius)
    {
        ShipHullRange[] shipHulls =
            FindObjectsByType<ShipHullRange>(FindObjectsSortMode.None);

        foreach (ShipHullRange hull in shipHulls)
        {
            ShipHealth ship = hull.GetComponentInParent<ShipHealth>();
            if (ship == null || ship.IsDead || ship.CurrentHealth <= 0f)
                continue;

            if (hull.IsWithinHorizontalRange(
                    ringCenter,
                    pickupRadius + shipClearance))
                return false;
        }

        return true;
    }

    private Transform ChoosePointAwayFromShips(float pickupRadius)
    {
        BarrelAmmoPickupController[] activePickups =
            FindObjectsByType<BarrelAmmoPickupController>(FindObjectsSortMode.None);

        Transform chosenPoint = null;
        int eligibleCount = 0;

        foreach (Transform point in spawnPoints)
        {
            if (point == null ||
                !IsFarEnoughFromShips(point.position, pickupRadius) ||
                IsPointOccupied(point, activePickups))
                continue;

            eligibleCount++;

            if (Random.Range(0, eligibleCount) == 0)
                chosenPoint = point;
        }

        return chosenPoint;
    }

    private bool IsPointOccupied(
        Transform point,
        BarrelAmmoPickupController[] activePickups)
    {
        foreach (BarrelAmmoPickupController pickup in activePickups)
        {
            PickupRingVisual ring = pickup.GetComponentInChildren<PickupRingVisual>();
            if (ring == null)
                continue;

            Vector3 center = ring.WorldCenter;
            float xDifference = center.x - point.position.x;
            float zDifference = center.z - point.position.z;

            if (xDifference * xDifference + zDifference * zDifference < 1f)
                return true;
        }

        return false;
    }

    private bool TrySpawnOne()
    {
        if (pickupPrefab == null)
            return false;

        BarrelAmmoPickupController[] activePickups =
            FindObjectsByType<BarrelAmmoPickupController>(FindObjectsSortMode.None);

        if (activePickups.Length >= 2)
            return false;

        PickupRingVisual prefabRing =
            pickupPrefab.GetComponentInChildren<PickupRingVisual>();

        if (prefabRing == null)
        {
            Debug.LogError("Barrel pickup prefab is missing PickupRingVisual.", this);
            return false;
        }

        Transform point = ChoosePointAwayFromShips(prefabRing.WorldRadius);
        if (point == null)
            return false;

        Vector3 ringOffset =
            pickupPrefab.transform.TransformVector(prefabRing.transform.localPosition);

        Vector3 rootPosition = new Vector3(
            point.position.x - ringOffset.x,
            0f,
            point.position.z - ringOffset.z
        );

        Instantiate(pickupPrefab, rootPosition, pickupPrefab.transform.rotation);
        return true;
    }
}