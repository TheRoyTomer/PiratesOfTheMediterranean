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

    public CameraMode Mode { get; private set; } = CameraMode.Main;
    public Camera ActiveGameplayCamera { get; private set; }

    private bool toggleRequested;
    private bool reverseHeld;
    private bool debugOverride;

    private void Awake()
    {
        ApplyCamera();
    }

    public void SetInput(bool togglePressed, bool reverseIsHeld)
    {
        toggleRequested |= togglePressed;
        reverseHeld = reverseIsHeld;
    }

    public void SetDebugOverride(bool active)
    {
        debugOverride = active;
        toggleRequested = false;
        ApplyCamera();
    }

    public void ResetToMain()
    {
        Mode = CameraMode.Main;
        toggleRequested = false;
        reverseHeld = false;
        ApplyCamera();
    }

    private void LateUpdate()
    {
        // Input and Q cycling finish in Update; HUD projection runs after this.
        if (reverseHeld)
            Mode = CameraMode.Reverse;
        else if (Mode == CameraMode.Reverse)
            Mode = CameraMode.Main;
        else if (toggleRequested && !debugOverride)
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
        if (debugOverride)
            selected = null;

        // Disable the old view before enabling its replacement. CameraFollow
        // and the Main Camera AudioListener remain active on the GameObject.
        DisableUnlessSelected(mainCamera, selected);
        DisableUnlessSelected(frontFiringCamera, selected);
        DisableUnlessSelected(rightFiringCamera, selected);
        DisableUnlessSelected(leftFiringCamera, selected);
        DisableUnlessSelected(backFiringCamera, selected);
        if (selected != null)
            selected.enabled = true;
        ActiveGameplayCamera = selected;
    }

    private static void DisableUnlessSelected(Camera camera, Camera selected)
    {
        if (camera != null && camera != selected)
            camera.enabled = false;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
            ResetToMain();
    }

    private void OnDisable()
    {
        ResetToMain();
    }
}
