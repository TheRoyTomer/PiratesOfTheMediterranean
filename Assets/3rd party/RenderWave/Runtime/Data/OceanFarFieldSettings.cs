using System;
using UnityEngine;

namespace RenderWave.Runtime.Data
{
    /// <summary>
    /// Controls the cheap horizon-coverage mesh used only for infinite ocean coverage.
    /// </summary>
    [Serializable]
    public sealed class OceanFarFieldSettings
    {
        [SerializeField] private bool enabled = true;
        [SerializeField, Min(256f)] private float farDistance = 4096f;
        [SerializeField, Range(-2f, 0f)] private float verticalOffset = -0.08f;

        public bool Enabled => enabled;
        public float FarDistance => farDistance;
        public float VerticalOffset => verticalOffset;

        public void Validate()
        {
            farDistance = Mathf.Max(256f, farDistance);
            verticalOffset = Mathf.Clamp(verticalOffset, -2f, 0f);
        }
    }
}
