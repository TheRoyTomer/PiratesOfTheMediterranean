using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ShipHealth), typeof(Rigidbody))]
[RequireComponent(typeof(ShipConfiguration))]
public class ShipSinking : MonoBehaviour
{
    private ShipConfig config;

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
        config = GetComponent<ShipConfiguration>().Config;
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

    private void BeginSinking()
    {
        if (phase != SinkingPhase.Alive)
            return;

        if (config.Sinking.SinkSpeed <= 0f || config.Sinking.SinkDepth < config.Sinking.RollDropDistance)
        {
            Debug.LogError($"{name}: ShipSinking requires positive sinkSpeed and sinkDepth at least as large as rollDropDistance.", this);
            return;
        }

        float hitSide = transform.InverseTransformPoint(shipHealth.FinalHitPosition).x;
        // Positive local Z rotation lowers the left side; negative lowers the right.
        float rollSign = hitSide < -config.Sinking.CenterlineThreshold ? 1f
            : hitSide > config.Sinking.CenterlineThreshold ? -1f
            : (Random.Range(0, 2) == 0 ? 1f : -1f);

        deathPosition = body.position;
        deathRotation = body.rotation;
        rolledRotation = deathRotation * Quaternion.AngleAxis(rollSign * config.Sinking.RollAngle, Vector3.forward);
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.isKinematic = true;
        phase = config.Sinking.SinkingStartDelay > 0f ? SinkingPhase.Waiting : SinkingPhase.Rolling;
    }

    private void FixedUpdate()
    {
        switch (phase)
        {
            case SinkingPhase.Waiting:
                waitingElapsed += Time.fixedDeltaTime;
                if (waitingElapsed >= config.Sinking.SinkingStartDelay)
                    phase = SinkingPhase.Rolling;
                break;

            case SinkingPhase.Rolling:
                rollElapsed += Time.fixedDeltaTime;
                float progress = config.Sinking.RollDuration > 0f ? Mathf.Clamp01(rollElapsed / config.Sinking.RollDuration) : 1f;
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                currentDrop = config.Sinking.RollDropDistance * easedProgress;
                body.MoveRotation(Quaternion.Slerp(deathRotation, rolledRotation, easedProgress));
                body.MovePosition(deathPosition + Vector3.down * currentDrop);
                if (progress >= 1f)
                    phase = SinkingPhase.Sinking;
                break;

            case SinkingPhase.Sinking:
                currentDrop = Mathf.MoveTowards(currentDrop, config.Sinking.SinkDepth, config.Sinking.SinkSpeed * Time.fixedDeltaTime);
                body.MoveRotation(rolledRotation);
                body.MovePosition(deathPosition + Vector3.down * currentDrop);
                if (currentDrop >= config.Sinking.SinkDepth)
                    phase = SinkingPhase.Finished;
                break;

            case SinkingPhase.Finished:
                // Allow the last physics move to complete before disabling the root.
                gameObject.SetActive(false);
                break;
        }
    }
}
