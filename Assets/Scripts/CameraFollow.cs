using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Camera Settings")]
    [SerializeField] private float distance = 65f;
    [SerializeField] private float height = 35f;
    [SerializeField] private float mouseSensitivity = 0.5f;
    [SerializeField] private float verticalMouseSensitivity = 0.5f;
    [SerializeField] private float minPitch = 5f;
    [SerializeField] private float maxPitch = 28f;

    [Header("Camera Smoothing")]
    [SerializeField] private float rotationSmoothSpeed = 5f;
    [SerializeField] private float returnDelay = 1.5f;
    [SerializeField] private float returnSpeed = 2f;

    private float targetYaw;
    private float currentYaw;

    private float targetPitch;
    private float currentPitch;

    private float timeSinceMouseInput;

    // Reuse the player's orbit tuning for runtime diagnostic cameras.
    public void CopySettingsFrom(CameraFollow source, Transform followTarget)
    {
        target = followTarget;
        distance = source.distance;
        height = source.height;
        mouseSensitivity = source.mouseSensitivity;
        verticalMouseSensitivity = source.verticalMouseSensitivity;
        minPitch = source.minPitch;
        maxPitch = source.maxPitch;
        rotationSmoothSpeed = source.rotationSmoothSpeed;
        returnDelay = source.returnDelay;
        returnSpeed = source.returnSpeed;
    }

    private void Start()
    {
        targetYaw = target.eulerAngles.y;
        currentYaw = targetYaw;

        float startPitch = transform.eulerAngles.x;

        if (startPitch > 180f)
            startPitch -= 360f;

        targetPitch = Mathf.Clamp(startPitch, minPitch, maxPitch);
        currentPitch = targetPitch;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector2 mouseDelta = Mouse.current != null
            ? Mouse.current.delta.ReadValue() : Vector2.zero;

        float mouseX = mouseDelta.x;
        float mouseY = mouseDelta.y;

        if (Mathf.Abs(mouseX) > 0.01f)
        {
            targetYaw += mouseX * mouseSensitivity;
            timeSinceMouseInput = 0f;
        }
        else
        {
            timeSinceMouseInput += Time.deltaTime;
        }

        if (Mathf.Abs(mouseY) > 0.01f)
        {
            targetPitch -= mouseY * verticalMouseSensitivity;

            targetPitch = Mathf.Clamp(
                targetPitch,
                minPitch,
                maxPitch
            );
        }

        if (timeSinceMouseInput >= returnDelay)
        {
            float shipYaw = target.eulerAngles.y;

            targetYaw = Mathf.LerpAngle(
                targetYaw,
                shipYaw,
                returnSpeed * Time.deltaTime
            );
        }

        currentYaw = Mathf.LerpAngle(
            currentYaw,
            targetYaw,
            rotationSmoothSpeed * Time.deltaTime
        );

        currentPitch = Mathf.LerpAngle(
            currentPitch,
            targetPitch,
            rotationSmoothSpeed * Time.deltaTime
        );

        Quaternion rotation = Quaternion.Euler(
            currentPitch,
            currentYaw,
            0f
        );

        Vector3 offset = rotation * new Vector3(
            0f,
            height,
            -distance
        );

        transform.position = target.position + offset;

        Vector3 lookTarget = target.position + Vector3.up * 5f;
        transform.LookAt(lookTarget);
    }
}
