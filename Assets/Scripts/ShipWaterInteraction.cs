using KWS;
using UnityEngine;

/// <summary>Coordinates water effects with the existing ship lifecycle; never drives the ship.</summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(ShipHealth))]
public sealed class ShipWaterInteraction : MonoBehaviour
{
    [SerializeField] private KWS_Buoyancy buoyancy;
    [SerializeField] private KWS_DynamicWavesSimulationEffector[] wakeEffectors;
    [SerializeField] private ParticleSystem[] sideFoam;
    [SerializeField] private ParticleSystem[] sideBubbles;
    [SerializeField] private ParticleSystem[] waterParticles;
    [SerializeField] private Behaviour[] emissionControllers;
    [SerializeField, Min(0f)] private float sideFoamPerMeter = 0.18f;
    [SerializeField, Min(0f)] private float sideBubblesPerMeter = 0.06f;
    [SerializeField, Min(0f)] private float minimumFoamSpeed = 0.5f;
    [SerializeField, Min(0.01f)] private float fullFoamSpeed = 4f;

    private Rigidbody body;
    private ShipHealth health;
    private bool suspended;

    public bool IsSuspended => suspended;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        health = GetComponent<ShipHealth>();
        SetSideEmission(0f);
    }

    private void OnEnable()
    {
        health.OnDeath += Suspend;
        CheckSuspension();
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= Suspend;
        SetSideEmission(0f);
    }

    private void FixedUpdate()
    {
        // Runs before buoyancy so a kinematic sinking body never receives its forces.
        CheckSuspension();
    }

    private void Update()
    {
        CheckSuspension();
        if (suspended)
            return;

        float speed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
        float fraction = Mathf.InverseLerp(minimumFoamSpeed,
            Mathf.Max(minimumFoamSpeed + 0.01f, fullFoamSpeed), speed);
        SetSideEmission(Mathf.SmoothStep(0f, 1f, fraction));
    }

    private void CheckSuspension()
    {
        if (!suspended && (health.IsDead || body.isKinematic))
            Suspend();
    }

    private void Suspend()
    {
        if (suspended)
            return;

        // Death is terminal in ShipSinking. Do not resume buoyancy during its manual motion.
        suspended = true;
        if (buoyancy != null)
            buoyancy.enabled = false;
        if (wakeEffectors != null)
            foreach (var effector in wakeEffectors)
                if (effector != null) effector.enabled = false;
        if (emissionControllers != null)
            foreach (var controller in emissionControllers)
                if (controller != null) controller.enabled = false;

        SetSideEmission(0f);
        if (waterParticles != null)
            foreach (var particles in waterParticles)
                if (particles != null)
                    particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        // The shared simulation zone stays alive so existing wake/foam can dissipate.
    }

    private void SetSideEmission(float multiplier)
    {
        SetRates(sideFoam, sideFoamPerMeter * multiplier);
        SetRates(sideBubbles, sideBubblesPerMeter * multiplier);
    }

    private static void SetRates(ParticleSystem[] systems, float rate)
    {
        if (systems == null) return;
        foreach (var particles in systems)
        {
            if (particles == null) continue;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = rate;
        }
    }
}
