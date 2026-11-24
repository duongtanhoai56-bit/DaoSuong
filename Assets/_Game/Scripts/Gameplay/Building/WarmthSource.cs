using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Campfire comfort bonus for survival temperature while the player is nearby.</summary>
    [DisallowMultipleComponent]
    public sealed class WarmthSource : MonoBehaviour
    {
        private static readonly HashSet<WarmthSource> ActiveSources = new HashSet<WarmthSource>();
        [SerializeField, Min(0f)] private float radius = 8f;
        [SerializeField, Min(0f)] private float warmthCelsius = 8f;

        public void Configure(float warmthRadius, float warmth)
        {
            radius = Mathf.Clamp(warmthRadius, 0f, 40f);
            warmthCelsius = Mathf.Clamp(warmth, 0f, 40f);
        }

        private void OnEnable() => ActiveSources.Add(this);
        private void OnDisable() => ActiveSources.Remove(this);

        public static float GetWarmthAt(Vector3 position)
        {
            float warmth = 0f;
            foreach (WarmthSource source in ActiveSources)
            {
                if (source == null || !source.isActiveAndEnabled) continue;
                float distance = Vector3.Distance(position, source.transform.position);
                if (distance > source.radius) continue;
                float factor = source.radius > 0.01f ? 1f - Mathf.Clamp01(distance / source.radius) * 0.45f : 1f;
                warmth = Mathf.Max(warmth, source.warmthCelsius * factor);
            }
            return warmth;
        }
    }
}
