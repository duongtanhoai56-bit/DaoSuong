using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay
{
    public enum WeatherType
    {
        Clear = 0,
        Fog = 1,
        Rain = 2,
        Storm = 3
    }

    [DisallowMultipleComponent]
    public sealed class WeatherSystem : MonoBehaviour
    {
        private struct WeatherSnapshot
        {
            public float rain;
            public float fog;
            public float clouds;
            public float light;
            public float wind;

            public static WeatherSnapshot Lerp(WeatherSnapshot a, WeatherSnapshot b, float t)
            {
                return new WeatherSnapshot
                {
                    rain = Mathf.Lerp(a.rain, b.rain, t),
                    fog = Mathf.Lerp(a.fog, b.fog, t),
                    clouds = Mathf.Lerp(a.clouds, b.clouds, t),
                    light = Mathf.Lerp(a.light, b.light, t),
                    wind = Mathf.Lerp(a.wind, b.wind, t)
                };
            }
        }

        [Header("Weather")]
        [SerializeField] private bool autoChange = true;
        [SerializeField] private Vector2 changeIntervalSeconds = new Vector2(150f, 360f);
        [SerializeField, Min(1f)] private float blendSeconds = 25f;
        [SerializeField] private int seed = 1;

        [Header("Rain")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Material rainMaterial;
        [SerializeField, Range(100, 3000)] private int maxRainParticles = 1200;

        [Header("Swamp fog")]
        [SerializeField] private IslandGenerator island;
        [SerializeField] private Transform player;
        [SerializeField, Min(0f)] private float swampFogBonus = 0.012f;

        private WeatherSnapshot from;
        private WeatherSnapshot to;
        private WeatherSnapshot current;
        private float blendT = 1f;
        private float nextChange;
        private float lightningTimer = 6f;
        private float swampFog;
        private DeterministicRandom random;
        private ParticleSystem rain;
        private Transform rainTransform;

        public WeatherType Current { get; private set; }
        public float RainIntensity => current.rain;
        public float FogDensity => current.fog + swampFog;
        public float Cloudiness => current.clouds;
        public float LightMultiplier => current.light;
        public float WindStrength => current.wind;
        public float Flash { get; private set; }
        public event Action<WeatherType> Changed;
        public event Action Lightning;

        private static WeatherSnapshot PresetOf(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Fog:
                    return new WeatherSnapshot { rain = 0f, fog = 0.020f, clouds = 0.5f, light = 0.7f, wind = 1f };
                case WeatherType.Rain:
                    return new WeatherSnapshot { rain = 0.6f, fog = 0.004f, clouds = 0.7f, light = 0.55f, wind = 3f };
                case WeatherType.Storm:
                    return new WeatherSnapshot { rain = 1f, fog = 0.008f, clouds = 1f, light = 0.3f, wind = 8f };
                default:
                    return new WeatherSnapshot { rain = 0f, fog = 0f, clouds = 0.1f, light = 1f, wind = 1f };
            }
        }

        private void Awake()
        {
            random = new DeterministicRandom(seed);
            Current = WeatherType.Clear;
            from = to = current = PresetOf(Current);
            nextChange = RandomInterval();
            BuildRain();
        }

        private void Start()
        {
            if (followTarget == null && Camera.main != null)
                followTarget = Camera.main.transform;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (blendT < 1f)
            {
                blendT = Mathf.Min(1f, blendT + deltaTime / Mathf.Max(1f, blendSeconds));
                current = WeatherSnapshot.Lerp(from, to, IslandNoise.Smooth(0f, 1f, blendT));
            }

            if (autoChange)
            {
                nextChange -= deltaTime;
                if (nextChange <= 0f)
                {
                    SetWeather(PickNext());
                    nextChange = RandomInterval();
                }
            }

            UpdateSwampFog(deltaTime);
            UpdateLightning(deltaTime);
            UpdateRain();
        }

        /// <summary>World authority will call this in Phase 19 to synchronize weather for all clients.</summary>
        public void SetWeather(WeatherType type, bool immediate = false)
        {
            Current = type;
            from = current;
            to = PresetOf(type);
            blendT = immediate ? 1f : 0f;
            if (immediate)
                current = to;
            Changed?.Invoke(type);
        }

        private float RandomInterval()
        {
            return random.Range(changeIntervalSeconds.x, Mathf.Max(changeIntervalSeconds.x, changeIntervalSeconds.y));
        }

        private WeatherType PickNext()
        {
            for (int i = 0; i < 8; i++)
            {
                float value = random.Next01();
                WeatherType candidate = value < 0.45f ? WeatherType.Clear
                    : value < 0.60f ? WeatherType.Fog
                    : value < 0.85f ? WeatherType.Rain
                    : WeatherType.Storm;
                if (candidate != Current)
                    return candidate;
            }
            return WeatherType.Clear;
        }

        private void UpdateSwampFog(float deltaTime)
        {
            float target = 0f;
            IslandGenerator currentIsland = ArchipelagoManager.Instance != null && player != null
                ? ArchipelagoManager.Instance.GetIslandAt(player.position) : island;
            if (currentIsland != null && player != null && currentIsland.IsGenerated &&
                currentIsland.ContainsWorldPosition(player.position) &&
                currentIsland.GetBiome(player.position.x, player.position.z) == Biome.Swamp)
            {
                target = swampFogBonus;
            }
            swampFog = Mathf.MoveTowards(swampFog, target, swampFogBonus * deltaTime / 4f);
        }

        private void UpdateLightning(float deltaTime)
        {
            Flash = Mathf.MoveTowards(Flash, 0f, deltaTime * 3.5f);
            if (current.rain < 0.9f)
                return;

            lightningTimer -= deltaTime;
            if (lightningTimer <= 0f)
            {
                Flash = 1f;
                Lightning?.Invoke();
                lightningTimer = random.Range(5f, 15f);
            }
        }

        private void BuildRain()
        {
            if (rainMaterial == null)
            {
                Debug.LogWarning("[Weather] Chưa gán M_Rain; hiệu ứng mưa sẽ tắt.", this);
                return;
            }

            GameObject rainObject = new GameObject("Rain_FollowCamera");
            rainTransform = rainObject.transform;
            rainTransform.SetParent(transform, false);
            rainTransform.rotation = Quaternion.Euler(90f, 0f, 0f);

            rain = rainObject.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = rain.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.9f;
            main.startSpeed = 0f;
            main.startSize = 0.06f;
            main.startColor = new Color(0.8f, 0.85f, 0.95f, 0.5f);
            main.maxParticles = maxRainParticles;
            main.gravityModifier = 0f;

            ParticleSystem.ShapeModule shape = rain.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 40f, 0.1f);

            ParticleSystem.VelocityOverLifetimeModule velocity = rain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(-28f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            ParticleSystem.EmissionModule emission = rain.emission;
            emission.rateOverTime = 0f;

            ParticleSystemRenderer renderer = rainObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1f;
            renderer.velocityScale = 0.04f;
            renderer.sharedMaterial = rainMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void UpdateRain()
        {
            if (rain == null)
                return;

            if (followTarget != null)
                rainTransform.position = followTarget.position + Vector3.up * 14f;

            ParticleSystem.EmissionModule emission = rain.emission;
            emission.rateOverTime = current.rain * maxRainParticles / 0.9f;
            bool shouldPlay = current.rain > 0.02f;
            if (shouldPlay && !rain.isPlaying)
                rain.Play();
            else if (!shouldPlay && rain.isPlaying)
                rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void OnValidate()
        {
            blendSeconds = Mathf.Max(1f, blendSeconds);
            changeIntervalSeconds.x = Mathf.Max(5f, changeIntervalSeconds.x);
            changeIntervalSeconds.y = Mathf.Max(changeIntervalSeconds.x, changeIntervalSeconds.y);
            maxRainParticles = Mathf.Clamp(maxRainParticles, 100, 3000);
            swampFogBonus = Mathf.Max(0f, swampFogBonus);
        }
    }
}
