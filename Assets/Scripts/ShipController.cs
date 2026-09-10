using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float turnAcceleration = 2f;
    [SerializeField] private float boostMultiplier = 1.5f;

    private Rigidbody rb;

    private float throttleInput;
    private float steeringInput;
    private bool boostActive;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplySteering();
    }

    public void SetThrottle(float value)
    {
        throttleInput = Mathf.Clamp(value, -1f, 1f);
    }

    public void SetSteering(float value)
    {
        steeringInput = Mathf.Clamp(value, -1f, 1f);
    }

    public void SetBoost(bool active)
    {
        boostActive = active;
    }

    private void ApplyMovement()
    {
        float multiplier = boostActive ? boostMultiplier : 1f;

        Vector3 force =
            transform.forward *
            throttleInput *
            acceleration *
            multiplier;

        rb.AddForce(force, ForceMode.Acceleration);
    }

    private void ApplySteering()
    {
        Vector3 torque =
            Vector3.up *
            steeringInput *
            turnAcceleration;

        rb.AddTorque(torque, ForceMode.Acceleration);
    }
}