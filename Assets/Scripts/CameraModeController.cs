using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(900)]
public sealed class CameraModeController : MonoBehaviour
{
    public enum CameraMode { Main, Firing, Reverse }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera frontFiringCamera;
    [SerializeField] private Camera rightFiringCamera;
    [SerializeField] private Camera leftFiringCamera;
    [SerializeField] private Camera backFiringCamera;
    [SerializeField] private FiringDirectionController firingDirectionController;
    [Header("Player death view — scene camera, disabled during gameplay")]
    [SerializeField] private Camera deathCamera;
    [SerializeField] private Vector3 deathViewOffset = new Vector3(140f, 105f, -65f);
    [SerializeField] private float deathLookHeight = 12f;
    private ShipHealth playerHealth;
    private bool deathViewActive;

    public CameraMode Mode { get; private set; } = CameraMode.Main;
    public Camera ActiveGameplayCamera { get; private set; }
    public bool IsFiringViewActive => !deathViewActive && debugCamera == null && Mode == CameraMode.Firing;

    private bool toggleRequested;
    private bool reverseHeld;
    private Camera debugCamera;

    public void BindPlayer(FiringDirectionController direction, Camera front, Camera right, Camera left, Camera back)
    {
        if (playerHealth != null) playerHealth.OnDeath -= BeginDeathView;
        playerHealth = direction.GetComponent<ShipHealth>();
        playerHealth.OnDeath += BeginDeathView;
        firingDirectionController = direction;
        frontFiringCamera = front;
        rightFiringCamera = right;
        leftFiringCamera = left;
        backFiringCamera = back;
        ResetToMain();
    }

    private void Awake()
    {
        ApplyCamera();
    }

    public void SetInput(bool togglePressed, bool reverseIsHeld)
    {
        if (deathViewActive) return;
        toggleRequested |= togglePressed;
        reverseHeld = reverseIsHeld;
    }

    public void SetDebugCamera(Camera camera)
    {
        if (deathViewActive) return;
        if (debugCamera != null && debugCamera != camera)
            debugCamera.enabled = false;
        debugCamera = camera;
        toggleRequested = false;
        ApplyCamera();
    }

    public void ResetToMain()
    {
        if (deathViewActive) return;
        Mode = CameraMode.Main;
        toggleRequested = false;
        reverseHeld = false;
        ApplyCamera();
    }

    private void LateUpdate()
    {
        if (PauseMenuController.BlocksGameplayInput) return;
        if (deathViewActive) { ApplyCamera(); return; }

        // Input and Q cycling finish in Update; HUD projection runs after this.
        if (reverseHeld)
            Mode = CameraMode.Reverse;
        else if (Mode == CameraMode.Reverse)
            Mode = CameraMode.Main;
        else if (toggleRequested && debugCamera == null)
            Mode = Mode == CameraMode.Main ? CameraMode.Firing : CameraMode.Main;

        toggleRequested = false;
        ApplyCamera();
    }

    private void ApplyCamera()
    {
        Camera selected = mainCamera;
        if (Mode == CameraMode.Reverse)
            selected = backFiringCamera;
        else if (Mode == CameraMode.Firing && firingDirectionController != null)
        {
            selected = firingDirectionController.SelectedDirection switch
            {
                FiringDirection.Front => frontFiringCamera,
                FiringDirection.Right => rightFiringCamera,
                FiringDirection.Left => leftFiringCamera,
                _ => mainCamera
            };
        }

        if (selected == null)
            selected = mainCamera;
        if (debugCamera != null)
            selected = debugCamera;
        if (deathViewActive && deathCamera != null)
            selected = deathCamera;

        // Disable the old view before enabling its replacement. CameraFollow
        // and the Main Camera AudioListener remain active on the GameObject.
        DisableUnlessSelected(mainCamera, selected);
        DisableUnlessSelected(frontFiringCamera, selected);
        DisableUnlessSelected(rightFiringCamera, selected);
        DisableUnlessSelected(leftFiringCamera, selected);
        DisableUnlessSelected(backFiringCamera, selected);
        DisableUnlessSelected(deathCamera, selected);
        DisableUnlessSelected(debugCamera, selected);
        if (selected != null)
            selected.enabled = true;
        ActiveGameplayCamera = selected;
    }

    private void BeginDeathView()
    {
        if (deathViewActive || playerHealth == null) return;
        if (deathCamera == null)
        {
            Debug.LogError("CameraModeController requires the scene's Death Camera.", this);
            return;
        }
        deathViewActive = true;
        toggleRequested = reverseHeld = false;
        // Snapshot the ship's position and yaw before it rolls. Never follow its sinking Y.
        Vector3 anchor = playerHealth.transform.position;
        float waterY = KWS.WaterSystem.Instance != null ? KWS.WaterSystem.Instance.WaterLevel : 0f;
        anchor.y = Mathf.Max(anchor.y, waterY);
        Quaternion yaw = Quaternion.Euler(0f, playerHealth.transform.eulerAngles.y, 0f);
        Vector3 position = anchor + yaw * deathViewOffset;
        position.y = Mathf.Max(position.y, waterY + 10f);
        Quaternion rotation = Quaternion.LookRotation(anchor + Vector3.up * deathLookHeight - position, Vector3.up);
        deathCamera.transform.SetPositionAndRotation(position, rotation);
        // Keep the existing single AudioListener at the cinematic viewpoint.
        if (mainCamera != null)
        {
            if (mainCamera.TryGetComponent<CameraFollow>(out var follow)) follow.enabled = false;
            mainCamera.transform.SetPositionAndRotation(position, rotation);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ApplyCamera();
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDeath -= BeginDeathView;
    }

    private static void DisableUnlessSelected(Camera camera, Camera selected)
    {
        if (camera != null && camera != selected)
            camera.enabled = false;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && !PauseMenuController.IsPaused)
            ResetToMain();
    }

    private void OnDisable()
    {
        ResetToMain();
    }
}
