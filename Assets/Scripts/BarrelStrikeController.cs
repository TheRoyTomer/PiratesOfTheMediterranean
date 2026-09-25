using UnityEngine;
using System.Collections.Generic;

public class BarrelStrikeController : MonoBehaviour
{
    [SerializeField] private BarrelController leftBarrel;
    [SerializeField] private BarrelController midLeftBarrel;
    [SerializeField] private BarrelController midRightBarrel;
    [SerializeField] private BarrelController rightBarrel;
    [SerializeField, Min(0.01f)] private float spreadDuration = 3f;
    [SerializeField, Min(0f)] private float barrelSpacing = 10f;
    [SerializeField, Min(0f)] private float explosionRadius = 7.5f;
    [SerializeField, Min(0f)] private float explosionDamage = 50f;
    [SerializeField, Min(0f)] private float ownerProtectionDuration = 10f;

    private bool hasDetonated;
    private ShipHealth ownerShip;
    private ShipHullRange ownerHull;
    private float ownerProtectionEndTime;
    private bool ownerProtectionActive;
    
    private bool spreadStarted;
    private bool spreadFinished;
    private float spreadElapsed;
    private Vector3 spreadCenter;
    private Vector3 rowDirection;
    private Vector3 leftStart;
    private Vector3 midLeftStart;
    private Vector3 midRightStart;
    private Vector3 rightStart;

    public void BeginRelease(Vector3 shipVelocity, ShipHealth releasingShip)
    {
        ownerShip = releasingShip;
        ownerHull = releasingShip != null
            ? releasingShip.GetComponentInChildren<ShipHullRange>()
            : null;
        ownerProtectionEndTime = Time.time + ownerProtectionDuration;
        ownerProtectionActive = releasingShip != null && ownerProtectionDuration > 0f;

        Vector3 outwardDirection = transform.right;

        leftBarrel.BeginRoll(outwardDirection, shipVelocity);
        midLeftBarrel.BeginRoll(outwardDirection, shipVelocity);
        midRightBarrel.BeginRoll(outwardDirection, shipVelocity);
        rightBarrel.BeginRoll(outwardDirection, shipVelocity);
    }

    private void LateUpdate()
    {
        UpdateOwnerProtection();

        if (spreadFinished)
        {
            CheckForNearbyShips();
            return;
        }

        if (!spreadStarted)
        {
            if (!leftBarrel.IsFloating || !midLeftBarrel.IsFloating ||
                !midRightBarrel.IsFloating || !rightBarrel.IsFloating)
                return;

            BeginSpread();
        }

        spreadElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(spreadElapsed / spreadDuration);
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

        MoveBarrel(leftBarrel, leftStart, -1.5f * barrelSpacing, easedProgress);
        MoveBarrel(midLeftBarrel, midLeftStart, -0.5f * barrelSpacing, easedProgress);
        MoveBarrel(midRightBarrel, midRightStart, 0.5f * barrelSpacing, easedProgress);
        MoveBarrel(rightBarrel, rightStart, 1.5f * barrelSpacing, easedProgress);

        if (progress >= 1f)
            spreadFinished = true;
    }

    private void UpdateOwnerProtection()
    {
        if (!ownerProtectionActive)
            return;

        if (Time.time >= ownerProtectionEndTime)
        {
            ownerProtectionActive = false;
            return;
        }

        if (ownerHull == null)
            return;

        bool ownerStillInRange =
            ownerHull.IsWithinHorizontalRange(leftBarrel.ExplosionPoint, explosionRadius) ||
            ownerHull.IsWithinHorizontalRange(midLeftBarrel.ExplosionPoint, explosionRadius) ||
            ownerHull.IsWithinHorizontalRange(midRightBarrel.ExplosionPoint, explosionRadius) ||
            ownerHull.IsWithinHorizontalRange(rightBarrel.ExplosionPoint, explosionRadius);

        if (!ownerStillInRange)
            ownerProtectionActive = false;
    }

    private void BeginSpread()
    {
        spreadStarted = true;

        leftStart = leftBarrel.transform.position;
        midLeftStart = midLeftBarrel.transform.position;
        midRightStart = midRightBarrel.transform.position;
        rightStart = rightBarrel.transform.position;

        spreadCenter = (leftStart + midLeftStart + midRightStart + rightStart) / 4f;
        rowDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
    }

    private void MoveBarrel(
        BarrelController barrel,
        Vector3 start,
        float distanceFromCenter,
        float progress)
    {
        Vector3 target = spreadCenter + rowDirection * distanceFromCenter;
        Vector3 position = Vector3.Lerp(start, target, progress);

        position.y = barrel.transform.position.y;
        barrel.transform.position = position;
    }

    private void CheckForNearbyShips()
    {
        ShipHullRange[] shipHulls =
            FindObjectsByType<ShipHullRange>(FindObjectsSortMode.None);

        foreach (ShipHullRange hull in shipHulls)
        {
            ShipHealth ship = hull.GetComponentInParent<ShipHealth>();
            if (ship == null || ship.IsDead)
                continue;

            if (ship == ownerShip && ownerProtectionActive)
                continue;

            if (hull.IsWithinHorizontalRange(leftBarrel.ExplosionPoint, explosionRadius) ||
                hull.IsWithinHorizontalRange(midLeftBarrel.ExplosionPoint, explosionRadius) ||
                hull.IsWithinHorizontalRange(midRightBarrel.ExplosionPoint, explosionRadius) ||
                hull.IsWithinHorizontalRange(rightBarrel.ExplosionPoint, explosionRadius))
            {
                Detonate();
                return;
            }
        }
    }

    public void Detonate()
    {
        if (hasDetonated)
            return;

        if (!leftBarrel.IsFloating || !midLeftBarrel.IsFloating ||
            !midRightBarrel.IsFloating || !rightBarrel.IsFloating)
            return;

        hasDetonated = true;

        leftBarrel.PlayExplosion();
        midLeftBarrel.PlayExplosion();
        midRightBarrel.PlayExplosion();
        rightBarrel.PlayExplosion();
        
        DamageNearbyShips();

        Destroy(gameObject);
    }
    
    private void DamageNearbyShips()
    {
        ShipHullRange[] shipHulls =
            FindObjectsByType<ShipHullRange>(FindObjectsSortMode.None);

        HashSet<ShipHealth> damagedShips = new HashSet<ShipHealth>();

        foreach (ShipHullRange hull in shipHulls)
        {
            if (hull == null)
                continue;

            ShipHealth ship = hull.GetComponentInParent<ShipHealth>();
            if (ship == null || ship.IsDead || ship.CurrentHealth <= 0f)
                continue;

            if (ship == ownerShip && ownerProtectionActive)
                continue;

            Vector3 hitPosition;

            if (hull.IsWithinHorizontalRange(leftBarrel.ExplosionPoint, explosionRadius))
                hitPosition = leftBarrel.ExplosionPoint;
            else if (hull.IsWithinHorizontalRange(midLeftBarrel.ExplosionPoint, explosionRadius))
                hitPosition = midLeftBarrel.ExplosionPoint;
            else if (hull.IsWithinHorizontalRange(midRightBarrel.ExplosionPoint, explosionRadius))
                hitPosition = midRightBarrel.ExplosionPoint;
            else if (hull.IsWithinHorizontalRange(rightBarrel.ExplosionPoint, explosionRadius))
                hitPosition = rightBarrel.ExplosionPoint;
            else
                continue;

            if (damagedShips.Add(ship))
                ship.TakeDamage(explosionDamage, hitPosition);
        }
    }
    
}