using UnityEngine;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Survival Settings", fileName = "SurvivalSettings")]
    public sealed class SurvivalSettings : ScriptableObject
    {
        [Header("Drain / hồi phục, theo giây")]
        [Min(0f)] public float hungerDrainPerSecond = 0.0125f;
        [Min(0f)] public float thirstDrainPerSecond = 0.020f;
        [Min(1f)] public float sprintHungerMultiplier = 1.25f;
        [Min(1f)] public float sprintThirstMultiplier = 1.5f;
        [Min(0f)] public float staminaRegenPerSecond = 17f;
        [Min(0f)] public float sprintStaminaPerSecond = 13f;

        [Header("Thiệt hại")]
        [Min(0f)] public float starvationDamagePerSecond = 0.18f;
        [Min(0f)] public float dehydrationDamagePerSecond = 0.32f;
        [Min(0f)] public float bleedingDamagePerSecond = 0.35f;
        [Min(0f)] public float coldDamagePerSecond = 0.15f;
        [Min(0f)] public float heatDamagePerSecond = 0.18f;

        [Header("Nhiệt độ cơ thể")]
        [Min(0f)] public float temperatureResponsePerSecond = 0.0008f;
        public float coldThresholdC = 35.5f;
        public float heatThresholdC = 38.5f;

        public SurvivalTuning ToTuning()
        {
            return new SurvivalTuning(hungerDrainPerSecond, thirstDrainPerSecond,
                sprintHungerMultiplier, sprintThirstMultiplier,
                starvationDamagePerSecond, dehydrationDamagePerSecond,
                staminaRegenPerSecond, bleedingDamagePerSecond,
                coldDamagePerSecond, heatDamagePerSecond,
                temperatureResponsePerSecond, coldThresholdC, heatThresholdC);
        }

        private void OnValidate()
        {
            hungerDrainPerSecond = Safe(hungerDrainPerSecond);
            thirstDrainPerSecond = Safe(thirstDrainPerSecond);
            sprintHungerMultiplier = Mathf.Max(1f, Safe(sprintHungerMultiplier, 1f));
            sprintThirstMultiplier = Mathf.Max(1f, Safe(sprintThirstMultiplier, 1f));
            staminaRegenPerSecond = Safe(staminaRegenPerSecond);
            sprintStaminaPerSecond = Safe(sprintStaminaPerSecond);
            starvationDamagePerSecond = Safe(starvationDamagePerSecond);
            dehydrationDamagePerSecond = Safe(dehydrationDamagePerSecond);
            bleedingDamagePerSecond = Safe(bleedingDamagePerSecond);
            coldDamagePerSecond = Safe(coldDamagePerSecond);
            heatDamagePerSecond = Safe(heatDamagePerSecond);
            temperatureResponsePerSecond = Safe(temperatureResponsePerSecond);
            coldThresholdC = float.IsNaN(coldThresholdC) || float.IsInfinity(coldThresholdC) ? 35.5f : Mathf.Clamp(coldThresholdC, 30f, 37f);
            heatThresholdC = float.IsNaN(heatThresholdC) || float.IsInfinity(heatThresholdC) ? 38.5f : Mathf.Clamp(heatThresholdC, 37f, 43f);
            heatThresholdC = Mathf.Max(coldThresholdC + 0.5f, heatThresholdC);
        }

        private static float Safe(float value, float fallback = 0f)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0f, value);
    }
}
