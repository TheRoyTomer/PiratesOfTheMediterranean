using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ShipConfiguration))]
public class ShipController : MonoBehaviour
{
    private ShipConfig config;
    

    private Rigidbody rb;
    private ShipHealth shipHealth;

    private float throttleInput;
    private float steeringInput;
    private bool boostActive;

    private void Awake()
    {
        config = GetComponent<ShipConfiguration>().Config;
        rb = GetComponent<Rigidbody>();
        shipHealth = GetComponent<ShipHealth>();
    }

    private void OnEnable()
    {
        if (shipHealth == null)
            return;

        shipHealth.OnDeath += StopMovement;

        if (shipHealth.IsDead)
            StopMovement();
    }

    private void OnDisable()
    {
        if (shipHealth != null)
            shipHealth.OnDeath -= StopMovement;
    }

    private void StopMovement()
    {
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        throttleInput = 0f;
        steeringInput = 0f;
        boostActive = false;
    }

    private void FixedUpdate()
    {
        if (shipHealth != null && shipHealth.IsDead)
        {
            StopMovement();
            return;
        }

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
        float multiplier = boostActive ? config.Movement.BoostMultiplier : 1f;

        Vector3 force =
            transform.forward *
            throttleInput *
            config.Movement.Acceleration *
            multiplier;

        rb.AddForce(force, ForceMode.Acceleration);
    }

    private void ApplySteering()
    {
        if (Mathf.Abs(rb.angularVelocity.y) < config.Movement.MaxTurnSpeed)
        {
            Vector3 torque =
                Vector3.up *
                steeringInput *
                config.Movement.TurnAcceleration;

            rb.AddTorque(torque, ForceMode.Acceleration);
        }
    }
}
