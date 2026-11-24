using System;
using UnityEngine;

namespace Game.Gameplay
{
    [Serializable]
    public sealed class PickupRule
    {
        public ItemData item;
        public Biome[] biomes = Array.Empty<Biome>();
        [Min(0)] public int count = 30;
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 2;
        [Min(1f)] public float respawnSeconds = 300f;

        public bool Allows(Biome biome)
        {
            if (biomes == null) return false;
            for (int i = 0; i < biomes.Length; i++)
                if (biomes[i] == biome) return true;
            return false;
        }
    }

    [CreateAssetMenu(menuName = "Game/Gather Settings", fileName = "GatherSettings")]
    public sealed class GatherSettings : ScriptableObject
    {
        public ResourceNodeData tree;
        public ResourceNodeData rockDefault;
        public ResourceNodeData rockMountain;
        public PickupRule[] pickups = Array.Empty<PickupRule>();
        public FishingTable fishing;
        public ToolData[] debugTools = Array.Empty<ToolData>();

        private void OnValidate()
        {
            if (pickups == null) pickups = Array.Empty<PickupRule>();
            if (debugTools == null) debugTools = Array.Empty<ToolData>();

            foreach (PickupRule rule in pickups)
            {
                if (rule == null) continue;
                rule.count = Mathf.Max(0, rule.count);
                rule.minAmount = Mathf.Max(1, rule.minAmount);
                rule.maxAmount = Mathf.Max(rule.minAmount, rule.maxAmount);
                if (float.IsNaN(rule.respawnSeconds) || float.IsInfinity(rule.respawnSeconds))
                    rule.respawnSeconds = 300f;
                rule.respawnSeconds = Mathf.Max(1f, rule.respawnSeconds);
                if (rule.biomes == null) rule.biomes = Array.Empty<Biome>();
            }
        }
    }
}
