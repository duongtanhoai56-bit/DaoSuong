using UnityEngine;
using Game.Gameplay;

namespace DaoSuong.Content
{
    [CreateAssetMenu(menuName = "Dao Suong/Bosses/Boss Definition", fileName = "SO_Boss_New")]
    public sealed class BossDefinition : ScriptableObject
    {
        public string stableId = "boss";
        public string displayName = "Thủ lĩnh";
        [Min(1f)] public float maxHealth = 500f;
        [Min(1)] public int minimumPlayers = 1;
        [Min(1)] public int maximumPlayers = 4;
        [Min(1)] public int spawnDay = 3;
        [Min(0f)] public float damage = 18f;
        [Min(0f)] public float phaseTwoDamageMultiplier = 1.5f;
        [Min(0.5f)] public float moveSpeed = 1.8f;
        [Min(1f)] public float detectionRange = 32f;
        [Min(0.5f)] public float attackRange = 2.8f;
        [Min(0.5f)] public float attackCooldown = 3f;
        [Min(0f)] public float areaAttackRadius = 5f;
        [Min(0f)] public float areaAttackDamage = 16f;
        [Min(1f)] public float bodyScale = 2.2f;
        public Color bodyColor = new Color(0.45f, 0.2f, 0.22f);
        [Min(1)] public int rewardAmount = 1;
        public ItemData rewardItem;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(stableId)) stableId = name.ToLowerInvariant().Replace(' ', '_');
            if (string.IsNullOrWhiteSpace(displayName)) displayName = name;
            maxHealth = Safe(maxHealth, 500f, 1f, 1000000f);
            minimumPlayers = Mathf.Clamp(minimumPlayers, 1, 8);
            maximumPlayers = Mathf.Clamp(maximumPlayers, minimumPlayers, 8);
            spawnDay = Mathf.Max(1, spawnDay);
            damage = Safe(damage, 18f, 0f, 100000f);
            phaseTwoDamageMultiplier = Safe(phaseTwoDamageMultiplier, 1.5f, 1f, 10f);
            moveSpeed = Safe(moveSpeed, 1.8f, 0f, 20f);
            detectionRange = Safe(detectionRange, 32f, 1f, 200f);
            attackRange = Safe(attackRange, 2.8f, 0.5f, 20f);
            attackCooldown = Safe(attackCooldown, 3f, 0.5f, 60f);
            areaAttackRadius = Safe(areaAttackRadius, 5f, 0f, 30f);
            areaAttackDamage = Safe(areaAttackDamage, 16f, 0f, 100000f);
            bodyScale = Safe(bodyScale, 2.2f, 0.5f, 15f);
            rewardAmount = Mathf.Clamp(rewardAmount, 1, 999);
        }

        private static float Safe(float value, float fallback, float min, float max)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
}
