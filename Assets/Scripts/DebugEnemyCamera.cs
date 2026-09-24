using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// Runtime-only third-person diagnostic view, available in Editor/development builds.
public sealed class DebugEnemyCamera : MonoBehaviour
{
    private Camera view;
    private CameraModeController cameraModeController;
    private Transform enemy;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateDebugCamera()
    {
        Camera main = Camera.main;
        GameObject enemyShip = GameObject.Find("EnemyShip");
        if (main == null || enemyShip == null)
            return;

        CameraFollow playerFollow = main.GetComponent<CameraFollow>();
        CameraModeController modes = main.GetComponent<CameraModeController>();
        if (playerFollow == null || modes == null)
            return;

        GameObject root = new GameObject("EnemyShip Debug Camera");
        root.SetActive(false);
        root.transform.rotation = main.transform.rotation;

        Camera camera = root.AddComponent<Camera>();
        camera.CopyFrom(main);
        camera.enabled = false;
        camera.targetTexture = null;
        camera.rect = new Rect(0f, 0f, 1f, 1f);

        UniversalAdditionalCameraData data = root.AddComponent<UniversalAdditionalCameraData>();
        UniversalAdditionalCameraData sourceData = main.GetComponent<UniversalAdditionalCameraData>();
        if (sourceData != null)
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(sourceData), data);
        data.renderType = CameraRenderType.Base;
        data.cameraStack.Clear();

        CameraFollow follow = root.AddComponent<CameraFollow>();
        follow.CopySettingsFrom(playerFollow, enemyShip.transform);

        DebugEnemyCamera toggle = root.AddComponent<DebugEnemyCamera>();
        toggle.view = camera;
        toggle.cameraModeController = modes;
        toggle.enemy = enemyShip.transform;
        root.SetActive(true);
    }
#endif

    private void Update()
    {
        if (enemy == null)
        {
            RestorePlayerCamera();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (cameraModeController.ActiveGameplayCamera == view)
                RestorePlayerCamera();
            else
                cameraModeController.SetDebugCamera(view);
        }
    }

    private void RestorePlayerCamera()
    {
        if (cameraModeController == null || cameraModeController.ActiveGameplayCamera != view)
            return;

        cameraModeController.ResetToMain();
        cameraModeController.SetDebugCamera(null);
    }

    private void OnDisable()
    {
        RestorePlayerCamera();
        if (view != null)
            view.enabled = false;
    }
}
