using UnityEngine;
using Game.Gameplay;

namespace DaoSuong.Content
{
    [CreateAssetMenu(menuName = "Dao Suong/Creatures/Creature Definition", fileName = "SO_Creature_New")]
    public sealed class CreatureDefinition : ScriptableObject
    {
        public string stableId = "creature";
        public string displayName = "Sinh vật";
        [Min(1f)] public float maxHealth = 35f;
        [Min(0f)] public float moveSpeed = 2.5f;
        [Min(0f)] public float damage = 6f;
        [Min(1f)] public float detectionRange = 18f;
        [Min(0.5f)] public float attackRange = 1.8f;
        [Min(0.2f)] public float attackCooldown = 2f;
        [Min(0.1f)] public float bodyScale = 1f;
        public Color bodyColor = new Color(0.4f, 0.5f, 0.3f);
        [Min(1)] public int rewardAmount = 1;
        public ItemData rewardItem;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(stableId)) stableId = name.ToLowerInvariant().Replace(' ', '_');
            if (string.IsNullOrWhiteSpace(displayName)) displayName = name;
            maxHealth = Safe(maxHealth, 35f, 1f, 100000f);
            moveSpeed = Safe(moveSpeed, 2.5f, 0f, 20f);
            damage = Safe(damage, 6f, 0f, 10000f);
            detectionRange = Safe(detectionRange, 18f, 1f, 100f);
            attackRange = Safe(attackRange, 1.8f, 0.5f, 10f);
            attackCooldown = Safe(attackCooldown, 2f, 0.2f, 30f);
            bodyScale = Safe(bodyScale, 1f, 0.1f, 8f);
            rewardAmount = Mathf.Clamp(rewardAmount, 1, 999);
        }

        private static float Safe(float value, float fallback, float min, float max)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
}
