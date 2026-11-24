using System;
using UnityEngine;

namespace Game.Gameplay
{
    [Serializable]
    public sealed class DropEntry
    {
        public ItemData item;
        [Min(1)] public int min = 1;
        [Min(1)] public int max = 1;
        [Range(0f, 1f)] public float chance = 1f;
    }

    [CreateAssetMenu(menuName = "Game/Resource Node", fileName = "ResourceNode")]
    public sealed class ResourceNodeData : ScriptableObject
    {
        public string displayName = "Cây";
        public string verb = "chặt";
        public ToolType requiredTool = ToolType.Axe;
        [Min(1)] public int minTier = 1;
        [Min(0.1f)] public float health = 6f;
        [Min(0)] public int durabilityCostPerHit = 1;
        [Min(1f)] public float respawnSeconds = 420f;
        public DropEntry[] drops = Array.Empty<DropEntry>();

        private void OnValidate()
        {
            minTier = Mathf.Max(1, minTier);
            if (float.IsNaN(health) || float.IsInfinity(health)) health = 6f;
            if (float.IsNaN(respawnSeconds) || float.IsInfinity(respawnSeconds)) respawnSeconds = 420f;
            health = Mathf.Max(0.1f, health);
            durabilityCostPerHit = Mathf.Max(0, durabilityCostPerHit);
            respawnSeconds = Mathf.Max(1f, respawnSeconds);
            if (drops == null) drops = Array.Empty<DropEntry>();
            foreach (DropEntry drop in drops)
            {
                if (drop == null) continue;
                drop.min = Mathf.Max(1, drop.min);
                drop.max = Mathf.Max(drop.min, drop.max);
                if (float.IsNaN(drop.chance) || float.IsInfinity(drop.chance)) drop.chance = 0f;
                drop.chance = Mathf.Clamp01(drop.chance);
            }
        }
    }
}
