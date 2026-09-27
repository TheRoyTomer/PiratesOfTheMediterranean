using System.Collections.Generic;
using UnityEngine;

public class MinimapController : MonoBehaviour
{
    [Header("Map Image")]
    [SerializeField] private RectTransform minimapBackground;

    [Header("Marker Prefabs")]
    [SerializeField] private RectTransform playerMarkerPrefab;
    [SerializeField] private RectTransform enemyMarkerPrefab;
    [SerializeField] private RectTransform barrelsMarkerPrefab;
    [SerializeField] private RectTransform shipPartsMarkerPrefab;

    private const float MinX = -2231f;
    private const float MaxX = 2769f;
    private const float MinZ = -2495f;
    private const float MaxZ = 2505f;

    private PlayerInputController player;
    private ShipHealth playerHealth;
    private RectTransform playerMarker;
    private float nextEnemyScan;

    private class EnemyEntry
    {
        public AIController Ship;
        public ShipHealth Health;
        public RectTransform Marker;
    }

    private readonly List<EnemyEntry> enemies = new();
    private readonly HashSet<AIController> trackedEnemies = new();

    private class BarrelEntry
    {
        public BarrelAmmoPickupController Pickup;
        public PickupRingVisual Ring;
        public LineRenderer RingLine;
        public RectTransform Marker;
    }

    private readonly List<BarrelEntry> barrels = new();

    private class ShipPartsEntry
    {
        public ShipPartsController Pickup;
        public PickupRingVisual Ring;
        public LineRenderer RingLine;
        public RectTransform Marker;
    }

    private readonly List<ShipPartsEntry> shipParts = new();

    private void Start()
    {
        if (minimapBackground == null ||
            playerMarkerPrefab == null ||
            enemyMarkerPrefab == null)
        {
            Debug.LogError(
                "MinimapController: Assign the background, player and enemy prefabs.",
                this
            );

            enabled = false;
            return;
        }

        playerMarker = CreateMarker(playerMarkerPrefab);
    }

    private RectTransform CreateMarker(RectTransform prefab)
    {
        RectTransform marker = Instantiate(
            prefab,
            minimapBackground,
            false
        );

        marker.anchorMin = minimapBackground.pivot;
        marker.anchorMax = minimapBackground.pivot;
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.localScale = Vector3.one;
        marker.gameObject.SetActive(false);

        return marker;
    }

    private void LateUpdate()
    {
        UpdatePlayer();

        if (Time.time >= nextEnemyScan)
        {
            nextEnemyScan = Time.time + 0.5f;
            DiscoverEnemies();
            DiscoverBarrels();
            DiscoverShipParts();
        }

        UpdateEnemies();
        UpdateBarrels();
        UpdateShipParts();

        // Keep the player visible above overlapping enemy markers.
        playerMarker.SetAsLastSibling();
    }

    private void UpdatePlayer()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerInputController>(
                FindObjectsInactive.Include
            );

            if (player != null)
                playerHealth = player.GetComponent<ShipHealth>();
        }

        bool visible =
            player != null &&
            player.gameObject.activeInHierarchy &&
            playerHealth != null &&
            !playerHealth.IsDead;

        playerMarker.gameObject.SetActive(visible);

        if (visible)
            UpdateShipMarker(playerMarker, player.transform);
    }

    private void DiscoverEnemies()
    {
        AIController[] ships = FindObjectsByType<AIController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (AIController ship in ships)
        {
            if (trackedEnemies.Contains(ship))
                continue;

            ShipHealth health = ship.GetComponent<ShipHealth>();

            if (health == null || health.IsDead)
                continue;

            enemies.Add(new EnemyEntry
            {
                Ship = ship,
                Health = health,
                Marker = CreateMarker(enemyMarkerPrefab)
            });

            trackedEnemies.Add(ship);
        }
    }

    private void UpdateEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyEntry entry = enemies[i];

            if (entry.Ship == null ||
                entry.Health == null ||
                entry.Health.IsDead)
            {
                entry.Marker.gameObject.SetActive(false);
                Destroy(entry.Marker.gameObject);
                trackedEnemies.Remove(entry.Ship);
                enemies.RemoveAt(i);
                continue;
            }

            bool visible = entry.Ship.gameObject.activeInHierarchy;
            entry.Marker.gameObject.SetActive(visible);

            if (visible)
                UpdateShipMarker(entry.Marker, entry.Ship.transform);
        }
    }

    private void UpdateShipMarker(RectTransform marker, Transform ship)
    {
        marker.anchoredPosition = WorldToMapPosition(ship.position);
        marker.localRotation = Quaternion.Euler(
            0f,
            0f,
            WorldToMapAngle(ship)
        );
    }

    private void DiscoverBarrels()
    {
        if (barrelsMarkerPrefab == null)
            return;

        BarrelAmmoPickupController[] pickups =
            FindObjectsByType<BarrelAmmoPickupController>(
                FindObjectsSortMode.None
            );

        foreach (BarrelAmmoPickupController pickup in pickups)
        {
            bool alreadyTracked = barrels.Exists(
                entry => entry.Pickup == pickup
            );

            if (alreadyTracked)
                continue;

            PickupRingVisual ring =
                pickup.GetComponentInChildren<PickupRingVisual>();

            if (ring == null)
                continue;

            RectTransform marker = CreateMarker(barrelsMarkerPrefab);
            marker.localRotation = Quaternion.identity;

            barrels.Add(new BarrelEntry
            {
                Pickup = pickup,
                Ring = ring,
                RingLine = ring.GetComponent<LineRenderer>(),
                Marker = marker
            });
        }
    }

    private void UpdateBarrels()
    {
        for (int i = barrels.Count - 1; i >= 0; i--)
        {
            BarrelEntry entry = barrels[i];

            if (entry.Pickup == null || entry.Ring == null)
            {
                entry.Marker.gameObject.SetActive(false);
                Destroy(entry.Marker.gameObject);
                barrels.RemoveAt(i);
                continue;
            }

            bool visible =
                entry.Pickup.gameObject.activeInHierarchy &&
                entry.Ring.gameObject.activeInHierarchy &&
                entry.RingLine != null &&
                entry.RingLine.enabled;

            entry.Marker.gameObject.SetActive(visible);

            if (visible)
            {
                entry.Marker.anchoredPosition =
                    WorldToMapPosition(entry.Ring.WorldCenter);
            }
        }
    }

    private void DiscoverShipParts()
    {
        if (shipPartsMarkerPrefab == null)
            return;

        ShipPartsController[] pickups =
            FindObjectsByType<ShipPartsController>(
                FindObjectsSortMode.None
            );

        foreach (ShipPartsController pickup in pickups)
        {
            bool alreadyTracked = shipParts.Exists(
                entry => entry.Pickup == pickup
            );

            if (alreadyTracked)
                continue;

            PickupRingVisual ring =
                pickup.GetComponentInChildren<PickupRingVisual>();

            if (ring == null)
                continue;

            LineRenderer ringLine = ring.GetComponent<LineRenderer>();
            RectTransform marker = CreateMarker(shipPartsMarkerPrefab);
            marker.localRotation = Quaternion.identity;

            shipParts.Add(new ShipPartsEntry
            {
                Pickup = pickup,
                Ring = ring,
                RingLine = ringLine,
                Marker = marker
            });
        }
    }

    private void UpdateShipParts()
    {
        for (int i = shipParts.Count - 1; i >= 0; i--)
        {
            ShipPartsEntry entry = shipParts[i];

            if (entry.Pickup == null || entry.Ring == null)
            {
                entry.Marker.gameObject.SetActive(false);
                Destroy(entry.Marker.gameObject);
                shipParts.RemoveAt(i);
                continue;
            }

            bool visible =
                entry.Pickup.gameObject.activeInHierarchy &&
                entry.Ring.gameObject.activeInHierarchy &&
                entry.RingLine != null &&
                entry.RingLine.enabled;

            entry.Marker.gameObject.SetActive(visible);

            if (visible)
            {
                entry.Marker.anchoredPosition =
                    WorldToMapPosition(entry.Ring.WorldCenter);
            }
        }
    }

    private Vector2 WorldToMapPosition(Vector3 worldPosition)
    {
        // Image right = world -Z. Image up = world +X.
        float u = (MaxZ - worldPosition.z) / (MaxZ - MinZ);
        float v = (worldPosition.x - MinX) / (MaxX - MinX);

        Rect rect = minimapBackground.rect;

        return new Vector2(
            rect.xMin + u * rect.width,
            rect.yMin + v * rect.height
        );
    }

    private float WorldToMapAngle(Transform ship)
    {
        Vector3 forward = ship.forward;
        return Mathf.Atan2(forward.z, forward.x) * Mathf.Rad2Deg;
    }

    private void OnDestroy()
    {
        if (playerMarker != null)
            Destroy(playerMarker.gameObject);

        foreach (EnemyEntry entry in enemies)
        {
            if (entry.Marker != null)
                Destroy(entry.Marker.gameObject);
        }

        foreach (BarrelEntry entry in barrels)
        {
            if (entry.Marker != null)
                Destroy(entry.Marker.gameObject);
        }

        foreach (ShipPartsEntry entry in shipParts)
        {
            if (entry.Marker != null)
                Destroy(entry.Marker.gameObject);
        }
    }
}
