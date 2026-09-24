using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class ShipFoamEmissionController : MonoBehaviour
{
    [SerializeField] private ParticleSystem mainFoam;
    [SerializeField] private ParticleSystem bubbles;
    [SerializeField, Min(0f)] private float mainFoamPerMeter = 0.4f;
    [SerializeField, Min(0f)] private float bubblesPerMeter = 0.15f;

    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        SetEmission(0f);
    }

    private void Update()
    {
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
        float speedFraction = Mathf.InverseLerp(0.5f, 4f, horizontalVelocity.magnitude);
        SetEmission(Mathf.SmoothStep(0f, 1f, speedFraction));
    }

    private void OnDisable()
    {
        SetEmission(0f);
    }

    private void SetEmission(float multiplier)
    {
        // Only stop new emission; existing world-space particles finish their lifetime.
        if (mainFoam != null)
        {
            var emission = mainFoam.emission;
            emission.rateOverDistance = mainFoamPerMeter * multiplier;
        }

        if (bubbles != null)
        {
            var emission = bubbles.emission;
            emission.rateOverDistance = bubblesPerMeter * multiplier;
        }
    }
}
