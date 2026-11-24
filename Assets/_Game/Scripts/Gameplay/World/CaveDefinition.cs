using System;
using DaoSuong.Content;
using UnityEngine;

namespace Game.Gameplay
{
    [Serializable]
    public sealed class CaveLootEntry
    {
        public string stableId;
        public ItemData item;
        [Min(0)] public int minAmount = 1;
        [Min(0)] public int maxAmount = 2;
        [Range(0f, 1f)] public float spawnChance = 1f;
        [Range(0, 4)] public int roomIndex = 1;
        public ItemData requiredKey;
        public bool consumeKey = true;
        [TextArea(2, 5)] public string secretText;
    }

    [CreateAssetMenu(menuName = "Game/World/Cave Definition", fileName = "CaveDefinition")]
    public sealed class CaveDefinition : ScriptableObject
    {
        public string stableId;
        public string islandId;
        public string displayName = "Hang Bí Ẩn";
        [TextArea(3, 8)] public string hiddenStory;
        [Min(2)] public int roomCount = 3;
        [Min(0)] public int dangerLevel = 1;
        [Range(-10f, 45f)] public float ambientTemperatureC = 14f;
        [Range(0f, 1f)] public float environmentExposure = 0.08f;
        public CreatureDefinition guardian;
        public Color rockColor = new Color(0.18f, 0.2f, 0.23f);
        public Color crystalColor = new Color(0.22f, 0.78f, 0.92f);
        public CaveLootEntry[] loot = Array.Empty<CaveLootEntry>();

        private void OnValidate()
        {
            stableId = string.IsNullOrWhiteSpace(stableId) ? name.ToLowerInvariant().Replace(' ', '_') : stableId.Trim();
            islandId = (islandId ?? string.Empty).Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();
            roomCount = CaveRules.ClampRoomCount(roomCount);
            dangerLevel = Mathf.Clamp(dangerLevel, 0, 5);
            if (float.IsNaN(ambientTemperatureC) || float.IsInfinity(ambientTemperatureC)) ambientTemperatureC = 14f;
            ambientTemperatureC = Mathf.Clamp(ambientTemperatureC, -10f, 45f);
            if (float.IsNaN(environmentExposure) || float.IsInfinity(environmentExposure)) environmentExposure = 0.08f;
            environmentExposure = Mathf.Clamp01(environmentExposure);
            if (loot == null) loot = Array.Empty<CaveLootEntry>();
            for (int i = 0; i < loot.Length; i++)
            {
                CaveLootEntry entry = loot[i];
                if (entry == null) continue;
                entry.stableId = (entry.stableId ?? string.Empty).Trim();
                entry.minAmount = Mathf.Clamp(entry.minAmount, 0, 999);
                entry.maxAmount = Mathf.Clamp(entry.maxAmount, entry.minAmount, 999);
                if (float.IsNaN(entry.spawnChance) || float.IsInfinity(entry.spawnChance)) entry.spawnChance = 0f;
                entry.spawnChance = Mathf.Clamp01(entry.spawnChance);
                entry.roomIndex = Mathf.Clamp(entry.roomIndex, 0, 4);
            }
        }
    }
}
