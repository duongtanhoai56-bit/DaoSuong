using System;
using UnityEngine;

namespace Game.Gameplay
{
    [Serializable]
    public sealed class FishDrop
    {
        public ItemData item;
        [Min(0f)] public float weight = 1f;
        [Min(1)] public int min = 1;
        [Min(1)] public int max = 1;
    }

    [CreateAssetMenu(menuName = "Game/Fishing Table", fileName = "FishingTable")]
    public sealed class FishingTable : ScriptableObject
    {
        public FishDrop[] drops = Array.Empty<FishDrop>();

        public ItemData Roll(System.Random random, out int amount)
        {
            amount = 0;
            if (random == null || drops == null || drops.Length == 0) return null;

            double totalWeight = 0d;
            for (int i = 0; i < drops.Length; i++)
            {
                FishDrop drop = drops[i];
                if (drop != null && drop.item != null && IsUsableWeight(drop.weight))
                    totalWeight += drop.weight;
            }
            if (totalWeight <= 0d) return null;

            double roll = random.NextDouble() * totalWeight;
            for (int i = 0; i < drops.Length; i++)
            {
                FishDrop drop = drops[i];
                if (drop == null || drop.item == null || !IsUsableWeight(drop.weight)) continue;
                roll -= drop.weight;
                if (roll < 0d)
                {
                    amount = GatherRules.RollAmount(random, drop.min, drop.max);
                    return drop.item;
                }
            }

            // Floating-point edge: return the final entry with a positive weight.
            for (int i = drops.Length - 1; i >= 0; i--)
            {
                FishDrop drop = drops[i];
                if (drop != null && drop.item != null && IsUsableWeight(drop.weight))
                {
                    amount = GatherRules.RollAmount(random, drop.min, drop.max);
                    return drop.item;
                }
            }
            return null;
        }

        private static bool IsUsableWeight(float value)
            => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;

        private void OnValidate()
        {
            if (drops == null) drops = Array.Empty<FishDrop>();
            foreach (FishDrop drop in drops)
            {
                if (drop == null) continue;
                if (float.IsNaN(drop.weight) || float.IsInfinity(drop.weight)) drop.weight = 0f;
                drop.weight = Mathf.Max(0f, drop.weight);
                drop.min = Mathf.Max(1, drop.min);
                drop.max = Mathf.Max(drop.min, drop.max);
            }
        }
    }
}
