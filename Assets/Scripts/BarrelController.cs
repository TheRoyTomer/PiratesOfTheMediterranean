using UnityEngine;
using KWS;

public class BarrelController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float rollSpeed = 5f;
    [SerializeField, Min(0f)] private float stationaryRollSpeed = 6f;
    [SerializeField, Min(0.01f)] private float fullSpeedThreshold = 5f;
    [SerializeField, Min(0.01f)] private float rollDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float barrelRadius = 0.72f;
    [SerializeField, Min(0f)] private float fallGravity = 9.81f;
    [SerializeField] private GameObject waterSplashPrefab;
    [SerializeField, Min(0f)] private float waterSplashLifetime = 8f;
    [SerializeField] private GameObject barrelExplosionPrefab;
    [SerializeField, Min(0f)] private float explosionEffectLifetime = 8f;
    [SerializeField, Min(0f)] private float explosionHeightOffset = 2f;
    [SerializeField, Min(0f)] private float floatBobHeight = 0.05f;
    [SerializeField, Min(0f)] private float floatBobSpeed = 1.2f;
    [SerializeField, Min(0f)] private float immersionDepth = 1f;[SerializeField] private GameObject barrelExplosionSplashPrefab;
    [SerializeField, Min(0f)] private float explosionSplashLifetime = 8f;
    

    private Vector3 rollDirection;
    private Vector3 inheritedVelocity;
    private float currentRollSpeed;
    private float rollTime;
    private bool isRolling;

    private Vector3 fallVelocity;
    private bool isFalling;
    private bool isFloating;
    private float floatingWaterOffset;

    public bool IsFloating => isFloating;

    public Vector3 ExplosionPoint
    {
        get
        {
            Vector3 point = barrelRenderer.bounds.center;
            point.y = lastSurfaceHeight;
            return point;
        }
    }

    private readonly WaterSurfaceRequestPoint waterRequest = new WaterSurfaceRequestPoint();
    private MeshRenderer barrelRenderer;
    private float lastSurfaceHeight;
    private bool hasSurfaceSample;

    private void Awake()
    {
        barrelRenderer = GetComponent<MeshRenderer>();
    }

    public void BeginRoll(Vector3 outwardDirection, Vector3 shipVelocity)
    {
        rollDirection = outwardDirection.normalized;
        inheritedVelocity = shipVelocity;
        currentRollSpeed = Mathf.Lerp(
            stationaryRollSpeed,
            rollSpeed,
            Mathf.Clamp01(shipVelocity.magnitude / fullSpeedThreshold)
        );
        rollTime = 0f;
        isRolling = true;
    }

    private void Update()
    {
        if (isFalling)
        {
            fallVelocity += Vector3.down * fallGravity * Time.deltaTime;
            transform.position += fallVelocity * Time.deltaTime;
            TryEnterWater();
            return;
        }

        if (isFloating)
        {
            TryEnterWater();
            if (hasSurfaceSample)
            {
                Vector3 position = transform.position;
                float bob = Mathf.Sin(Time.time * floatBobSpeed +
                                      transform.GetSiblingIndex()) * floatBobHeight;
                float targetY = lastSurfaceHeight + floatingWaterOffset + bob - immersionDepth;
                position.y = Mathf.Lerp(
                    position.y,
                    targetY,
                    1f - Mathf.Exp(-8f * Time.deltaTime)
                );
                transform.position = position;
            }

            return;
        }

        if (!isRolling)
            return;

        float step = Mathf.Min(Time.deltaTime, rollDuration - rollTime);
        Vector3 axis = transform.forward;
        Vector3 center = GetComponent<MeshFilter>().sharedMesh.bounds.center;

        transform.position += (rollDirection * currentRollSpeed + inheritedVelocity) * step;
        transform.RotateAround(
            transform.TransformPoint(center),
            axis,
            currentRollSpeed / barrelRadius * Mathf.Rad2Deg * step
        );

        rollTime += step;
        if (rollTime >= rollDuration)
        {
            isRolling = false;
            isFalling = true;
            fallVelocity = rollDirection * currentRollSpeed + inheritedVelocity;
        }
    }

    private void TryEnterWater()
    {
        WaterSystem water = WaterSystem.Instance;
        if (water != null && water.isActiveAndEnabled)
        {
            waterRequest.SetNewPosition(barrelRenderer.bounds.center);
            WaterSystem.TryGetWaterSurfaceData(waterRequest);

            if (waterRequest.IsDataReady && waterRequest.Result.HasWater != 0)
            {
                float height = waterRequest.Result.Position.y;
                if (!float.IsNaN(height) && !float.IsInfinity(height))
                {
                    lastSurfaceHeight = height;
                    hasSurfaceSample = true;
                }
            }
        }

        if (!hasSurfaceSample || isFloating || barrelRenderer.bounds.min.y > lastSurfaceHeight)
            return;

        isFalling = false;
        isFloating = true;
        floatingWaterOffset = transform.position.y - lastSurfaceHeight;

        if (waterSplashPrefab != null)
        {
            Vector3 splashPosition = barrelRenderer.bounds.center;
            splashPosition.y = lastSurfaceHeight;
            GameObject splash = Instantiate(
                waterSplashPrefab,
                splashPosition,
                Quaternion.identity
            );
            Destroy(splash, waterSplashLifetime);
        }
    }

    public void PlayExplosion()
    {
        if (barrelExplosionPrefab != null)
        {
            GameObject effect = Instantiate(
                barrelExplosionPrefab,
                ExplosionPoint + Vector3.up * explosionHeightOffset,
                Quaternion.identity
            );

            Destroy(effect, explosionEffectLifetime);
        }

        if (barrelExplosionSplashPrefab != null)
        {
            GameObject splash = Instantiate(
                barrelExplosionSplashPrefab,
                ExplosionPoint,
                Quaternion.identity
            );

            Destroy(splash, explosionSplashLifetime);
        }
    }
}