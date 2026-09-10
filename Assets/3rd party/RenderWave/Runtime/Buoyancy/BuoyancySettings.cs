using System;
using UnityEngine;

namespace RenderWave.Runtime.Buoyancy
{
    public enum BuoyancySampleMode
    {
        SinglePoint = 0,
        FourPoint = 1
    }

    /// <summary>
    /// Serialized settings for BuoyancyBody.
    /// Controls how a Rigidbody floats on the RenderWave water surface.
    /// </summary>
    [Serializable]
    public sealed class BuoyancySettings
    {
        [Tooltip("Sampling mode. Single Point is cheapest. Four Point improves pitch/roll on boats and rafts but costs four water queries per physics tick.")]
        [SerializeField]
        private BuoyancySampleMode sampleMode = BuoyancySampleMode.SinglePoint;

        [Tooltip("Upward force per meter of submersion. Higher values make the object pop to the surface faster.")]
        [SerializeField, Min(0.1f)]
        private float buoyancyForce = 15f;

        [Tooltip("Damping force opposing vertical velocity. Prevents endless bouncing.")]
        [SerializeField, Min(0f)]
        private float verticalDamping = 2f;

        [Tooltip("Maximum submersion depth that contributes to force. Prevents force explosion when spawning below water.")]
        [SerializeField, Min(0.25f)]
        private float submersionDepthClamp = 3f;

        [Tooltip("Target float height relative to the water surface. Positive values float above, negative values float below.")]
        [SerializeField, Range(-2f, 2f)]
        private float surfaceOffset;

        [Tooltip("Local-space position of the water sample point relative to the object origin.")]
        [SerializeField]
        private Vector3 samplePointOffset = Vector3.zero;

        [Tooltip("Half-size of the four-point sampling footprint in local X/Z. Only used in Four Point mode.")]
        [SerializeField]
        private Vector2 sampleFootprintHalfExtents = new Vector2(0.6f, 1.2f);

        [Tooltip("Time in seconds over which buoyancy force ramps up after activation. Prevents spawn explosions.")]
        [SerializeField, Min(0f)]
        private float forceRampDuration = 0.25f;

        [Tooltip("Strength of torque aligning the object's up axis to the water surface normal. Zero disables alignment entirely.")]
        [SerializeField, Min(0f)]
        private float alignmentStrength = 2f;

        [Tooltip("Damping torque opposing angular velocity during alignment. Prevents oscillation.")]
        [SerializeField, Min(0f)]
        private float alignmentDamping = 1.5f;

        /// <summary>Water sampling mode. Single Point is the safe default.</summary>
        public BuoyancySampleMode SampleMode => sampleMode;

        /// <summary>Upward force per meter of submersion.</summary>
        public float BuoyancyForce => buoyancyForce;

        /// <summary>Damping force opposing vertical velocity.</summary>
        public float VerticalDamping => verticalDamping;

        /// <summary>Maximum submersion depth that contributes to buoyancy force.</summary>
        public float SubmersionDepthClamp => submersionDepthClamp;

        /// <summary>Height offset relative to the water surface the object targets.</summary>
        public float SurfaceOffset => surfaceOffset;

        /// <summary>Local-space offset for the water query sample point.</summary>
        public Vector3 SamplePointOffset => samplePointOffset;

        /// <summary>Half-size of the four-point sampling footprint in local X/Z.</summary>
        public Vector2 SampleFootprintHalfExtents => sampleFootprintHalfExtents;

        /// <summary>Seconds over which force ramps from zero to full after activation.</summary>
        public float ForceRampDuration => forceRampDuration;

        /// <summary>Torque strength for aligning the object up-axis to the surface normal. Zero disables.</summary>
        public float AlignmentStrength => alignmentStrength;

        /// <summary>Damping torque opposing angular velocity during surface alignment.</summary>
        public float AlignmentDamping => alignmentDamping;
    }
}
