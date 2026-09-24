using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private ShipController shipController;
    [SerializeField] private FiringDirectionController firingDirectionController;
    [SerializeField] private WeaponSystem weaponSystem;
    [SerializeField] private CameraModeController cameraModeController;
    
    [SerializeField, Range(0f, 1f)] private float repairFraction = 0.25f;

    private ShipHealth shipHealth;
    private PlayerInventory playerInventory;

    private PiratesInputActions inputActions;

    private void Awake()
    {
        inputActions = new PiratesInputActions();
        shipHealth = GetComponent<ShipHealth>();
        playerInventory = GetComponent<PlayerInventory>();
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

        #if UNITY_EDITOR
        if (shipController != null)
            shipController.SetDevelopmentBoost(false);
        #endif
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

               #if UNITY_EDITOR
                shipController.SetDevelopmentBoost(
                    Keyboard.current != null &&
                    Keyboard.current.vKey.isPressed);
        #endif 


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
        
        if (inputActions.Player.Repair.WasPressedThisFrame() &&
            shipHealth != null &&
            playerInventory != null &&
            playerInventory.ShipParts > 0 &&
            !shipHealth.IsDead &&
            shipHealth.CurrentHealth < shipHealth.MaxHealth)
        {
            float healthBeforeRepair = shipHealth.CurrentHealth;
            shipHealth.Heal(shipHealth.MaxHealth * repairFraction);

            if (shipHealth.CurrentHealth > healthBeforeRepair)
                playerInventory.TryUseShipPart();
        }
    }
}
