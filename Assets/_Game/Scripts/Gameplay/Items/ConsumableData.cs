using UnityEngine;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Consumable", fileName = "Consumable")]
    public sealed class ConsumableData : ItemData
    {
        [Header("Hiệu quả sử dụng")]
        [Min(0f)] public float healthRestore;
        [Min(0f)] public float hungerRestore;
        [Min(0f)] public float thirstRestore;
        [Min(0f)] public float staminaRestore;
        [Range(-5f, 5f)] public float temperatureDeltaC;

        [Header("Trạng thái")]
        [Min(0f)] public float buffDurationSeconds;
        [Range(0.1f, 1f)] public float hungerDrainMultiplier = 1f;
        [Range(0.1f, 1f)] public float thirstDrainMultiplier = 1f;
        public bool clearsBleeding;
        public bool curesSickness;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrWhiteSpace(displayName)) displayName = name;
            healthRestore = Safe(healthRestore);
            hungerRestore = Safe(hungerRestore);
            thirstRestore = Safe(thirstRestore);
            staminaRestore = Safe(staminaRestore);
            buffDurationSeconds = Safe(buffDurationSeconds);
            if (float.IsNaN(temperatureDeltaC) || float.IsInfinity(temperatureDeltaC)) temperatureDeltaC = 0f;
            hungerDrainMultiplier = Mathf.Clamp(Safe(hungerDrainMultiplier, 1f), 0.1f, 1f);
            thirstDrainMultiplier = Mathf.Clamp(Safe(thirstDrainMultiplier, 1f), 0.1f, 1f);
        }

        private static float Safe(float value, float fallback = 0f)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0f, value);
    }
}
