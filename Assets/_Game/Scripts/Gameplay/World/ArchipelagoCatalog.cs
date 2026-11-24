using System;
using UnityEngine;
using DaoSuong.Content;

namespace Game.Gameplay
{
    [Serializable]
    public sealed class IslandDestination
    {
        public string id;
        public string displayName;
        public IslandSettings settings;
        public Vector3 worldPosition;
        public int seed = 12345;
        [Min(0)] public int unlockDay;
        public string requiredBossId;
        [Min(0)] public int creatureCount = 3;
        public CreatureDefinition[] creatures;
        public BossDefinition boss;
    }

    [CreateAssetMenu(menuName = "Game/World/Archipelago Catalog", fileName = "ArchipelagoCatalog")]
    public sealed class ArchipelagoCatalog : ScriptableObject
    {
        public IslandDestination[] islands = Array.Empty<IslandDestination>();

        public IslandDestination Find(string islandId)
        {
            if (string.IsNullOrWhiteSpace(islandId) || islands == null) return null;
            for (int i = 0; i < islands.Length; i++)
                if (islands[i] != null && string.Equals(islands[i].id, islandId, StringComparison.Ordinal))
                    return islands[i];
            return null;
        }

        private void OnValidate()
        {
            if (islands == null) islands = Array.Empty<IslandDestination>();
            for (int i = 0; i < islands.Length; i++)
            {
                IslandDestination destination = islands[i];
                if (destination == null) continue;
                destination.id = (destination.id ?? string.Empty).Trim();
                destination.displayName = string.IsNullOrWhiteSpace(destination.displayName) ? destination.id : destination.displayName.Trim();
                destination.unlockDay = Mathf.Max(0, destination.unlockDay);
                destination.creatureCount = Mathf.Clamp(destination.creatureCount, 0, 20);
            }
        }
    }
}
