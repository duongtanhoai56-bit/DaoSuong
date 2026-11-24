using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DayNightCycle : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Light sun;
        [SerializeField] private WeatherSystem weather;

        [Header("Clock")]
        [SerializeField, Min(30f)] private float dayLengthSeconds = 720f;
        [SerializeField, Range(0f, 1f)] private float startTime01 = 0.28f;
        [SerializeField] private float sunYaw = 30f;

        [Header("Lighting and fog")]
        [SerializeField] private Color dayLight = new Color(1f, 0.96f, 0.86f);
        [SerializeField] private Color horizonLight = new Color(1f, 0.55f, 0.25f);
        [SerializeField] private Color dayAmbient = new Color(0.55f, 0.65f, 0.80f);
        [SerializeField] private Color nightAmbient = new Color(0.09f, 0.11f, 0.20f);
        [SerializeField] private Color dayFog = new Color(0.72f, 0.82f, 0.92f);
        [SerializeField] private Color nightFog = new Color(0.03f, 0.05f, 0.10f);
        [SerializeField, Min(0f)] private float sunIntensity = 1.1f;
        [SerializeField, Min(0f)] private float baseFogDensity = 0.005f;

        public float Time01 { get; private set; }
        public float Hour => Time01 * 24f;
        public float Daylight { get; private set; }
        public int DayCount { get; private set; }
        public bool IsNight => Daylight < 0.15f;
        public float SpeedMultiplier { get; set; } = 1f;
        public event Action<bool> NightChanged;
        public event Action<int> DayChanged;

        private bool wasNight;
        private Material originalSkybox;
        private Material runtimeSkybox;

        private void Start()
        {
            Time01 = startTime01;
            if (sun != null)
                RenderSettings.sun = sun;

            // Clone the scene skybox so Play Mode lighting changes never mutate the source asset.
            originalSkybox = RenderSettings.skybox;
            if (originalSkybox != null)
            {
                runtimeSkybox = new Material(originalSkybox) { name = "Runtime_DayNight_Skybox" };
                RenderSettings.skybox = runtimeSkybox;
            }
            ApplyLighting();
            wasNight = IsNight;
        }

        private void Update()
        {
            float dayLength = Mathf.Max(30f, dayLengthSeconds);
            Time01 += Time.deltaTime * Mathf.Max(0f, SpeedMultiplier) / dayLength;
            while (Time01 >= 1f)
            {
                Time01 -= 1f;
                DayCount++;
                DayChanged?.Invoke(DayCount);
            }

            ApplyLighting();
            NotifyNightTransition();
        }

        /// <summary>Restores a saved completed-day counter without emitting a day transition.</summary>
        public void SetDayCount(int completedDays)
        {
            DayCount = Mathf.Max(0, completedDays);
        }

        /// <summary>Called by debug tools now and by the authoritative world clock in Phase 19.</summary>
        public void SetTime01(float normalizedTime)
        {
            Time01 = Mathf.Repeat(normalizedTime, 1f);
            ApplyLighting();
            NotifyNightTransition();
        }

        private void NotifyNightTransition()
        {
            bool night = IsNight;
            if (night == wasNight)
                return;

            wasNight = night;
            NightChanged?.Invoke(night);
        }

        private void ApplyLighting()
        {
            float elevation = Mathf.Sin(Time01 * Mathf.PI * 2f - Mathf.PI * 0.5f);
            Daylight = IslandNoise.Smooth(-0.1f, 0.3f, elevation);
            float horizon = 1f - Mathf.Clamp01(Mathf.Abs(elevation) / 0.35f);

            float lightMultiplier = weather != null ? weather.LightMultiplier : 1f;
            float cloudiness = weather != null ? weather.Cloudiness : 0f;
            float flash = weather != null ? weather.Flash : 0f;
            float extraFog = weather != null ? weather.FogDensity : 0f;

            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(Time01 * 360f - 90f, sunYaw, 0f);
                sun.color = Color.Lerp(dayLight, horizonLight, horizon * horizon);
                sun.intensity = sunIntensity * Daylight * lightMultiplier;
                sun.shadows = sun.intensity > 0.05f ? LightShadows.Hard : LightShadows.None;
            }

            // Ambient and fog must still update even if the directional light reference is missing.
            Color ambient = Color.Lerp(nightAmbient, dayAmbient, Daylight) * Mathf.Lerp(1f, 0.7f, cloudiness);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient + new Color(0.5f, 0.55f, 0.7f) * (flash * 0.6f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(nightFog, dayFog, Daylight) * Mathf.Lerp(1f, 0.65f, cloudiness);
            RenderSettings.fogDensity = baseFogDensity + extraFog;
            ApplySkybox(horizon, cloudiness, flash);
        }

        private void ApplySkybox(float horizon, float cloudiness, float flash)
        {
            if (runtimeSkybox == null)
                return;

            Color nightTint = new Color(0.10f, 0.14f, 0.25f);
            Color dayTint = new Color(0.48f, 0.62f, 0.82f);
            Color horizonTint = new Color(0.96f, 0.48f, 0.24f);
            Color tint = Color.Lerp(nightTint, dayTint, Daylight);
            tint = Color.Lerp(tint, horizonTint, horizon * 0.85f);
            tint *= Mathf.Lerp(1f, 0.72f, cloudiness);

            if (runtimeSkybox.HasProperty("_SkyTint"))
                runtimeSkybox.SetColor("_SkyTint", tint);
            if (runtimeSkybox.HasProperty("_GroundColor"))
                runtimeSkybox.SetColor("_GroundColor", Color.Lerp(new Color(0.035f, 0.045f, 0.075f), new Color(0.32f, 0.30f, 0.27f), Daylight));
            if (runtimeSkybox.HasProperty("_Exposure"))
                runtimeSkybox.SetFloat("_Exposure", Mathf.Lerp(0.32f, 1.15f, Daylight) * Mathf.Lerp(1f, 0.72f, cloudiness) + flash * 0.2f);
        }

        private void OnDestroy()
        {
            if (runtimeSkybox == null)
                return;

            if (RenderSettings.skybox == runtimeSkybox)
                RenderSettings.skybox = originalSkybox;

            if (Application.isPlaying)
                Destroy(runtimeSkybox);
            else
                DestroyImmediate(runtimeSkybox);
        }

        private void OnValidate()
        {
            dayLengthSeconds = Mathf.Max(30f, dayLengthSeconds);
            sunIntensity = Mathf.Max(0f, sunIntensity);
            baseFogDensity = Mathf.Max(0f, baseFogDensity);
        }
    }
}
