using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private ShipController shipController;

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

    private void Update()
    {
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        shipController.SetThrottle(moveInput.y);
        shipController.SetSteering(moveInput.x);
    }
}