using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Camera Settings")]
    [SerializeField] private float distance = 65f;
    [SerializeField] private float height = 35f;
    [SerializeField] private float mouseSensitivity = 0.5f;

    [Header("Camera Smoothing")]
    [SerializeField] private float rotationSmoothSpeed = 5f;
    [SerializeField] private float returnDelay = 1.5f;
    [SerializeField] private float returnSpeed = 2f;

    private float targetYaw;
    private float currentYaw;
    private float timeSinceMouseInput;

    private void Start()
    {
        targetYaw = target.eulerAngles.y;
        currentYaw = targetYaw;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float mouseX = Mouse.current.delta.ReadValue().x;

        if (Mathf.Abs(mouseX) > 0.01f)
        {
            targetYaw += mouseX * mouseSensitivity;
            timeSinceMouseInput = 0f;
        }
        else
        {
            timeSinceMouseInput += Time.deltaTime;
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

        Quaternion rotation = Quaternion.Euler(0f, currentYaw, 0f);

        Vector3 offset = rotation * new Vector3(0f, height, -distance);

        transform.position = target.position + offset;

        Vector3 lookTarget = target.position + Vector3.up * 5f;
        transform.LookAt(lookTarget);
    }
}