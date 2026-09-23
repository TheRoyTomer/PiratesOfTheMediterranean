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

    private float contactRecoveryTimer;

    public bool IsContactRecoveryActive => contactRecoveryTimer > 0f;

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

        contactRecoveryTimer = 0f;
    }

    private void FixedUpdate()
    {
        if (shipHealth != null && shipHealth.IsDead)
        {
            StopMovement();
            return;
        }

        if (contactRecoveryTimer > 0f)
            contactRecoveryTimer -= Time.fixedDeltaTime;
        else
            ApplyMovement();

        ApplyLateralDamping();
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

    public void StartContactRecovery(
        float kickSpeed,
        float throttleLockDuration)
    {
        if (rb.isKinematic)
            return;

        if (shipHealth != null && shipHealth.IsDead)
            return;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        if (forward.sqrMagnitude < 0.000001f)
            return;

        forward.Normalize();

        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, forward);
        float targetForwardSpeed = -Mathf.Max(0f, kickSpeed);

        rb.AddForce(
            forward * (targetForwardSpeed - currentForwardSpeed),
            ForceMode.VelocityChange
        );

        contactRecoveryTimer = Mathf.Max(
            contactRecoveryTimer,
            throttleLockDuration
        );
    }

    private void ApplyMovement()
    {
        if (throttleInput < 0f)
        {
            // Brake only along the horizontal forward axis, preserving lateral and vertical motion.
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            float speed = Vector3.Dot(rb.linearVelocity, forward);
            float nextSpeed = Mathf.MoveTowards(
                speed, 0f,
                Mathf.Max(0f, config.Movement.BrakeAcceleration) * -throttleInput * Time.fixedDeltaTime);

            rb.linearVelocity += forward * (nextSpeed - speed);
            return;
        }

        float multiplier = boostActive ? config.Movement.BoostMultiplier : 1f;

        Vector3 force =
            transform.forward *
            throttleInput *
            config.Movement.Acceleration *
            multiplier;

        rb.AddForce(force, ForceMode.Acceleration);
    }

    private void ApplyLateralDamping()
    {
        float drag = Mathf.Max(0f, config.Movement.LateralDrag);
        float deltaTime = Time.fixedDeltaTime;
        if (drag <= 0f || deltaTime <= 0f || rb.isKinematic)
            return;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.000001f)
            return;
        forward.Normalize();

        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up);
        right -= forward * Vector3.Dot(right, forward);
        if (right.sqrMagnitude < 0.000001f)
            right = Vector3.Cross(Vector3.up, forward);
        right.Normalize();

        float sideSpeed = Vector3.Dot(rb.linearVelocity, right);
        float dampingFraction = 1f - Mathf.Exp(-drag * deltaTime);
        // Center-of-mass force preserves forward/Y components and adds no torque.
        rb.AddForce(-right * (sideSpeed * dampingFraction / deltaTime), ForceMode.Acceleration);
    }

    private void ApplySteering()
    {
        Vector3 forward = Vector3.ProjectOnPlane(
            transform.forward,
            Vector3.up
        );

        if (forward.sqrMagnitude < 0.000001f)
            return;

        forward.Normalize();

        float forwardSpeed = Mathf.Max(
            0f,
            Vector3.Dot(rb.linearVelocity, forward)
        );

        float steeringEffectiveness = Mathf.Clamp01(
            forwardSpeed / config.Movement.FullSteeringSpeed
        );

        if (Mathf.Abs(rb.angularVelocity.y) < config.Movement.MaxTurnSpeed)
        {
            float torqueAmount =
                steeringInput *
                config.Movement.TurnAcceleration *
                steeringEffectiveness;

            Vector3 torque = Vector3.up * torqueAmount;

            rb.AddTorque(torque, ForceMode.Acceleration);
        }
    }
}