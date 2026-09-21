using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ShipHealth))]
[DefaultExecutionOrder(1000)]
public sealed class EnemyHudController : MonoBehaviour
{
    [SerializeField] private EnemyHudView distanceHudPrefab;
    [SerializeField] private EnemyHudView healthHudPrefab;
    [SerializeField] private Transform playerShip;
    [SerializeField] private Transform hudAnchor;
    [SerializeField] private Transform healthHudAnchor;
    [SerializeField] private Renderer[] visibilityRenderers;

    [Tooltip("Ship-local hull/mast sample points, not the elevated HUD.")]
    [SerializeField] private Vector3[] visibilityPoints;

    [Header("Distance HUD Screen Scaling")]
    [SerializeField] private float distanceHudCloseDistance = 100f;
    [SerializeField] private float distanceHudFarDistance = 1700f;
    [SerializeField] private float distanceHudCloseScale = 1.1f;
    [SerializeField] private float distanceHudFarScale = 0.7f;

    [Header("Health HUD Screen Scaling")]
    [SerializeField] private float healthHudCloseDistance = 100f;
    [SerializeField] private float healthHudFarDistance = 1700f;
    [SerializeField] private float healthHudMinScreenScale = 0.35f;
    [SerializeField] private float healthHudMaxScreenScale = 0.45f;

    private ShipHealth health;
    private EnemyHudView distanceView;
    private EnemyHudView healthView;
    private CameraModeController cameraModeController;
    private Camera[] cameraBuffer = new Camera[4];
    private readonly Plane[] frustum = new Plane[6];

    private float lastHealth = float.NaN;
    private float lastMaxHealth = float.NaN;
    private int lastDistance = -1;

    private void Awake()
    {
        health = GetComponent<ShipHealth>();
        cameraModeController = FindFirstObjectByType<CameraModeController>();

        if (distanceHudPrefab == null || healthHudPrefab == null
            || hudAnchor == null || healthHudAnchor == null)
        {
            enabled = false;
            return;
        }

        distanceView = Instantiate(distanceHudPrefab);
        distanceView.name = name + " DistanceHUD";

        healthView = Instantiate(healthHudPrefab);
        healthView.name = name + " HealthHUD";

        SetVisible(false);
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HideOnDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HideOnDeath;

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (distanceView != null)
            Destroy(distanceView.gameObject);

        if (healthView != null)
            Destroy(healthView.gameObject);
    }

    private void HideOnDeath()
    {
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (distanceView == null || healthView == null)
            return;

        Camera activeCamera = ResolveCamera();

        if (health.IsDead || playerShip == null || activeCamera == null)
        {
            SetVisible(false);
            return;
        }

        bool distanceAnchorInFront = UpdateDistanceScreenPosition(activeCamera);

        bool healthAnchorInFront =
            UpdateHealthScreenPosition(activeCamera);

        bool visible =
            ShipIsVisible(activeCamera);

        SetVisible(visible);
        distanceView.canvas.enabled = visible && distanceAnchorInFront;

        healthView.canvas.enabled =
            visible && healthAnchorInFront;

        if (!visible)
            return;

        float current = health.CurrentHealth;
        float maximum = health.MaxHealth;

        if (current != lastHealth || maximum != lastMaxHealth)
        {
            float fraction =
                maximum > 0f
                    ? Mathf.Clamp01(current / maximum)
                    : 0f;

            healthView.healthFill.fillAmount = fraction;
            healthView.healthFill.color = GetHealthColor(fraction);

            healthView.percentageText.SetText(
                "{0}%",
                Mathf.RoundToInt(fraction * 100f)
            );

            lastHealth = current;
            lastMaxHealth = maximum;
        }

        Vector3 delta =
            transform.position - playerShip.position;

        int meters = Mathf.RoundToInt(
            new Vector2(delta.x, delta.z).magnitude
        );

        if (meters != lastDistance)
        {
            distanceView.distanceText.SetText(
                "{0} m",
                meters
            );

            lastDistance = meters;
        }
    }

    private bool UpdateDistanceScreenPosition(Camera camera)
    {
        Vector3 anchorPosition =
            hudAnchor.position;

        Vector3 screenPoint =
            camera.WorldToScreenPoint(anchorPosition);

        if (screenPoint.z <= 0f)
            return false;

        distanceView.canvas.targetDisplay =
            camera.targetDisplay;

        RectTransform canvasRect =
            (RectTransform)distanceView.canvas.transform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                null,
                out Vector2 canvasPoint))
        {
            return false;
        }

        distanceView.screenContent.anchoredPosition =
            canvasPoint;

        float distance = Vector3.Distance(
            camera.transform.position,
            anchorPosition
        );

        float distanceBlend = Mathf.InverseLerp(
            distanceHudCloseDistance,
            distanceHudFarDistance,
            distance
        );

        float pixelScale = Mathf.Lerp(
            distanceHudCloseScale,
            distanceHudFarScale,
            distanceBlend
        );

        float localScale =
            pixelScale / distanceView.canvas.scaleFactor;

        distanceView.screenContent.localScale =
            new Vector3(
                localScale,
                localScale,
                1f
            );

        return true;
    }

    private bool UpdateHealthScreenPosition(Camera camera)
    {
        Vector3 anchorPosition =
            healthHudAnchor.position;

        Vector3 screenPoint =
            camera.WorldToScreenPoint(anchorPosition);

        if (screenPoint.z <= 0f)
            return false;

        healthView.canvas.targetDisplay =
            camera.targetDisplay;

        RectTransform canvasRect =
            (RectTransform)healthView.canvas.transform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                null,
                out Vector2 canvasPoint))
        {
            return false;
        }

        healthView.screenContent.anchoredPosition =
            canvasPoint;

        float distance = Vector3.Distance(
            camera.transform.position,
            anchorPosition
        );

        float distanceBlend = Mathf.InverseLerp(
            healthHudCloseDistance,
            healthHudFarDistance,
            distance
        );

        float pixelScale = Mathf.Lerp(
            healthHudMinScreenScale,
            healthHudMaxScreenScale,
            distanceBlend
        );

        float localScale =
            pixelScale / healthView.canvas.scaleFactor;

        healthView.screenContent.localScale =
            new Vector3(
                localScale,
                localScale,
                1f
            );

        return true;
    }

    private Color GetHealthColor(float fraction)
    {
        if (fraction >= 0.75f)
        {
            return Color.green;
        }

        if (fraction >= 0.35f)
        {
            float t =
                Mathf.InverseLerp(
                    0.75f,
                    0.35f,
                    fraction
                );

            return Color.Lerp(
                Color.green,
                new Color(1f, 0.5f, 0f),
                t
            );
        }

        if (fraction >= 0.05f)
        {
            float t =
                Mathf.InverseLerp(
                    0.35f,
                    0.05f,
                    fraction
                );

            return Color.Lerp(
                new Color(1f, 0.5f, 0f),
                Color.red,
                t
            );
        }

        return Color.red;
    }

    private Camera ResolveCamera()
    {
        int count =
            Camera.allCamerasCount;

        if (cameraBuffer.Length < count)
            cameraBuffer = new Camera[count];

        count =
            Camera.GetAllCameras(cameraBuffer);

        Camera gameplay = null;

        for (int i = 0; i < count; i++)
        {
            Camera candidate =
                cameraBuffer[i];

            if (!candidate.isActiveAndEnabled
                || candidate.cameraType != CameraType.Game
                || candidate.targetTexture != null)
            {
                continue;
            }

            if (candidate.GetComponent<DebugTopCamera>() != null)
                return candidate;

            if (candidate.GetComponent<CameraFollow>() != null
                || (cameraModeController != null
                    && candidate == cameraModeController.ActiveGameplayCamera))
                gameplay = candidate;
        }

        return gameplay;
    }

    private bool ShipIsVisible(Camera camera)
    {
        GeometryUtility.CalculateFrustumPlanes(
            camera,
            frustum
        );

        bool inFrustum = false;

        foreach (Renderer mesh in visibilityRenderers)
        {
            if (mesh != null
                && mesh.enabled
                && mesh.gameObject.activeInHierarchy
                && (camera.cullingMask & (1 << mesh.gameObject.layer)) != 0
                && GeometryUtility.TestPlanesAABB(
                    frustum,
                    mesh.bounds
                ))
            {
                inFrustum = true;
                break;
            }
        }

        if (!inFrustum)
            return false;

        int clearPoints = 0;

        foreach (Vector3 localPoint in visibilityPoints)
        {
            Vector3 point =
                transform.TransformPoint(localPoint);

            Vector3 viewport =
                camera.WorldToViewportPoint(point);

            if (viewport.z < camera.nearClipPlane
                || viewport.z > camera.farClipPlane
                || viewport.x < 0f
                || viewport.x > 1f
                || viewport.y < 0f
                || viewport.y > 1f)
            {
                continue;
            }

            Vector3 delta =
                point - camera.transform.position;

            if (!Physics.Raycast(
                    camera.transform.position,
                    delta.normalized,
                    delta.magnitude,
                    LayerMask.GetMask("Default", "EnvironmentOccluder"),
                    QueryTriggerInteraction.Ignore
                )
                && ++clearPoints >= 2)
            {
                return true;
            }
        }

        return false;
    }

    private void SetVisible(bool visible)
    {
        if (distanceView != null)
            distanceView.canvas.enabled = visible;

        if (healthView != null)
            healthView.canvas.enabled = visible;
    }
}
