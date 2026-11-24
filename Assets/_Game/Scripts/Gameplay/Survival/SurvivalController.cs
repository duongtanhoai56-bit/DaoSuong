using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SurvivalController : MonoBehaviour
    {
        [Header("Data and world")]
        [SerializeField] private SurvivalSettings settings;
        [SerializeField] private IslandGenerator island;
        [SerializeField] private DayNightCycle dayNight;
        [SerializeField] private WeatherSystem weather;
        [SerializeField] private PlayerInventory inventory;

        [Header("Environment")]
        [SerializeField, Min(-10f)] private float baseNightTemperatureC = 16f;
        [SerializeField, Min(0f)] private float daylightTemperatureBonusC = 14f;
        [SerializeField, Min(0f)] private float rainCoolingC = 2f;
        [SerializeField, Min(0f)] private float stormCoolingC = 4f;
        [SerializeField, Min(0f)] private float mountainCoolingPerMeter = 0.45f;

        private SurvivalModel model;
        private bool sprinting;
        private bool deathRaised;
        private float nextPublish;
        private SurvivalVitals lastPublished;

        public SurvivalVitals Vitals => Model.Vitals;
        public SurvivalModel Model
        {
            get
            {
                if (model == null) model = new SurvivalModel(settings != null ? settings.ToTuning() : SurvivalTuning.Default);
                return model;
            }
        }
        public bool IsDead => Model.IsDead;
        public event Action<SurvivalVitals> Changed;
        public event Action Died;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (island == null) island = FindFirstObjectByType<IslandGenerator>();
            if (dayNight == null) dayNight = FindFirstObjectByType<DayNightCycle>();
            if (weather == null) weather = FindFirstObjectByType<WeatherSystem>();
            model = new SurvivalModel(settings != null ? settings.ToTuning() : SurvivalTuning.Default);
            lastPublished = model.Vitals;
        }

        private void OnDisable()
        {
            sprinting = false;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f || IsDead) return;

            float ambient;
            float exposure;
            CalculateEnvironment(out ambient, out exposure);
            model.Tick(deltaTime, ambient, exposure, sprinting);
            RaiseDeathIfNeeded();

            if (Time.unscaledTime >= nextPublish)
            {
                nextPublish = Time.unscaledTime + 0.1f;
                SurvivalVitals current = model.Vitals;
                if (HasMeaningfulDifference(lastPublished, current))
                {
                    lastPublished = current;
                    Changed?.Invoke(current);
                }
            }
        }

        public bool TrySpendSprintStamina(float deltaTime)
        {
            if (IsDead || !IsFinite(deltaTime) || deltaTime <= 0f) return false;
            float drainPerSecond = settings != null ? settings.sprintStaminaPerSecond : 13f;
            if (!IsFinite(drainPerSecond) || drainPerSecond < 0f) return false;
            if (drainPerSecond <= 0f) return true; // Zero is a valid tuning choice: sprint without stamina cost.
            return Model.TrySpendStamina(drainPerSecond * Mathf.Min(deltaTime, 2f));
        }

        public void SetSprinting(bool value) => sprinting = value && !IsDead;

        public void ApplyDamage(float amount)
        {
            Model.ApplyDamage(amount);
            RaiseDeathIfNeeded();
            PublishImmediately();
        }

        public void ApplyBleeding(float durationSeconds, float damagePerSecond = -1f)
        {
            float dps = damagePerSecond >= 0f ? damagePerSecond : (settings != null ? settings.bleedingDamagePerSecond : 0.35f);
            if (Model.ApplyBleeding(durationSeconds, dps)) PublishImmediately();
        }

        public void ApplySickness(float durationSeconds, float thirstMultiplier = 1.5f)
        {
            if (Model.ApplySickness(durationSeconds, thirstMultiplier)) PublishImmediately();
        }

        public void RestoreForDebug()
        {
            Model.RestoreAll();
            deathRaised = false;
            sprinting = false;
            InputService.Current?.ClearPendingInput();
            PublishImmediately();
        }

        public bool TryConsume(PlayerInventory sourceInventory, int slotIndex)
        {
            if (sourceInventory == null || IsDead || slotIndex < 0 || slotIndex >= sourceInventory.SlotCount)
                return false;
            ItemStack stack = sourceInventory.GetSlot(slotIndex);
            ConsumableData item = stack != null ? stack.item as ConsumableData : null;
            if (item == null || stack.count <= 0) return false;

            bool canApply = Model.CanApplyConsumable(item.healthRestore, item.hungerRestore,
                item.thirstRestore, item.staminaRestore, item.temperatureDeltaC,
                item.buffDurationSeconds, item.hungerDrainMultiplier, item.thirstDrainMultiplier,
                item.clearsBleeding, item.curesSickness);
            if (!canApply) return false;

            // Remove first, then apply. All checks are synchronous and the slot cannot change
            // between these statements on Unity's main thread.
            if (!sourceInventory.TryRemoveAt(slotIndex, 1)) return false;
            bool applied = Model.ApplyConsumable(item.healthRestore, item.hungerRestore,
                item.thirstRestore, item.staminaRestore, item.temperatureDeltaC,
                item.buffDurationSeconds, item.hungerDrainMultiplier, item.thirstDrainMultiplier,
                item.clearsBleeding, item.curesSickness);
            if (!applied)
            {
                // Defensive recovery. This should never be reached after CanApplyConsumable.
                sourceInventory.Add(item, 1);
                return false;
            }

            HudMessages.Toast("Đã sử dụng " + item.displayName);
            PublishImmediately();
            return true;
        }

        private void CalculateEnvironment(out float ambientTemperatureC, out float exposure)
        {
            CaveManager cave = CaveManager.Instance;
            bool underground = cave != null && cave.IsInsideCave;
            if (underground)
            {
                // Stone ceilings shelter the player from rain, storms and surface day/night swings.
                ambientTemperatureC = cave.ActiveAmbientTemperatureC;
                exposure = cave.ActiveExposure;
            }
            else
            {
                float daylight = dayNight != null ? dayNight.Daylight : 0.6f;
                ambientTemperatureC = baseNightTemperatureC + daylightTemperatureBonusC * Mathf.Clamp01(daylight);
                exposure = 1f;

                IslandGenerator currentIsland = ArchipelagoManager.Instance != null
                    ? ArchipelagoManager.Instance.GetIslandAt(transform.position) : island;
                if (currentIsland != null && currentIsland.IsGenerated && currentIsland.ContainsWorldPosition(transform.position))
                {
                    Biome biome = currentIsland.GetBiome(transform.position.x, transform.position.z);
                    float height = currentIsland.GetHeight(transform.position.x, transform.position.z);
                    if (biome == Biome.Mountain || biome == Biome.Snow)
                        ambientTemperatureC -= Mathf.Max(0f, height - 12f) * mountainCoolingPerMeter;
                    else if (biome == Biome.Swamp)
                    {
                        ambientTemperatureC -= 1.5f;
                        exposure *= 1.1f;
                    }
                }

                if (weather != null)
                {
                    ambientTemperatureC -= weather.RainIntensity * rainCoolingC;
                    if (weather.Current == WeatherType.Storm)
                    {
                        ambientTemperatureC -= stormCoolingC;
                        exposure *= 1.35f;
                    }
                    else if (weather.Current == WeatherType.Rain)
                        exposure *= 1.2f;
                    else if (weather.Current == WeatherType.Fog)
                        exposure *= 1.05f;
                }
            }

            // Nearby campfires remain effective in caves and at the base.
            ambientTemperatureC += WarmthSource.GetWarmthAt(transform.position);
        }

        private void RaiseDeathIfNeeded()
        {
            if (!Model.IsDead || deathRaised) return;
            deathRaised = true;
            sprinting = false;
            InputService.Current?.ClearPendingInput();
            HudMessages.Toast("Bạn đã gục ngã! Cần đồng đội cứu hoặc hồi sinh tại căn cứ.");
            Died?.Invoke();
        }

        private void PublishImmediately()
        {
            lastPublished = Model.Vitals;
            Changed?.Invoke(lastPublished);
        }

        private static bool HasMeaningfulDifference(SurvivalVitals a, SurvivalVitals b)
        {
            return Mathf.Abs(a.Health - b.Health) > 0.05f ||
                   Mathf.Abs(a.Hunger - b.Hunger) > 0.05f ||
                   Mathf.Abs(a.Thirst - b.Thirst) > 0.05f ||
                   Mathf.Abs(a.Stamina - b.Stamina) > 0.1f ||
                   Mathf.Abs(a.BodyTemperatureC - b.BodyTemperatureC) > 0.02f ||
                   a.Conditions != b.Conditions;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private void OnValidate()
        {
            baseNightTemperatureC = ClampFinite(baseNightTemperatureC, 16f, -10f, 35f);
            daylightTemperatureBonusC = ClampFinite(daylightTemperatureBonusC, 14f, 0f, 25f);
            rainCoolingC = ClampFinite(rainCoolingC, 2f, 0f, 15f);
            stormCoolingC = ClampFinite(stormCoolingC, 4f, 0f, 20f);
            mountainCoolingPerMeter = ClampFinite(mountainCoolingPerMeter, 0.45f, 0f, 1f);
        }

        private static float ClampFinite(float value, float fallback, float min, float max)
        {
            if (!IsFinite(value)) value = fallback;
            return Mathf.Clamp(value, min, max);
        }
    }
}
