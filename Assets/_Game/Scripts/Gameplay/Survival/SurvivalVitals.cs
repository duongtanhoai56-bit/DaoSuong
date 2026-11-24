using System;

namespace Game.Gameplay
{
    [Flags]
    public enum SurvivalCondition
    {
        None = 0,
        Bleeding = 1 << 0,
        Sick = 1 << 1,
        Cold = 1 << 2,
        HeatStress = 1 << 3,
        WellFed = 1 << 4,
        Hydrated = 1 << 5,
        Exhausted = 1 << 6,
        Dead = 1 << 7
    }

    /// <summary>Immutable snapshot. Core values use 0..100; temperature is degrees Celsius.</summary>
    public readonly struct SurvivalVitals
    {
        public float Health { get; }
        public float Hunger { get; }
        public float Thirst { get; }
        public float Stamina { get; }
        public float BodyTemperatureC { get; }
        public SurvivalCondition Conditions { get; }
        public bool IsDead => Health <= 0f;

        public SurvivalVitals(float health, float hunger, float thirst, float stamina,
            float bodyTemperatureC, SurvivalCondition conditions)
        {
            Health = ClampFinite(health, 0f, 100f);
            Hunger = ClampFinite(hunger, 0f, 100f);
            Thirst = ClampFinite(thirst, 0f, 100f);
            Stamina = ClampFinite(stamina, 0f, 100f);
            BodyTemperatureC = ClampFinite(bodyTemperatureC, 30f, 43f, 37f);
            Conditions = conditions;
        }

        private static float ClampFinite(float value, float min, float max, float fallback = 0f)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    /// <summary>Pure tuning data shared by runtime simulation and EditMode tests.</summary>
    public readonly struct SurvivalTuning
    {
        public readonly float HungerDrainPerSecond;
        public readonly float ThirstDrainPerSecond;
        public readonly float SprintHungerMultiplier;
        public readonly float SprintThirstMultiplier;
        public readonly float StarvationDamagePerSecond;
        public readonly float DehydrationDamagePerSecond;
        public readonly float StaminaRegenPerSecond;
        public readonly float BleedingDamagePerSecond;
        public readonly float ColdDamagePerSecond;
        public readonly float HeatDamagePerSecond;
        public readonly float TemperatureResponsePerSecond;
        public readonly float ColdThresholdC;
        public readonly float HeatThresholdC;

        public SurvivalTuning(float hungerDrainPerSecond, float thirstDrainPerSecond,
            float sprintHungerMultiplier, float sprintThirstMultiplier,
            float starvationDamagePerSecond, float dehydrationDamagePerSecond,
            float staminaRegenPerSecond, float bleedingDamagePerSecond,
            float coldDamagePerSecond, float heatDamagePerSecond,
            float temperatureResponsePerSecond, float coldThresholdC, float heatThresholdC)
        {
            HungerDrainPerSecond = SafeNonNegative(hungerDrainPerSecond);
            ThirstDrainPerSecond = SafeNonNegative(thirstDrainPerSecond);
            SprintHungerMultiplier = SafeAtLeastOne(sprintHungerMultiplier);
            SprintThirstMultiplier = SafeAtLeastOne(sprintThirstMultiplier);
            StarvationDamagePerSecond = SafeNonNegative(starvationDamagePerSecond);
            DehydrationDamagePerSecond = SafeNonNegative(dehydrationDamagePerSecond);
            StaminaRegenPerSecond = SafeNonNegative(staminaRegenPerSecond);
            BleedingDamagePerSecond = SafeNonNegative(bleedingDamagePerSecond);
            ColdDamagePerSecond = SafeNonNegative(coldDamagePerSecond);
            HeatDamagePerSecond = SafeNonNegative(heatDamagePerSecond);
            TemperatureResponsePerSecond = SafeNonNegative(temperatureResponsePerSecond);
            ColdThresholdC = IsFinite(coldThresholdC) ? Clamp(coldThresholdC, 30f, 37f) : 35.5f;
            float safeHeat = IsFinite(heatThresholdC) ? Clamp(heatThresholdC, 37f, 43f) : 38.5f;
            HeatThresholdC = Math.Max(ColdThresholdC + 0.5f, safeHeat);
        }

        public static SurvivalTuning Default => new SurvivalTuning(
            0.0125f, 0.020f, 1.25f, 1.5f,
            0.18f, 0.32f, 17f, 0.35f,
            0.15f, 0.18f, 0.0008f, 35.5f, 38.5f);

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
        private static float SafeNonNegative(float value) => IsFinite(value) ? Math.Max(0f, value) : 0f;
        private static float SafeAtLeastOne(float value) => IsFinite(value) ? Math.Max(1f, value) : 1f;
    }

    /// <summary>Deterministic survival simulation. No MonoBehaviour, Time, physics or UI dependency.</summary>
    public sealed class SurvivalModel
    {
        private readonly SurvivalTuning tuning;
        private float health = 100f;
        private float hunger = 100f;
        private float thirst = 100f;
        private float stamina = 100f;
        private float bodyTemperatureC = 37f;
        private float bleedingSeconds;
        private float bleedingDamagePerSecond;
        private float sicknessSeconds;
        private float sicknessThirstMultiplier = 1.5f;
        private float wellFedSeconds;
        private float hydratedSeconds;
        private float hungerBuffMultiplier = 1f;
        private float thirstBuffMultiplier = 1f;

        public SurvivalModel(SurvivalTuning tuning)
        {
            this.tuning = tuning;
        }

        public SurvivalVitals Vitals => new SurvivalVitals(health, hunger, thirst, stamina,
            bodyTemperatureC, CalculateConditions());
        public bool IsDead => health <= 0f;
        public float Health => health;
        public float Hunger => hunger;
        public float Thirst => thirst;
        public float Stamina => stamina;
        public float BodyTemperatureC => bodyTemperatureC;

        public void Tick(float deltaTime, float ambientTemperatureC, float exposure01, bool isSprinting)
        {
            if (IsDead || !IsFinite(deltaTime) || deltaTime <= 0f) return;
            float dt = Math.Min(deltaTime, 2f);
            float ambient = IsFinite(ambientTemperatureC) ? Clamp(ambientTemperatureC, -10f, 55f) : 22f;
            float exposure = IsFinite(exposure01) ? Clamp(exposure01, 0f, 2f) : 1f;

            float sprintHunger = isSprinting ? tuning.SprintHungerMultiplier : 1f;
            float sprintThirst = isSprinting ? tuning.SprintThirstMultiplier : 1f;
            float starvationSeconds = DrainWithModifiers(ref hunger, dt,
                tuning.HungerDrainPerSecond * sprintHunger,
                wellFedSeconds, hungerBuffMultiplier, 0f, 1f);
            float dehydrationSeconds = DrainWithModifiers(ref thirst, dt,
                tuning.ThirstDrainPerSecond * sprintThirst,
                hydratedSeconds, thirstBuffMultiplier, sicknessSeconds, sicknessThirstMultiplier);

            // Core temperature is buffered around 37 C; ambient air is not the same as body temperature.
            // Comfortable tropical air should not slowly cause hypothermia. Severe cold/heat shifts the target.
            float thermalOffset = Clamp((ambient - 20f) * 0.10f, -3.5f, 3.5f);
            float targetBodyTemperature = 37f + thermalOffset;
            float temperatureRate = tuning.TemperatureResponsePerSecond * exposure;
            bodyTemperatureC = Clamp(bodyTemperatureC + (targetBodyTemperature - bodyTemperatureC) * temperatureRate * dt, 30f, 43f);

            float totalDamage = tuning.StarvationDamagePerSecond * starvationSeconds +
                                tuning.DehydrationDamagePerSecond * dehydrationSeconds;
            float bleedingTimeThisTick = Math.Min(dt, bleedingSeconds);
            totalDamage += bleedingDamagePerSecond * bleedingTimeThisTick;
            if (bodyTemperatureC < tuning.ColdThresholdC) totalDamage += tuning.ColdDamagePerSecond * dt;
            if (bodyTemperatureC > tuning.HeatThresholdC) totalDamage += tuning.HeatDamagePerSecond * dt;
            health = Clamp(health - totalDamage, 0f, 100f);

            if (!isSprinting)
            {
                float regen = tuning.StaminaRegenPerSecond;
                if (bodyTemperatureC < tuning.ColdThresholdC || bodyTemperatureC > tuning.HeatThresholdC)
                    regen *= 0.65f;
                stamina = Clamp(stamina + regen * dt, 0f, 100f);
            }

            bleedingSeconds = Math.Max(0f, bleedingSeconds - dt);
            sicknessSeconds = Math.Max(0f, sicknessSeconds - dt);
            wellFedSeconds = Math.Max(0f, wellFedSeconds - dt);
            hydratedSeconds = Math.Max(0f, hydratedSeconds - dt);
            if (sicknessSeconds <= 0f) sicknessThirstMultiplier = 1.5f;
            if (bleedingSeconds <= 0f) bleedingDamagePerSecond = 0f;
            if (wellFedSeconds <= 0f) hungerBuffMultiplier = 1f;
            if (hydratedSeconds <= 0f) thirstBuffMultiplier = 1f;
        }

        public bool TrySpendStamina(float amount)
        {
            if (IsDead || !IsFinite(amount) || amount <= 0f || stamina + 0.0001f < amount)
                return false;
            stamina = Clamp(stamina - amount, 0f, 100f);
            return true;
        }

        public void ApplyDamage(float amount)
        {
            if (IsDead || !IsFinite(amount) || amount <= 0f) return;
            health = Clamp(health - amount, 0f, 100f);
        }

        public bool ApplyBleeding(float durationSeconds, float damagePerSecond)
        {
            if (IsDead || !IsFinite(durationSeconds) || !IsFinite(damagePerSecond) || durationSeconds <= 0f || damagePerSecond <= 0f)
                return false;
            bleedingSeconds = Math.Max(bleedingSeconds, Clamp(durationSeconds, 0f, 3600f));
            bleedingDamagePerSecond = Math.Max(bleedingDamagePerSecond, Clamp(damagePerSecond, 0f, 20f));
            return true;
        }

        public bool ApplySickness(float durationSeconds, float thirstMultiplier)
        {
            if (IsDead || !IsFinite(durationSeconds) || !IsFinite(thirstMultiplier) || durationSeconds <= 0f)
                return false;
            sicknessSeconds = Math.Max(sicknessSeconds, Clamp(durationSeconds, 0f, 7200f));
            sicknessThirstMultiplier = Math.Max(sicknessThirstMultiplier, Clamp(thirstMultiplier, 1f, 5f));
            return true;
        }

        public bool CanApplyConsumable(float healthRestore, float hungerRestore, float thirstRestore,
            float staminaRestore, float temperatureDeltaC, float buffDurationSeconds,
            float hungerDrainMultiplier, float thirstDrainMultiplier, bool clearsBleeding, bool curesSickness)
        {
            if (IsDead || !AreFinite(healthRestore, hungerRestore, thirstRestore, staminaRestore,
                    temperatureDeltaC, buffDurationSeconds, hungerDrainMultiplier, thirstDrainMultiplier))
                return false;
            // Keep the pure model safe even when callers bypass ScriptableObject OnValidate.
            if (healthRestore < 0f || hungerRestore < 0f || thirstRestore < 0f || staminaRestore < 0f ||
                buffDurationSeconds < 0f || temperatureDeltaC < -5f || temperatureDeltaC > 5f ||
                hungerDrainMultiplier < 0.1f || hungerDrainMultiplier > 1f ||
                thirstDrainMultiplier < 0.1f || thirstDrainMultiplier > 1f)
                return false;

            bool improvesTemperature = Math.Abs(bodyTemperatureC + temperatureDeltaC - 37f) + 0.001f <
                                       Math.Abs(bodyTemperatureC - 37f);
            bool improvesHungerBuff = IsBuffUpgrade(wellFedSeconds, hungerBuffMultiplier,
                buffDurationSeconds, hungerDrainMultiplier);
            bool improvesThirstBuff = IsBuffUpgrade(hydratedSeconds, thirstBuffMultiplier,
                buffDurationSeconds, thirstDrainMultiplier);
            return (healthRestore > 0f && health < 100f) || (hungerRestore > 0f && hunger < 100f) ||
                   (thirstRestore > 0f && thirst < 100f) || (staminaRestore > 0f && stamina < 100f) ||
                   improvesTemperature || (clearsBleeding && bleedingSeconds > 0f) ||
                   (curesSickness && sicknessSeconds > 0f) || improvesHungerBuff || improvesThirstBuff;
        }

        public bool ApplyConsumable(float healthRestore, float hungerRestore, float thirstRestore,
            float staminaRestore, float temperatureDeltaC, float buffDurationSeconds,
            float hungerDrainMultiplier, float thirstDrainMultiplier, bool clearsBleeding, bool curesSickness)
        {
            if (!CanApplyConsumable(healthRestore, hungerRestore, thirstRestore, staminaRestore,
                    temperatureDeltaC, buffDurationSeconds, hungerDrainMultiplier, thirstDrainMultiplier,
                    clearsBleeding, curesSickness))
                return false;

            health = Clamp(health + Math.Max(0f, healthRestore), 0f, 100f);
            hunger = Clamp(hunger + Math.Max(0f, hungerRestore), 0f, 100f);
            thirst = Clamp(thirst + Math.Max(0f, thirstRestore), 0f, 100f);
            stamina = Clamp(stamina + Math.Max(0f, staminaRestore), 0f, 100f);
            bodyTemperatureC = Clamp(bodyTemperatureC + temperatureDeltaC, 30f, 43f);
            if (clearsBleeding) { bleedingSeconds = 0f; bleedingDamagePerSecond = 0f; }
            if (curesSickness) { sicknessSeconds = 0f; sicknessThirstMultiplier = 1.5f; }
            if (buffDurationSeconds > 0f)
            {
                float duration = Clamp(buffDurationSeconds, 0f, 7200f);
                ApplyTimedBuff(ref wellFedSeconds, ref hungerBuffMultiplier, duration, hungerDrainMultiplier);
                ApplyTimedBuff(ref hydratedSeconds, ref thirstBuffMultiplier, duration, thirstDrainMultiplier);
            }
            return true;
        }

        public void RestoreAll()
        {
            health = hunger = thirst = stamina = 100f;
            bodyTemperatureC = 37f;
            bleedingSeconds = sicknessSeconds = wellFedSeconds = hydratedSeconds = 0f;
            bleedingDamagePerSecond = 0f;
            sicknessThirstMultiplier = 1.5f;
            hungerBuffMultiplier = thirstBuffMultiplier = 1f;
        }

        private static bool IsBuffUpgrade(float remainingSeconds, float currentMultiplier,
            float newDuration, float newMultiplier)
        {
            if (newDuration <= 0f || newMultiplier >= 0.999f) return false;
            if (remainingSeconds <= 0f || currentMultiplier >= 0.999f) return true;
            if (newMultiplier < currentMultiplier - 0.0001f) return true;
            return Math.Abs(newMultiplier - currentMultiplier) <= 0.0001f && newDuration > remainingSeconds;
        }

        private static void ApplyTimedBuff(ref float remainingSeconds, ref float currentMultiplier,
            float newDuration, float newMultiplier)
        {
            if (newDuration <= 0f || newMultiplier >= 0.999f) return;
            if (remainingSeconds <= 0f || currentMultiplier >= 0.999f ||
                newMultiplier < currentMultiplier - 0.0001f)
            {
                // A stronger effect replaces a weaker one and starts its own duration.
                currentMultiplier = newMultiplier;
                remainingSeconds = newDuration;
            }
            else if (Math.Abs(newMultiplier - currentMultiplier) <= 0.0001f)
            {
                // Only an equal-strength effect refreshes the remaining duration.
                remainingSeconds = Math.Max(remainingSeconds, newDuration);
            }
            // A weaker effect never extends an already active stronger effect.
        }

        /// <summary>Drains a stat over two expiring modifiers and returns seconds spent at zero.</summary>
        private static float DrainWithModifiers(ref float value, float deltaTime, float baseRate,
            float firstDuration, float firstMultiplier, float secondDuration, float secondMultiplier)
        {
            if (baseRate <= 0f || value <= 0f) return value <= 0f ? deltaTime : 0f;
            float elapsed = 0f;
            while (elapsed < deltaTime)
            {
                float next = deltaTime;
                if (firstDuration > elapsed && firstDuration < next) next = firstDuration;
                if (secondDuration > elapsed && secondDuration < next) next = secondDuration;
                float multiplier = (elapsed < firstDuration ? firstMultiplier : 1f) *
                                   (elapsed < secondDuration ? secondMultiplier : 1f);
                float segment = next - elapsed;
                float rate = baseRate * multiplier;
                float drain = rate * segment;
                if (value <= drain)
                {
                    float timeToZero = rate > 0f ? value / rate : segment;
                    value = 0f;
                    return deltaTime - (elapsed + timeToZero);
                }
                value -= drain;
                elapsed = next;
            }
            return 0f;
        }

        private SurvivalCondition CalculateConditions()
        {
            SurvivalCondition result = SurvivalCondition.None;
            if (bleedingSeconds > 0f) result |= SurvivalCondition.Bleeding;
            if (sicknessSeconds > 0f) result |= SurvivalCondition.Sick;
            if (bodyTemperatureC < tuning.ColdThresholdC) result |= SurvivalCondition.Cold;
            if (bodyTemperatureC > tuning.HeatThresholdC) result |= SurvivalCondition.HeatStress;
            if (wellFedSeconds > 0f) result |= SurvivalCondition.WellFed;
            if (hydratedSeconds > 0f) result |= SurvivalCondition.Hydrated;
            if (stamina < 20f) result |= SurvivalCondition.Exhausted;
            if (IsDead) result |= SurvivalCondition.Dead;
            return result;
        }

        private static bool AreFinite(params float[] values)
        {
            for (int i = 0; i < values.Length; i++) if (!IsFinite(values[i])) return false;
            return true;
        }
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    }
}
