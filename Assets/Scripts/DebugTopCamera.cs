using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// Runtime-only diagnostic view; no scene or gameplay-camera settings are saved.
public sealed class DebugTopCamera : MonoBehaviour
{
    private Camera gameplayCamera;
    private Camera debugCamera;
    private Transform ship;
    private CameraModeController cameraModeController;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateDebugCamera()
    {
        Camera main = Camera.main;
        GameObject player = GameObject.Find("PlayerShip");
        if (main == null || player == null || main.GetComponent<CameraModeController>() == null)
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
