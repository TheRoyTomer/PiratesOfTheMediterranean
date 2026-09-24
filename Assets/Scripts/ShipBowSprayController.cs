using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class ShipBowSprayController : MonoBehaviour
{
    [SerializeField] private ParticleSystem leftSpray;
    [SerializeField] private ParticleSystem rightSpray;
    [SerializeField, Min(0f)] private float maximumEmissionRate = 10f;

    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        SetEmission(0f);
    }

    private void Update()
    {
        float horizontalSpeed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
        float speedFraction = Mathf.InverseLerp(0.5f, 18.2f, horizontalSpeed);
        SetEmission(Mathf.SmoothStep(0f, 1f, speedFraction));
    }

    private void OnDisable()
    {
        SetEmission(0f);
    }

    private void SetEmission(float multiplier)
    {
        // Leave existing particles alive so they finish their lifetime naturally.
        if (leftSpray != null)
        {
            var emission = leftSpray.emission;
            emission.rateOverTime = maximumEmissionRate * multiplier;
        }

        if (rightSpray != null)
        {
            var emission = rightSpray.emission;
            emission.rateOverTime = maximumEmissionRate * multiplier;
        }
    }
}
