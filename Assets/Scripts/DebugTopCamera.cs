using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// Supported overhead gameplay view. The class name is retained for existing HUD references.
public sealed class DebugTopCamera : MonoBehaviour
{
    private Camera gameplayCamera;
    private Camera debugCamera;
    private Transform ship;
    private CameraModeController cameraModeController;

    // Called for each spawned player, including menu entry and scene-reload restart.
    public static void CreateForPlayer(Camera main, Transform player, CameraModeController modes)
    {
        if (main == null || player == null || modes == null)
            return;

        foreach (var existing in FindObjectsByType<DebugTopCamera>(FindObjectsSortMode.None))
        {
            if (existing.cameraModeController != modes) continue;
            existing.ship = player;
            return;
        }

        GameObject root = new GameObject("Overhead Camera");
        root.SetActive(false);
        Camera view = root.AddComponent<Camera>();
        view.CopyFrom(main);
        view.enabled = false;
        view.orthographic = false;
        view.fieldOfView = 60f;
        view.nearClipPlane = 1f;
        view.farClipPlane = Mathf.Max(main.farClipPlane, 3000f);
        view.targetTexture = null;
        view.rect = new Rect(0f, 0f, 1f, 1f);
        root.AddComponent<UniversalAdditionalCameraData>();

        DebugTopCamera toggle = root.AddComponent<DebugTopCamera>();
        toggle.gameplayCamera = main;
        toggle.cameraModeController = modes;
        toggle.debugCamera = view;
        toggle.ship = player;
        toggle.FrameCurrentArea();
        root.SetActive(true);
    }

    private void Update()
    {
        if (PauseMenuController.BlocksGameplayInput) return;

        if (Keyboard.current == null || !Keyboard.current.capsLockKey.wasPressedThisFrame
            || gameplayCamera == null || debugCamera == null || ship == null)
            return;

        if (cameraModeController.ActiveGameplayCamera == debugCamera)
        {
            RestoreGameplayCamera();
        }
        else
        {
            FrameCurrentArea();
            cameraModeController.SetDebugCamera(debugCamera);
        }
    }

    private void FrameCurrentArea()
    {
        // Center on the player while retaining the fixed world orientation.
        transform.SetPositionAndRotation(
            ship.position + new Vector3(0f, 1000f, -267.9492f),
            Quaternion.Euler(75f, 0f, 0f));
    }

    private void LateUpdate()
    {
        if (ship != null && cameraModeController != null &&
            cameraModeController.ActiveGameplayCamera == debugCamera)
            FrameCurrentArea();
    }

    private void RestoreGameplayCamera()
    {
        if (debugCamera != null)
            debugCamera.enabled = false;
        if (cameraModeController != null &&
            cameraModeController.ActiveGameplayCamera == debugCamera)
            cameraModeController.SetDebugCamera(null);
    }

    private void OnDisable()
    {
        RestoreGameplayCamera();
    }
}
