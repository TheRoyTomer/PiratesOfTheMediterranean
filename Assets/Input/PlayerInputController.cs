using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private ShipController shipController;
    [SerializeField] private FiringDirectionController firingDirectionController;
    [SerializeField] private WeaponSystem weaponSystem;
    [SerializeField] private CameraModeController cameraModeController;

    private PiratesInputActions inputActions;

    private void Awake()
    {
        inputActions = new PiratesInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
        if (cameraModeController != null)
            cameraModeController.ResetToMain();
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    private void Update()
    {
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        shipController.SetThrottle(moveInput.y);
        shipController.SetSteering(moveInput.x);
        
        if (inputActions.Player.CycleFiringDirection.WasPressedThisFrame())
        {
            firingDirectionController.CycleDirection();
        }

        if (cameraModeController != null)
        {
            cameraModeController.SetInput(
                inputActions.Player.ToggleFiringCamera.WasPressedThisFrame(),
                inputActions.Player.ReverseCamera.IsPressed());
        }
        
        if (inputActions.Player.Fire.WasPressedThisFrame())
        {
            weaponSystem.Fire(firingDirectionController.SelectedDirection);
        }
    }
}
