using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ShipConfig", menuName = "Ships/Ship Config")]
public sealed class ShipConfig : ScriptableObject
{
    [Serializable]
    public sealed class MovementSettings
    {
        [Tooltip("Forward acceleration in world units per second squared.")]
        [SerializeField] private float acceleration = 15f;
        [Tooltip("Longitudinal braking acceleration in world units per second squared.")]
        [Min(0f)] [SerializeField] private float brakeAcceleration = 10f;
        [Tooltip("Horizontal sideways velocity damping rate per second.")]
        [Min(0f)] [SerializeField] private float lateralDrag = 1f;
        [Tooltip("Steering torque acceleration.")]
        [SerializeField] private float turnAcceleration = 2f;
        [Tooltip("Acceleration multiplier while boosting.")]
        [SerializeField] private float boostMultiplier = 1.5f;
        [Tooltip("Angular speed threshold for applying steering torque.")]
        [SerializeField] private float maxTurnSpeed = 0.6f;
        [Tooltip("Forward speed at which steering reaches full effectiveness.")]
        [Min(0.01f)]
        [SerializeField] private float fullSteeringSpeed = 20f;

        public float Acceleration => acceleration;
        public float BrakeAcceleration => brakeAcceleration;
        public float LateralDrag => lateralDrag;
        public float TurnAcceleration => turnAcceleration;
        public float BoostMultiplier => boostMultiplier;
        public float MaxTurnSpeed => maxTurnSpeed;
        public float FullSteeringSpeed => fullSteeringSpeed;
    }

    [Serializable]
    public sealed class CombatSettings
    {
        [Tooltip("Health assigned when a ship initializes.")]
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("Seconds of cooldown after firing.")]
        [SerializeField] private float cooldownDuration = 2f;
        [Tooltip("Additional cooldown when another firing direction is cooling down.")]
        [SerializeField] private float extraCooldownPenalty = 2f;
        [Tooltip("Cannonball launch speed in world units per second.")]
        [SerializeField] private float cannonballSpeed = 150f;

        public float MaxHealth => maxHealth;
        public float CooldownDuration => cooldownDuration;
        public float ExtraCooldownPenalty => extraCooldownPenalty;
        public float CannonballSpeed => cannonballSpeed;
    }

    [Serializable]
    public sealed class DeathEffectsSettings
    {
        [Tooltip("Seconds between consecutive death explosions.")]
        [Min(0f)] [SerializeField] private float explosionDelay = 1f;
        [Tooltip("Seconds after each individual explosion starts before its smoke starts.")]
        [UnityEngine.Serialization.FormerlySerializedAs("smokeDelay")]
        [Min(0f)] [SerializeField] private float smokeStartAfterExplosion = 0.7f;

        public float ExplosionDelay => explosionDelay;
        public float SmokeStartAfterExplosion => smokeStartAfterExplosion;
    }

    [Serializable]
    public sealed class SinkingSettings
    {
        [Tooltip("Fallback delay only for ships without active ShipDeathEffects. Otherwise explosion completion starts slow roll.")]
        [Min(0f)] [SerializeField] private float sinkingStartDelay = 8f;
        [Tooltip("Local-space centerline band in which the roll side is chosen randomly.")]
        [Min(0f)] [SerializeField] private float centerlineThreshold = 0.5f;
        [Tooltip("Roll angle relative to the death orientation, in degrees.")]
        [Range(0f, 90f)] [SerializeField] private float rollAngle = 80f;
        [Tooltip("Legacy setting. Phased capsize now uses slowRollSpeed and fastRollSpeed.")]
        [Min(0f)] [SerializeField] private float rollDuration = 6f;
        [Tooltip("Degrees per second during slow roll; holds at slowRollTargetAngle until the phase timer ends.")]
        [Min(0.01f)] [SerializeField] private float slowRollSpeed = 3.0f;
        [Tooltip("Seconds in the slow-roll phase before fast capsize begins, independent of smoke.")]
        [Min(0f)] [SerializeField] private float slowRollPhaseDuration = 5f;
        [Tooltip("Degrees per second after the slow-roll phase timer ends.")]
        [Min(0.01f)] [SerializeField] private float fastRollSpeed = 15f;
        [Tooltip("Maximum subtle lean during the slow-roll phase, relative to the death orientation.")]
        [Range(0f, 45f)] [SerializeField] private float slowRollTargetAngle = 15f;
        [Tooltip("World-space downward distance covered during the roll.")]
        [Min(0f)] [SerializeField] private float rollDropDistance = 2f;
        [Tooltip("World units per second during sinking. Must be greater than zero.")]
        [Min(0f)] [SerializeField] private float sinkSpeed = 2f;
        [Tooltip("Total downward distance from the death position, including the roll drop.")]
        [Min(0f)] [SerializeField] private float sinkDepth = 30f;

        public float SinkingStartDelay => sinkingStartDelay;
        public float CenterlineThreshold => centerlineThreshold;
        public float RollAngle => rollAngle;
        public float RollDuration => rollDuration;
        public float SlowRollSpeed => slowRollSpeed;
        public float SlowRollPhaseDuration => slowRollPhaseDuration;
        public float FastRollSpeed => fastRollSpeed;
        public float SlowRollTargetAngle => slowRollTargetAngle;
        public float RollDropDistance => rollDropDistance;
        public float SinkSpeed => sinkSpeed;
        public float SinkDepth => sinkDepth;

        internal void Validate(ShipConfig owner)
        {
            sinkDepth = Mathf.Max(sinkDepth, rollDropDistance);
            if (sinkSpeed <= 0f)
                Debug.LogError($"{owner.name}: ShipConfig requires a positive sinkSpeed.", owner);
        }
    }

    [SerializeField] private MovementSettings movement = new MovementSettings();
    [SerializeField] private CombatSettings combat = new CombatSettings();
    [SerializeField] private DeathEffectsSettings deathEffects = new DeathEffectsSettings();
    [SerializeField] private SinkingSettings sinking = new SinkingSettings();

    public MovementSettings Movement => movement;
    public CombatSettings Combat => combat;
    public DeathEffectsSettings DeathEffects => deathEffects;
    public SinkingSettings Sinking => sinking;

    private void OnValidate()
    {
        sinking.Validate(this);
    }
}
