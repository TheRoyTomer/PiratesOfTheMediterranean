using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// Runtime-only diagnostic view; no scene or gameplay-camera settings are saved.
public sealed class DebugTopCamera : MonoBehaviour
{
    private Camera gameplayCamera;
    private Camera debugCamera;
    private Transform ship;
    private bool showingDebug;
    private bool previousCameraEnabled;
    private CameraModeController cameraModeController;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateDebugCamera()
    {
        Camera main = Camera.main;
        GameObject player = GameObject.Find("PlayerShip");
        if (main == null || player == null)
            return;

        GameObject root = new GameObject("DebugTopCamera");
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
        toggle.cameraModeController = main.GetComponent<CameraModeController>();
        toggle.debugCamera = view;
        toggle.ship = player.transform;
        toggle.FrameCurrentArea();
        root.SetActive(true);
    }
#endif

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.cKey.wasPressedThisFrame
            || gameplayCamera == null || debugCamera == null || ship == null)
            return;

        if (showingDebug)
        {
            RestoreGameplayCamera();
        }
        else
        {
            FrameCurrentArea();
            previousCameraEnabled = gameplayCamera.enabled;
            if (cameraModeController != null)
                cameraModeController.SetDebugOverride(true);
            else
                gameplayCamera.enabled = false;
            debugCamera.enabled = true;
            showingDebug = true;
        }
    }

    private void FrameCurrentArea()
    {
        Vector3 forward = Vector3.ProjectOnPlane(ship.forward, Vector3.up).normalized;
        Vector3 center = ship.position + forward * 150f;
        // Fixed world orientation: no follow, orbit, or rotation while viewing.
        transform.SetPositionAndRotation(
            center + new Vector3(0f, 1000f, -267.9492f),
            Quaternion.Euler(75f, 0f, 0f));
    }

    private void RestoreGameplayCamera()
    {
        if (debugCamera != null)
            debugCamera.enabled = false;
        if (showingDebug)
        {
            if (cameraModeController != null)
                cameraModeController.SetDebugOverride(false);
            else if (gameplayCamera != null)
                gameplayCamera.enabled = previousCameraEnabled;
        }
        showingDebug = false;
    }

    private void OnDisable()
    {
        RestoreGameplayCamera();
    }
}
