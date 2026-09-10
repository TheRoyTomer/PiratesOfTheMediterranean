using System;
using UnityEngine;

namespace RenderWave.Runtime.Wakes
{
    /// <summary>
    /// Practical per-emitter controls for wake generation.
    /// </summary>
    [Serializable]
    public sealed class WakeEmitterSettings
    {
        [SerializeField, Min(0f)] private float minSpeed = 1.5f;
        [SerializeField, Min(0.1f)] private float emissionRate = 8f;
        [SerializeField, Min(0.25f)] private float width = 3.5f;
        [SerializeField, Min(0.25f)] private float length = 7f;
        [SerializeField, Min(0.1f)] private float lifetime = 2.75f;
        [SerializeField, Range(0f, 1f)] private float fadeStart = 0.45f;
        [SerializeField, Range(0f, 2f)] private float intensity = 1f;
        [SerializeField, Min(0f)] private float surfaceContactTolerance = 0.35f;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.03f;

        public float MinSpeed => minSpeed;
        public float EmissionRate => emissionRate;
        public float Width => width;
        public float Length => length;
        public float Lifetime => lifetime;
        public float FadeStart => fadeStart;
        public float Intensity => intensity;
        public float SurfaceContactTolerance => surfaceContactTolerance;
        public float SurfaceOffset => surfaceOffset;

        public void Validate()
        {
            minSpeed = Mathf.Max(0f, minSpeed);
            emissionRate = Mathf.Max(0.1f, emissionRate);
            width = Mathf.Max(0.25f, width);
            length = Mathf.Max(0.25f, length);
            lifetime = Mathf.Max(0.1f, lifetime);
            fadeStart = Mathf.Clamp01(fadeStart);
            intensity = Mathf.Clamp(intensity, 0f, 2f);
            surfaceContactTolerance = Mathf.Max(0f, surfaceContactTolerance);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
        }
    }
}
