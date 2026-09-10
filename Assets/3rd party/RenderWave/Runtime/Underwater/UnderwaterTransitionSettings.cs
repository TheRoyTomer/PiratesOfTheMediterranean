using System;
using UnityEngine;

namespace RenderWave.Runtime.Underwater
{
    /// <summary>
    /// Stability settings for crossing the waterline.
    /// </summary>
    [Serializable]
    public sealed class UnderwaterTransitionSettings
    {
        [SerializeField, Min(0f)] private float enterThreshold = 0.15f;
        [SerializeField, Min(0f)] private float exitThreshold = 0.15f;
        [SerializeField, Min(0.01f)] private float fadeInSpeed = 4f;
        [SerializeField, Min(0.01f)] private float fadeOutSpeed = 5f;

        public float EnterThreshold => enterThreshold;
        public float ExitThreshold => exitThreshold;
        public float FadeInSpeed => fadeInSpeed;
        public float FadeOutSpeed => fadeOutSpeed;

        public void Validate()
        {
            enterThreshold = Mathf.Max(0f, enterThreshold);
            exitThreshold = Mathf.Max(0f, exitThreshold);
            fadeInSpeed = Mathf.Max(0.01f, fadeInSpeed);
            fadeOutSpeed = Mathf.Max(0.01f, fadeOutSpeed);
        }
    }
}
