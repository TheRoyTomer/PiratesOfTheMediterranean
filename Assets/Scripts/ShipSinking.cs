using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ShipHealth), typeof(Rigidbody))]
public class ShipSinking : MonoBehaviour
{
    [Header("Death Timing")]
    [Tooltip("Seconds after death before rolling begins, allowing the death smoke to fade first.")]
    [Min(0f)]
    [SerializeField] private float sinkingStartDelay = 8f;

    [Header("Hit Side")]
    [Tooltip("Local-space distance from the centerline within which the roll side is chosen randomly.")]
    [Min(0f)]
    [SerializeField] private float centerlineThreshold = 0.5f;

    [Header("Roll and Settle")]
    [Tooltip("Roll relative to the death orientation; 80 degrees leaves the ship almost on its side.")]
    [Range(0f, 90f)]
    [SerializeField] private float rollAngle = 80f;
    [Tooltip("Seconds to complete the roll and initial drop. Zero completes them immediately.")]
    [Min(0f)]
    [SerializeField] private float rollDuration = 6f;
    [Tooltip("World-space downward distance covered during the roll.")]
    [Min(0f)]
    [SerializeField] private float rollDropDistance = 2f;

    [Header("Sink and Disable")]
    [Tooltip("World units per second during the straight downward phase. Must be greater than zero.")]
    [Min(0f)]
    [SerializeField] private float sinkSpeed = 2f;
    [Tooltip("Total downward distance from the death position, including the roll drop. Tune until the rolled ship is fully submerged before it is disabled.")]
    [Min(0f)]
    [SerializeField] private float sinkDepth = 30f;

    private enum SinkingPhase { Alive, Waiting, Rolling, Sinking, Finished }

    private ShipHealth shipHealth;
    private Rigidbody body;
    private SinkingPhase phase;
    private Vector3 deathPosition;
    private Quaternion deathRotation;
    private Quaternion rolledRotation;
    private float rollElapsed;
    private float waitingElapsed;
    private float currentDrop;

    private void Awake()
    {
        shipHealth = GetComponent<ShipHealth>();
        body = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        shipHealth.OnDeath += BeginSinking;
        if (shipHealth.IsDead)
            BeginSinking();
    }

    private void OnDisable()
    {
        shipHealth.OnDeath -= BeginSinking;
    }

    private void OnValidate()
    {
        sinkDepth = Mathf.Max(sinkDepth, rollDropDistance);
    }

    private void BeginSinking()
    {
        if (phase != SinkingPhase.Alive)
            return;

        if (sinkSpeed <= 0f || sinkDepth < rollDropDistance)
        {
            Debug.LogError($"{name}: ShipSinking requires positive sinkSpeed and sinkDepth at least as large as rollDropDistance.", this);
            return;
        }

        float hitSide = transform.InverseTransformPoint(shipHealth.FinalHitPosition).x;
        // Positive local Z rotation lowers the left side; negative lowers the right.
        float rollSign = hitSide < -centerlineThreshold ? 1f
            : hitSide > centerlineThreshold ? -1f
            : (Random.Range(0, 2) == 0 ? 1f : -1f);

        deathPosition = body.position;
        deathRotation = body.rotation;
        rolledRotation = deathRotation * Quaternion.AngleAxis(rollSign * rollAngle, Vector3.forward);
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.isKinematic = true;
        phase = sinkingStartDelay > 0f ? SinkingPhase.Waiting : SinkingPhase.Rolling;
    }

    private void FixedUpdate()
    {
        switch (phase)
        {
            case SinkingPhase.Waiting:
                waitingElapsed += Time.fixedDeltaTime;
                if (waitingElapsed >= sinkingStartDelay)
                    phase = SinkingPhase.Rolling;
                break;

            case SinkingPhase.Rolling:
                rollElapsed += Time.fixedDeltaTime;
                float progress = rollDuration > 0f ? Mathf.Clamp01(rollElapsed / rollDuration) : 1f;
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                currentDrop = rollDropDistance * easedProgress;
                body.MoveRotation(Quaternion.Slerp(deathRotation, rolledRotation, easedProgress));
                body.MovePosition(deathPosition + Vector3.down * currentDrop);
                if (progress >= 1f)
                    phase = SinkingPhase.Sinking;
                break;

            case SinkingPhase.Sinking:
                currentDrop = Mathf.MoveTowards(currentDrop, sinkDepth, sinkSpeed * Time.fixedDeltaTime);
                body.MoveRotation(rolledRotation);
                body.MovePosition(deathPosition + Vector3.down * currentDrop);
                if (currentDrop >= sinkDepth)
                    phase = SinkingPhase.Finished;
                break;

            case SinkingPhase.Finished:
                // Allow the last physics move to complete before disabling the root.
                gameObject.SetActive(false);
                break;
        }
    }
}
