using System;
using UnityEngine;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/World/Cave Catalog", fileName = "CaveCatalog")]
    public sealed class CaveCatalog : ScriptableObject
    {
        public CaveDefinition[] caves = Array.Empty<CaveDefinition>();

        public CaveDefinition FindForIsland(string islandId)
        {
            if (string.IsNullOrWhiteSpace(islandId) || caves == null) return null;
            for (int i = 0; i < caves.Length; i++)
            {
                CaveDefinition cave = caves[i];
                if (cave != null && string.Equals(cave.islandId, islandId, StringComparison.Ordinal)) return cave;
            }
            return null;
        }

        private void OnValidate()
        {
            if (caves == null) caves = Array.Empty<CaveDefinition>();
        }
    }

    /// <summary>Small pure rules boundary for deterministic cave generation and safe loot handling.</summary>
    public static class CaveRules
    {
        public const int MinimumRooms = 2;
        public const int MaximumRooms = 5;

        public static int ClampRoomCount(int value) => Mathf.Clamp(value, MinimumRooms, MaximumRooms);

        public static bool ShouldSpawnLoot(float roll01, float chance)
        {
            if (float.IsNaN(roll01) || float.IsInfinity(roll01) ||
                float.IsNaN(chance) || float.IsInfinity(chance)) return false;
            return Mathf.Clamp01(roll01) < Mathf.Clamp01(chance);
        }

        public static bool CanEnter(bool hasDefinition, bool alreadyInside, bool campaignFinished)
            => hasDefinition && !alreadyInside && !campaignFinished;

        public static bool ShouldRecoverFromSurfaceFall(bool insideCave, float playerY, float recoveryThreshold)
        {
            if (insideCave || float.IsNaN(playerY) || float.IsInfinity(playerY) ||
                float.IsNaN(recoveryThreshold) || float.IsInfinity(recoveryThreshold)) return false;
            return playerY < recoveryThreshold;
        }

        public static bool CanOpenLockedTreasure(bool requiresKey, bool hasKey)
            => !requiresKey || hasKey;

        public static int AcceptedAmount(int requested, int unaccepted)
        {
            if (requested <= 0) return 0;
            return Mathf.Clamp(requested - Mathf.Clamp(unaccepted, 0, requested), 0, requested);
        }

        public static bool ShouldCreateTreasure(bool hasItem, int amount, bool hasLore)
            => (hasItem && amount > 0) || hasLore;

        public static int ClampSavedRemainingAmount(int value)
            => Mathf.Clamp(value, 0, 999);

        public static bool IsStableIdValid(string value)
            => !string.IsNullOrWhiteSpace(value) && value.Length <= 80 && value.Trim() == value;
    }
}
