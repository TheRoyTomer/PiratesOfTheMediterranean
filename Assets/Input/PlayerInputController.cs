using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private ShipController shipController;
    [SerializeField] private FiringDirectionController firingDirectionController;
    [SerializeField] private WeaponSystem weaponSystem;

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
        
        if (inputActions.Player.Fire.WasPressedThisFrame())
        {
            weaponSystem.Fire(firingDirectionController.SelectedDirection);
        }
    }
}
