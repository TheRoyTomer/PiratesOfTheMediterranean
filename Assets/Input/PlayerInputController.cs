using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    public void SetCameraController(CameraModeController controller) => cameraModeController = controller;

    [SerializeField] private ShipController shipController;
    [SerializeField] private FiringDirectionController firingDirectionController;
    [SerializeField] private WeaponSystem weaponSystem;
    [SerializeField] private CameraModeController cameraModeController;
    
    [SerializeField, Range(0f, 1f)] private float repairFraction = 0.25f;

    private ShipHealth shipHealth;
    private PlayerShipParts playerInventory;

    private PiratesInputActions inputActions;

    private void Awake()
    {
        inputActions = new PiratesInputActions();
        shipHealth = GetComponent<ShipHealth>();
        playerInventory = GetComponent<PlayerShipParts>();
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
        if (PauseMenuController.BlocksGameplayInput || (shipHealth != null && shipHealth.IsDead)) return;

        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        shipController.SetThrottle(moveInput.y);
        shipController.SetSteering(moveInput.x);

        if (inputActions.Player.CycleFiringDirection.WasPressedThisFrame())
        {
            firingDirectionController.CycleDirection();
            GameAudio.Play(GameSound.SwitchFireDirection);
        }

        if (cameraModeController != null)
        {
            cameraModeController.SetInput(
                inputActions.Player.ToggleFiringCamera.WasPressedThisFrame(),
                inputActions.Player.ReverseCamera.IsPressed());
        }
        
        if (inputActions.Player.Fire.WasPressedThisFrame())
        {
            if ((shipHealth != null && shipHealth.IsDead) || weaponSystem.IsOnCooldown(firingDirectionController.SelectedDirection))
                GameAudio.Play(GameSound.ActionDenied);
            weaponSystem.Fire(firingDirectionController.SelectedDirection);
        }
        
        if (inputActions.Player.DeployBarrels.WasPressedThisFrame())
        {
            if (!weaponSystem.CanDeployBarrels) GameAudio.Play(GameSound.ActionDenied);
            weaponSystem.DeployBarrels();
        }
        
        if (inputActions.Player.Repair.WasPressedThisFrame())
        {
            if (shipHealth != null &&
                playerInventory != null &&
                playerInventory.ShipParts > 0 &&
                !shipHealth.IsDead &&
                shipHealth.CurrentHealth < shipHealth.MaxHealth)
            {
                float healthBeforeRepair = shipHealth.CurrentHealth;
                shipHealth.Heal(shipHealth.MaxHealth * repairFraction);

                if (shipHealth.CurrentHealth > healthBeforeRepair && playerInventory.TryUseShipPart())
                    GameAudio.Play(GameSound.Repair);
                else
                    GameAudio.Play(GameSound.ActionDenied);
            }
            else GameAudio.Play(GameSound.ActionDenied);
        }
    }
}
