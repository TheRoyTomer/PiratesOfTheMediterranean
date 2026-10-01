using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene-owned particle pools. Effects stay owned by the pool even while following a ship.</summary>
[DisallowMultipleComponent]
public sealed class VfxPool : MonoBehaviour
{
    private sealed class Bucket
    {
        public GameObject Prefab;
        public readonly Stack<Effect> Available = new();
        public int Count;
    }

    private sealed class Effect
    {
        public Bucket Bucket;
        public GameObject Object;
        public Transform Transform;
        public ParticleSystem[] Particles;
        public Transform Follow;
        public bool HasFollow;
        public Vector3 Position;
        public Quaternion Rotation;
        public float StartAt;
        public float Lifetime;
        public bool Started;
        public int StartFrame;
    }

    private readonly Dictionary<GameObject, Bucket> buckets = new();
    private readonly List<Effect> active = new(512);
    private Transform storage;

    public int CreatedCount { get; private set; }
    public int ReusedCount { get; private set; }
    public int ActiveCount => active.Count;

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null) return;
        Bucket bucket = GetBucket(prefab);
        while (bucket.Count < count)
            bucket.Available.Push(Create(bucket));
    }

    /// <param name="lifetime">Existing timed effects keep their lifetime; zero returns when particles finish.</param>
    public void Play(GameObject prefab, Vector3 position, Quaternion rotation,
        float lifetime = 0f, Transform follow = null, float delay = 0f)
    {
        if (prefab == null || !isActiveAndEnabled) return;
        Bucket bucket = GetBucket(prefab);
        Effect effect = null;
        while (bucket.Available.Count > 0 && effect == null)
        {
            Effect candidate = bucket.Available.Pop();
            if (candidate.Object != null) effect = candidate;
            else bucket.Count--;
        }
        if (effect == null) effect = Create(bucket);
        else ReusedCount++;

        effect.Follow = follow;
        effect.HasFollow = follow != null;
        effect.Position = effect.HasFollow ? follow.InverseTransformPoint(position) : position;
        effect.Rotation = effect.HasFollow ? Quaternion.Inverse(follow.rotation) * rotation : rotation;
        effect.Lifetime = Mathf.Max(0f, lifetime);
        effect.StartAt = Time.time + Mathf.Max(0f, delay);
        effect.Started = false;
        active.Add(effect);
        if (delay <= 0f) StartEffect(effect);
    }

    private Bucket GetBucket(GameObject prefab)
    {
        if (buckets.TryGetValue(prefab, out Bucket bucket)) return bucket;
        if (storage == null)
        {
            var root = new GameObject("Inactive VFX");
            root.SetActive(false);
            root.transform.SetParent(transform, false);
            storage = root.transform;
        }
        bucket = new Bucket { Prefab = prefab };
        buckets.Add(prefab, bucket);
        return bucket;
    }

    private Effect Create(Bucket bucket)
    {
        // Inactive staging prevents play-on-awake and KWS registration before reset/positioning.
        GameObject instance = Instantiate(bucket.Prefab, storage);
        instance.SetActive(false);
        var particles = instance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem system in particles)
        {
            var main = system.main;
            // Muzzle flash and smoke prefabs otherwise destroy themselves after playing.
            main.stopAction = ParticleSystemStopAction.None;
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        bucket.Count++;
        CreatedCount++;
        return new Effect { Bucket = bucket, Object = instance, Transform = instance.transform, Particles = particles };
    }

    private void StartEffect(Effect effect)
    {
        effect.Transform.SetParent(transform, false);
        effect.Transform.localScale = effect.Bucket.Prefab.transform.localScale;
        UpdatePose(effect);
        effect.Started = true;
        effect.StartAt = Time.time;
        effect.StartFrame = Time.frameCount;
        // Re-enabling also resets the splash's KWS effector and animation curve via OnEnable.
        // Keep prefab child activation/playOnAwake settings, including sub-emitters, intact.
        effect.Object.SetActive(true);
    }

    private static void UpdatePose(Effect effect)
    {
        Vector3 position = effect.HasFollow ? effect.Follow.TransformPoint(effect.Position) : effect.Position;
        Quaternion rotation = effect.HasFollow ? effect.Follow.rotation * effect.Rotation : effect.Rotation;
        effect.Transform.SetPositionAndRotation(position, rotation);
    }

    private void LateUpdate()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Effect effect = active[i];
            if (effect.Object == null)
            {
                effect.Bucket.Count--;
                RemoveActive(i);
                continue;
            }
            if (effect.HasFollow && (effect.Follow == null || !effect.Follow.gameObject.activeInHierarchy))
            {
                Return(i);
                continue;
            }
            if (!effect.Started)
            {
                if (Time.time >= effect.StartAt) StartEffect(effect);
                continue;
            }
            if (effect.HasFollow) UpdatePose(effect);
            if (Time.frameCount == effect.StartFrame) continue;
            // Timed splash effects include a water simulation effector as well as particles.
            bool finished = effect.Lifetime > 0f
                ? Time.time - effect.StartAt >= effect.Lifetime
                : !IsAlive(effect);
            if (finished) Return(i);
        }
    }

    private static bool IsAlive(Effect effect)
    {
        foreach (ParticleSystem system in effect.Particles)
            if (system != null && system.gameObject.activeInHierarchy && system.IsAlive(false)) return true;
        return false;
    }

    private void Return(int index)
    {
        Effect effect = active[index];
        foreach (ParticleSystem system in effect.Particles)
            if (system != null) system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Object.SetActive(false);
        effect.Transform.SetParent(storage, false);
        effect.Follow = null;
        effect.HasFollow = false;
        effect.Started = false;
        effect.Bucket.Available.Push(effect);
        RemoveActive(index);
    }

    private void RemoveActive(int index)
    {
        active[index] = active[active.Count - 1];
        active.RemoveAt(active.Count - 1);
    }

    private void OnDisable()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i].Object != null) Return(i);
            else RemoveActive(i);
        }
    }
}
