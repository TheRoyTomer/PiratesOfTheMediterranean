using UnityEngine;
using KWS;

[DisallowMultipleComponent]
[RequireComponent(typeof(ShipHealth), typeof(Rigidbody))]
[RequireComponent(typeof(ShipConfiguration))]
public class ShipSinking : MonoBehaviour
{
    private ShipConfig config;

    private enum SinkingPhase
    {
        Alive,
        Waiting,
        SlowRoll,
        FastCapsize,
        Sinking,
        Finished
    }
    
    [Header("Enemy Ship Parts")]
    [SerializeField] private GameObject shipPartsPickupPrefab;
    [SerializeField] private float shipPartsRiseDepth = 3f;
    [SerializeField] private float shipPartsRiseDuration = 1.5f;

    private bool shipPartsPickupSpawned;

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

    [Header("Water Impact Splashes")]
    [SerializeField] private GameObject waterDeathSplashPrefab;
    [SerializeField] private float waterSplashLifetime = 5f;

    // Automatically found on Ship.prefab.
    private Transform hullImpactBowPort;
    private Transform hullImpactMidPort;
    private Transform hullImpactSternPort;
    private Transform mastImpactPort;

    private Transform hullImpactBowStarboard;
    private Transform hullImpactMidStarboard;
    private Transform hullImpactSternStarboard;
    private Transform mastImpactStarboard;

    private bool bowSplashTriggered;
    private bool midSplashTriggered;
    private bool sternSplashTriggered;
    private bool mastSplashTriggered;

    private void Awake()
    {
        config = GetComponent<ShipConfiguration>().Config;
        shipHealth = GetComponent<ShipHealth>();
        body = GetComponent<Rigidbody>();
        deathEffects = GetComponent<ShipDeathEffects>();

        FindWaterImpactMarkers();
    }

    private void FindWaterImpactMarkers()
    {
        hullImpactBowPort = transform.Find("HullImpact_Bow_Port");
        hullImpactMidPort = transform.Find("HullImpact_Mid_Port");
        hullImpactSternPort = transform.Find("HullImpact_Stern_Port");
        mastImpactPort = transform.Find("MastImpact_Port");

        hullImpactBowStarboard = transform.Find("HullImpact_Bow_Starboard");
        hullImpactMidStarboard = transform.Find("HullImpact_Mid_Starboard");
        hullImpactSternStarboard = transform.Find("HullImpact_Stern_Starboard");
        mastImpactStarboard = transform.Find("MastImpact_Starboard");

        if (hullImpactBowPort == null ||
            hullImpactMidPort == null ||
            hullImpactSternPort == null ||
            mastImpactPort == null ||
            hullImpactBowStarboard == null ||
            hullImpactMidStarboard == null ||
            hullImpactSternStarboard == null ||
            mastImpactStarboard == null)
        {
            Debug.LogError(
                $"{name}: One or more water impact markers could not be found.",
                this
            );
        }
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

        if (config.Sinking.SinkSpeed <= 0f ||
            config.Sinking.SinkDepth < config.Sinking.RollDropDistance)
        {
            Debug.LogError(
                $"{name}: ShipSinking requires positive sinkSpeed and sinkDepth at least as large as rollDropDistance.",
                this
            );
            return;
        }

        bowSplashTriggered = false;
        midSplashTriggered = false;
        sternSplashTriggered = false;
        mastSplashTriggered = false;

        float hitSide =
            transform.InverseTransformPoint(
                shipHealth.FinalHitPosition
            ).x;

        // Positive local Z rotation lowers the left/port side.
        // Negative local Z rotation lowers the right/starboard side.
        rollSign =
            hitSide < -config.Sinking.CenterlineThreshold ? 1f :
            hitSide > config.Sinking.CenterlineThreshold ? -1f :
            (Random.Range(0, 2) == 0 ? 1f : -1f);

        deathPosition = body.position;
        deathRotation = body.rotation;

        rolledRotation =
            deathRotation *
            Quaternion.AngleAxis(
                rollSign * config.Sinking.RollAngle,
                Vector3.forward
            );

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
                if (deathEffects != null &&
                    deathEffects.isActiveAndEnabled)
                {
                    if (deathEffects.HasFinalExplosionStarted)
                        BeginSlowRoll();
                }
                else
                {
                    waitingElapsed += Time.fixedDeltaTime;

                    if (waitingElapsed >=
                        config.Sinking.SinkingStartDelay)
                    {
                        phase = SinkingPhase.FastCapsize;
                    }
                }
                break;

            case SinkingPhase.SlowRoll:
                MoveRoll(
                    Mathf.Min(
                        config.Sinking.SlowRollTargetAngle,
                        config.Sinking.RollAngle
                    ),
                    config.Sinking.SlowRollSpeed
                );

                slowRollElapsed += Time.fixedDeltaTime;

                if (slowRollElapsed >=
                    config.Sinking.SlowRollPhaseDuration)
                {
                    fastRollStartAngle = currentRollAngle;
                    phase = SinkingPhase.FastCapsize;
                }
                break;

            case SinkingPhase.FastCapsize:
                MoveRoll(
                    config.Sinking.RollAngle,
                    config.Sinking.FastRollSpeed
                );

                CheckWaterImpactMarkers();

                float progress =
                    Mathf.Approximately(
                        fastRollStartAngle,
                        config.Sinking.RollAngle
                    )
                        ? 1f
                        : Mathf.InverseLerp(
                            fastRollStartAngle,
                            config.Sinking.RollAngle,
                            currentRollAngle
                        );

                currentDrop =
                    config.Sinking.RollDropDistance * progress;

                body.MovePosition(
                    deathPosition +
                    Vector3.down * currentDrop
                );

                if (progress >= 1f)
                    phase = SinkingPhase.Sinking;

                break;

            case SinkingPhase.Sinking:
                currentDrop =
                    Mathf.MoveTowards(
                        currentDrop,
                        config.Sinking.SinkDepth,
                        config.Sinking.SinkSpeed *
                        Time.fixedDeltaTime
                    );

                body.MoveRotation(rolledRotation);

                body.MovePosition(
                    deathPosition +
                    Vector3.down * currentDrop
                );

                if (currentDrop >= config.Sinking.SinkDepth)
                    phase = SinkingPhase.Finished;

                break;

            case SinkingPhase.Finished:
                SpawnShipPartsPickup();
                gameObject.SetActive(false);
                break;
        }
    }

    private void MoveRoll(float targetAngle, float speed)
    {
        currentRollAngle =
            Mathf.MoveTowards(
                currentRollAngle,
                targetAngle,
                Mathf.Max(0.01f, speed) *
                Time.fixedDeltaTime
            );

        body.MoveRotation(
            deathRotation *
            Quaternion.AngleAxis(
                rollSign * currentRollAngle,
                Vector3.forward
            )
        );
    }

    private void CheckWaterImpactMarkers()
    {
        if (WaterSystem.Instance == null)
            return;

        float waterY = WaterSystem.Instance.WaterLevel;

        Transform bow;
        Transform mid;
        Transform stern;
        Transform mast;

        if (rollSign > 0f)
        {
            // Port side is falling.
            bow = hullImpactBowPort;
            mid = hullImpactMidPort;
            stern = hullImpactSternPort;
            mast = mastImpactPort;
        }
        else
        {
            // Starboard side is falling.
            bow = hullImpactBowStarboard;
            mid = hullImpactMidStarboard;
            stern = hullImpactSternStarboard;
            mast = mastImpactStarboard;
        }

        if (!bowSplashTriggered &&
            IsTouchingWater(bow, waterY))
        {
            bowSplashTriggered = true;
            SpawnWaterSplash(bow, waterY);
        }

        if (!midSplashTriggered &&
            IsTouchingWater(mid, waterY))
        {
            midSplashTriggered = true;
            SpawnWaterSplash(mid, waterY);
        }

        if (!sternSplashTriggered &&
            IsTouchingWater(stern, waterY))
        {
            sternSplashTriggered = true;
            SpawnWaterSplash(stern, waterY);
        }

        if (!mastSplashTriggered &&
            IsTouchingWater(mast, waterY))
        {
            mastSplashTriggered = true;
            SpawnWaterSplash(mast, waterY);
        }
    }

    private bool IsTouchingWater(
        Transform point,
        float waterY
    )
    {
        return
            point != null &&
            point.position.y <= waterY;
    }

    private void SpawnWaterSplash(
        Transform point,
        float waterY
    )
    {
        if (waterDeathSplashPrefab == null ||
            point == null)
        {
            return;
        }

        Vector3 spawnPosition = point.position;

        // Spawn directly on the water surface.
        spawnPosition.y = waterY;

        GameObject splash =
            Instantiate(
                waterDeathSplashPrefab,
                spawnPosition,
                waterDeathSplashPrefab.transform.rotation
            );

        Destroy(
            splash,
            waterSplashLifetime
        );
    }
    
    private void SpawnShipPartsPickup()
    {
        if (shipPartsPickupSpawned ||
            shipPartsPickupPrefab == null ||
            GetComponent<AIController>() == null)
            return;

        shipPartsPickupSpawned = true;

        Vector3 spawnPosition = deathPosition;
        spawnPosition.y = WaterSystem.Instance != null
            ? WaterSystem.Instance.WaterLevel
            : 0f;

        GameObject pickup = Instantiate(
            shipPartsPickupPrefab,
            spawnPosition,
            shipPartsPickupPrefab.transform.rotation
        );

        ShipPartsController partsController = pickup.GetComponent<ShipPartsController>();
        if (partsController != null)
            partsController.BeginPickup();
    }
}