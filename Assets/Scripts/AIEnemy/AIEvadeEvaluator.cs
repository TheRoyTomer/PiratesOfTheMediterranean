using UnityEngine;
using System.Collections.Generic;

public sealed class AIEvadeEvaluator : MonoBehaviour
{
    public enum DamageDecision
    {
        None,
        EnterEvade,
        ExitEvadeToChase
    }

    [Header("Evade Decision")]
    [SerializeField] private int burstHitCount = 3;
    [SerializeField] private float burstHitWindow = 2.5f;
    [SerializeField, Range(0f, 1f)]
    private float lowHealthThreshold = 0.3f;

    [Header("Evade Cooldown")]
    [SerializeField] private float evadeCooldown = 6f;

    [Header("Evade Failure")]
    [SerializeField] private int evadeFailureHitCount = 2;
    [SerializeField] private float evadeFailureHitWindow = 3f;
    [SerializeField] private float evadeFailureGraceTime = 5f;

    private readonly List<float> recentHitTimes =
        new List<float>();

    private readonly List<float> evadeHitTimes =
        new List<float>();

    private float evadeCooldownTimer;

    private float lastDamageTime =
        Mathf.NegativeInfinity;

    public float LastDamageTime => lastDamageTime;

    public void Tick(float deltaTime)
    {
        if (evadeCooldownTimer > 0f)
        {
            evadeCooldownTimer -= deltaTime;
        }
    }

    public DamageDecision EvaluateDamage(
        ShipHealth shipHealth,
        bool isEvading,
        bool canEnterEvade,
        float evadeTimer)
    {
        lastDamageTime = Time.time;

        if (isEvading &&
            evadeTimer >= evadeFailureGraceTime)
        {
            evadeHitTimes.Add(Time.time);

            float evadeCutoffTime =
                Time.time - evadeFailureHitWindow;

            evadeHitTimes.RemoveAll(
                hitTime => hitTime < evadeCutoffTime
            );

            if (evadeHitTimes.Count >=
                evadeFailureHitCount)
            {
                evadeHitTimes.Clear();

                return DamageDecision.ExitEvadeToChase;
            }
        }

        recentHitTimes.Add(Time.time);

        float cutoffTime =
            Time.time - burstHitWindow;

        recentHitTimes.RemoveAll(
            hitTime => hitTime < cutoffTime
        );

        bool burstTriggered =
            recentHitTimes.Count >= burstHitCount;

        bool lowHealthTriggered =
            shipHealth != null &&
            shipHealth.MaxHealth > 0f &&
            shipHealth.CurrentHealth /
            shipHealth.MaxHealth <= lowHealthThreshold;

        bool cooldownReady =
            evadeCooldownTimer <= 0f;

        if (canEnterEvade &&
            cooldownReady &&
            (burstTriggered || lowHealthTriggered))
        {
            return DamageDecision.EnterEvade;
        }

        return DamageDecision.None;
    }

    public void NotifyEvadeStarted()
    {
        evadeHitTimes.Clear();
    }

    public void NotifyEvadeEnded()
    {
        evadeCooldownTimer = evadeCooldown;
    }
}