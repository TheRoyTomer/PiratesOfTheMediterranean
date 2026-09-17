using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ShipHealth), typeof(Rigidbody))]
[RequireComponent(typeof(ShipConfiguration))]
public class ShipSinking : MonoBehaviour
{
    private ShipConfig config;

    private enum SinkingPhase { Alive, Waiting, SlowRoll, FastCapsize, Sinking, Finished }

    private ShipHealth shipHealth;
    private Rigidbody body;
    private ShipDeathEffects deathEffects;
    private SinkingPhase phase;
    private Vector3 deathPosition;
    private Quaternion deathRotation;
    private Quaternion rolledRotation;
    private float rollSign;
    private float currentRollAngle;
    private float fastRollStartAngle;
    private float waitingElapsed;
    private float slowRollElapsed;
    private float currentDrop;

    private void Awake()
    {
        config = GetComponent<ShipConfiguration>().Config;
        shipHealth = GetComponent<ShipHealth>();
        body = GetComponent<Rigidbody>();
        deathEffects = GetComponent<ShipDeathEffects>();
    }

    private void OnEnable()
    {
        shipHealth.OnDeath += BeginSinking;
        if (deathEffects != null)
            deathEffects.FinalExplosionStarted += BeginSlowRoll;
        if (shipHealth.IsDead)
            BeginSinking();
    }

    private void OnDisable()
    {
        shipHealth.OnDeath -= BeginSinking;
        if (deathEffects != null)
            deathEffects.FinalExplosionStarted -= BeginSlowRoll;
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
        rollSign = hitSide < -config.Sinking.CenterlineThreshold ? 1f
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
        phase = SinkingPhase.Waiting;
    }

    private void BeginSlowRoll()
    {
        if (phase != SinkingPhase.Waiting)
            return;
        slowRollElapsed = 0f;
        phase = SinkingPhase.SlowRoll;
    }

    private void FixedUpdate()
    {
        switch (phase)
        {
            case SinkingPhase.Waiting:
                if (deathEffects != null && deathEffects.isActiveAndEnabled)
                {
                    if (deathEffects.HasFinalExplosionStarted)
                        BeginSlowRoll();
                }
                else
                {
                    // Preserve a fallback for ships without a death-effects component.
                    waitingElapsed += Time.fixedDeltaTime;
                    if (waitingElapsed >= config.Sinking.SinkingStartDelay)
                        phase = SinkingPhase.FastCapsize;
                }
                break;

            case SinkingPhase.SlowRoll:
                MoveRoll(Mathf.Min(config.Sinking.SlowRollTargetAngle, config.Sinking.RollAngle),
                    config.Sinking.SlowRollSpeed);
                slowRollElapsed += Time.fixedDeltaTime;
                if (slowRollElapsed >= config.Sinking.SlowRollPhaseDuration)
                {
                    fastRollStartAngle = currentRollAngle;
                    phase = SinkingPhase.FastCapsize;
                }
                break;

            case SinkingPhase.FastCapsize:
                MoveRoll(config.Sinking.RollAngle, config.Sinking.FastRollSpeed);
                float progress = Mathf.Approximately(fastRollStartAngle, config.Sinking.RollAngle)
                    ? 1f : Mathf.InverseLerp(fastRollStartAngle, config.Sinking.RollAngle, currentRollAngle);
                currentDrop = config.Sinking.RollDropDistance * progress;
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

    private void MoveRoll(float targetAngle, float speed)
    {
        currentRollAngle = Mathf.MoveTowards(currentRollAngle, targetAngle,
            Mathf.Max(0.01f, speed) * Time.fixedDeltaTime);
        body.MoveRotation(deathRotation * Quaternion.AngleAxis(rollSign * currentRollAngle, Vector3.forward));
    }
}
