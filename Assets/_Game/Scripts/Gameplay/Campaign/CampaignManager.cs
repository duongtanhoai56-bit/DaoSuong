using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Tracks a single 99-day run and boss milestones. Day progress saves locally.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-500)]
    public sealed class CampaignManager : MonoBehaviour
    {
        private const string SaveKey = "DAO_SUONG_CAMPAIGN_V1";

        [SerializeField] private DayNightCycle dayNight;
        [SerializeField] private SurvivalController survival;
        [SerializeField, Min(1)] private int requiredDays = 99;
        [SerializeField] private bool loadSavedRun = true;

        [Serializable]
        private sealed class SaveData
        {
            public int version = 1;
            public int survivedDays;
            public bool victory;
            public bool defeated;
            public string[] defeatedBossIds = Array.Empty<string>();
        }

        private readonly HashSet<string> defeatedBossIds = new HashSet<string>(StringComparer.Ordinal);
        private bool subscribed;

        public int SurvivedDays { get; private set; }
        public int RequiredDays => Mathf.Max(1, requiredDays);
        public bool HasWon { get; private set; }
        public bool HasLost { get; private set; }
        public bool IsFinished => HasWon || HasLost;
        public IReadOnlyCollection<string> DefeatedBossIds => defeatedBossIds;
        public DayNightCycle Clock => dayNight;
        public event Action<int, int> ProgressChanged;
        public event Action Won;
        public event Action Lost;
        public event Action<string> BossDefeated;

        private void Awake()
        {
            if (dayNight == null) dayNight = FindFirstObjectByType<DayNightCycle>();
            if (survival == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) survival = player.GetComponent<SurvivalController>();
            }
            if (loadSavedRun) Load();
        }

        private void OnEnable()
        {
            Subscribe();
            if (survival != null) survival.Died += HandlePlayerDied;
        }

        private void Start()
        {
            ProgressChanged?.Invoke(SurvivedDays, RequiredDays);
            if (HasWon) Won?.Invoke();
            else if (HasLost) Lost?.Invoke();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (survival != null) survival.Died -= HandlePlayerDied;
        }

        public bool IsBossDefeated(string bossId)
            => !string.IsNullOrWhiteSpace(bossId) && defeatedBossIds.Contains(bossId);

        public bool IsIslandUnlocked(IslandDestination destination)
        {
            if (destination == null) return false;
            return CampaignProgressRules.IsIslandUnlocked(SurvivedDays, destination.unlockDay,
                destination.requiredBossId, defeatedBossIds);
        }

        public void MarkBossDefeated(string bossId)
        {
            if (string.IsNullOrWhiteSpace(bossId) || IsFinished) return;
            if (!defeatedBossIds.Add(bossId)) return;
            Save();
            BossDefeated?.Invoke(bossId);
        }

        public void ResetRun()
        {
            SurvivedDays = 0;
            if (dayNight != null)
            {
                dayNight.SetDayCount(0);
                dayNight.SetTime01(0.28f);
            }
            HasWon = false;
            HasLost = false;
            defeatedBossIds.Clear();
            Save();
            ProgressChanged?.Invoke(SurvivedDays, RequiredDays);
        }

        private void Subscribe()
        {
            if (subscribed || dayNight == null) return;
            dayNight.DayChanged += HandleDayChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || dayNight == null) return;
            dayNight.DayChanged -= HandleDayChanged;
            subscribed = false;
        }

        private void HandleDayChanged(int dayNumber)
        {
            if (HasWon || HasLost) return;
            bool alive = survival == null || !survival.IsDead;
            if (!CampaignProgressRules.CanCountSurvivedDay(dayNumber, alive, SurvivedDays)) return;

            SurvivedDays = Mathf.Max(SurvivedDays, dayNumber);
            Save();
            ProgressChanged?.Invoke(SurvivedDays, RequiredDays);
            if (CampaignProgressRules.HasWon(SurvivedDays, RequiredDays))
            {
                HasWon = true;
                Save();
                Won?.Invoke();
            }
        }

        private void HandlePlayerDied()
        {
            if (HasWon || HasLost) return;
            HasLost = true;
            Save();
            Lost?.Invoke();
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                if (data == null || data.version != 1) return;
                SurvivedDays = Mathf.Clamp(data.survivedDays, 0, RequiredDays);
                if (dayNight != null) dayNight.SetDayCount(SurvivedDays);
                HasWon = data.victory || CampaignProgressRules.HasWon(SurvivedDays, RequiredDays);
                HasLost = data.defeated && !HasWon;
                defeatedBossIds.Clear();
                if (data.defeatedBossIds != null)
                    for (int i = 0; i < data.defeatedBossIds.Length; i++)
                        if (!string.IsNullOrWhiteSpace(data.defeatedBossIds[i])) defeatedBossIds.Add(data.defeatedBossIds[i]);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Campaign] Save không hợp lệ, bắt đầu run mới: " + exception.Message, this);
            }
        }

        private void Save()
        {
            SaveData data = new SaveData
            {
                survivedDays = SurvivedDays,
                victory = HasWon,
                defeated = HasLost,
                defeatedBossIds = new List<string>(defeatedBossIds).ToArray()
            };
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void OnValidate() => requiredDays = Mathf.Max(1, requiredDays);
    }
}
