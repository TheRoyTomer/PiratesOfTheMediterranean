using KWS;
using UnityEngine;

public class FloatingShipPart : MonoBehaviour
{
    [Tooltip("Residual bob amplitude in world units, independent of parent scale.")]
    [SerializeField, Min(0f)] private float bobHeight = 0.05f;
    [SerializeField] private float bobSpeed = 1.2f;
    [SerializeField] private float tiltAngle = 2f;
    [SerializeField] private float phaseOffset;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private readonly WaterSurfaceRequestPoint waterRequest = new WaterSurfaceRequestPoint();
    private bool hasWaterReference;
    private bool hasSurfaceSample;
    private float referenceWaterLevel;
    private float lastSurfaceHeight;
    private float verticalOffset;
    private float verticalVelocity;
    
    
    private float emergenceDepth;
    private float emergenceDuration;
    private float emergenceElapsed;
    private float sinkingDepth;
    private float sinkingDuration;
    private float sinkingElapsed;
    private bool isSinking;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        TryInitializeWaterReference();
    }

    private void TryInitializeWaterReference()
    {
        if (hasWaterReference || WaterSystem.Instance == null)
            return;

        referenceWaterLevel = WaterSystem.Instance.WaterLevel;
        hasWaterReference = true;
    }

    private void Update()
    {
        float phase = Time.time * bobSpeed + phaseOffset;

        // Rebuild the authored anchor, never the previous frame's animated position.
        Vector3 anchor = transform.parent != null
            ? transform.parent.TransformPoint(baseLocalPosition)
            : baseLocalPosition;

        TryInitializeWaterReference();
        WaterSystem water = WaterSystem.Instance;
        if (water != null && water.isActiveAndEnabled)
        {
            waterRequest.SetNewPosition(anchor);
            WaterSystem.TryGetWaterSurfaceData(waterRequest);

            // KWS supplies the latest completed asynchronous result, not an immediate query.
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

        // Keep the authored immersion depth and hold the last height through missing samples.
        // Start at the authored pose and ease into the first result rather than snapping.
        float targetOffset = hasSurfaceSample
            ? lastSurfaceHeight - referenceWaterLevel + Mathf.Sin(phase) * bobHeight
            : 0f;
        verticalOffset = Mathf.SmoothDamp(verticalOffset, targetOffset,
            ref verticalVelocity, 0.025f, Mathf.Infinity, Time.deltaTime);
        anchor.y += verticalOffset;
        
        if (emergenceElapsed < emergenceDuration)
        {
            emergenceElapsed = Mathf.Min(
                emergenceElapsed + Time.deltaTime,
                emergenceDuration
            );

            float progress = emergenceElapsed / emergenceDuration;
            progress = Mathf.SmoothStep(0f, 1f, progress);
            anchor.y -= emergenceDepth * (1f - progress);
        }
        
        if (isSinking)
        {
            sinkingElapsed = Mathf.Min(
                sinkingElapsed + Time.deltaTime,
                sinkingDuration
            );

            float progress = sinkingElapsed / sinkingDuration;
            progress = Mathf.SmoothStep(0f, 1f, progress);
            anchor.y -= sinkingDepth * progress;
        }
        
        transform.position = anchor;

        transform.localRotation = baseLocalRotation *
                                  Quaternion.Euler(
                                      Mathf.Sin(phase * 0.8f) * tiltAngle,
                                      0f,
                                      Mathf.Cos(phase) * tiltAngle
                                  );
    }
    
    public void BeginEmergence(float depth, float duration)
    {
        emergenceDepth = Mathf.Max(0f, depth);
        emergenceDuration = Mathf.Max(0.01f, duration);
        emergenceElapsed = 0f;
    }
    
    public void BeginSinking(float depth, float duration)
    {
        sinkingDepth = Mathf.Max(0f, depth);
        sinkingDuration = Mathf.Max(0.01f, duration);
        sinkingElapsed = 0f;
        isSinking = true;
    }
}
