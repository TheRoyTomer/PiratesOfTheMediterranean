using UnityEngine;
using UnityEngine.InputSystem;

public class FlyCameraInputSystem : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float sprintMultiplier = 3f;
    [SerializeField] private float verticalSpeed = 6f;
    [SerializeField] private float lookSensitivity = 0.12f;

    [Header("Options")]
    [SerializeField] private bool requireRightMouseToLook = true;
    [SerializeField] private bool lockCursorOnPlay = false;
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction upAction;
    private InputAction downAction;
    private InputAction sprintAction;
    private InputAction lookHoldAction;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);

        BuildInputActions();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        upAction.Enable();
        downAction.Enable();
        sprintAction.Enable();
        lookHoldAction.Enable();

        if (lockCursorOnPlay && !requireRightMouseToLook)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        upAction.Disable();
        downAction.Disable();
        sprintAction.Disable();
        lookHoldAction.Disable();
    }

    private void OnDestroy()
    {
        moveAction.Dispose();
        lookAction.Dispose();
        upAction.Dispose();
        downAction.Dispose();
        sprintAction.Dispose();
        lookHoldAction.Dispose();
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        bool canLook = !requireRightMouseToLook || lookHoldAction.IsPressed();

        if (!canLook)
        {
            if (requireRightMouseToLook)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }

        if (requireRightMouseToLook)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Vector2 lookDelta = lookAction.ReadValue<Vector2>();
        yaw += lookDelta.x * lookSensitivity;
        pitch -= lookDelta.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float up = upAction.IsPressed() ? 1f : 0f;
        float down = downAction.IsPressed() ? 1f : 0f;

        float currentSpeed = moveSpeed;
        if (sprintAction.IsPressed())
            currentSpeed *= sprintMultiplier;

        Vector3 move =
            transform.forward * moveInput.y +
            transform.right * moveInput.x +
            transform.up * ((up - down) * (verticalSpeed / Mathf.Max(moveSpeed, 0.001f)));

        transform.position += move * currentSpeed * Time.deltaTime;
    }

    private void BuildInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        lookAction = new InputAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");

        upAction = new InputAction("Up", InputActionType.Button);
        upAction.AddBinding("<Keyboard>/e");
        upAction.AddBinding("<Keyboard>/pageUp");
        upAction.AddBinding("<Gamepad>/rightShoulder");

        downAction = new InputAction("Down", InputActionType.Button);
        downAction.AddBinding("<Keyboard>/q");
        downAction.AddBinding("<Keyboard>/pageDown");
        downAction.AddBinding("<Gamepad>/leftShoulder");

        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");

        lookHoldAction = new InputAction("LookHold", InputActionType.Button);
        lookHoldAction.AddBinding("<Mouse>/rightButton");
    }

    private float NormalizePitch(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}