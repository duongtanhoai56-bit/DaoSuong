using DaoSuong.Content;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Lightweight pursuit AI for mobile. Boss agents share the same combat lifecycle.</summary>
    [DisallowMultipleComponent]
    public sealed class CreatureAgent : MonoBehaviour
    {
        private CreatureDefinition creature;
        private BossDefinition boss;
        private Transform target;
        private SurvivalController targetSurvival;
        private PlayerInventory rewardInventory;
        private CampaignManager campaign;
        private ArchipelagoManager archipelago;
        private float maximumHealth;
        private float moveSpeed;
        private float damage;
        private float detectionRange;
        private float attackRange;
        private float attackCooldown;
        private float areaRadius;
        private float areaDamage;
        private float phaseTwoMultiplier;
        private float nextAttack;
        private bool isDead;
        private bool isBoss;
        private Renderer bodyRenderer;
        private Collider bodyCollider;

        public string StableId => isBoss && boss != null ? boss.stableId : creature != null ? creature.stableId : string.Empty;
        public string DisplayName => isBoss && boss != null ? boss.displayName : creature != null ? creature.displayName : "Sinh vật";
        public bool IsBoss => isBoss;
        public bool IsDead => isDead;
        public float Health { get; private set; }
        public float MaxHealth => maximumHealth;
        public float Health01 => maximumHealth > 0f ? Mathf.Clamp01(Health / maximumHealth) : 0f;

        public void Initialize(CreatureDefinition creatureDefinition, BossDefinition bossDefinition,
            Transform player, CampaignManager campaignManager, PlayerInventory inventory, ArchipelagoManager world)
        {
            creature = creatureDefinition;
            boss = bossDefinition;
            isBoss = boss != null;
            target = player;
            targetSurvival = player != null ? player.GetComponent<SurvivalController>() : null;
            rewardInventory = inventory;
            campaign = campaignManager;
            archipelago = world;
            maximumHealth = isBoss ? boss.maxHealth : creature != null ? creature.maxHealth : 1f;
            Health = maximumHealth;
            moveSpeed = isBoss ? boss.moveSpeed : creature != null ? creature.moveSpeed : 2f;
            damage = isBoss ? boss.damage : creature != null ? creature.damage : 4f;
            detectionRange = isBoss ? boss.detectionRange : creature != null ? creature.detectionRange : 12f;
            attackRange = isBoss ? boss.attackRange : creature != null ? creature.attackRange : 1.6f;
            attackCooldown = isBoss ? boss.attackCooldown : creature != null ? creature.attackCooldown : 2f;
            areaRadius = isBoss ? boss.areaAttackRadius : 0f;
            areaDamage = isBoss ? boss.areaAttackDamage : 0f;
            phaseTwoMultiplier = isBoss ? boss.phaseTwoDamageMultiplier : 1f;
            float scale = isBoss ? boss.bodyScale : creature != null ? creature.bodyScale : 1f;
            Color color = isBoss ? boss.bodyColor : creature != null ? creature.bodyColor : Color.red;
            transform.localScale = Vector3.one * Mathf.Clamp(scale, 0.1f, 15f);
            bodyRenderer = GetComponent<Renderer>();
            if (bodyRenderer != null) bodyRenderer.material.color = color;
            bodyCollider = GetComponent<Collider>();
            if (bodyCollider == null) bodyCollider = gameObject.AddComponent<CapsuleCollider>();
            gameObject.name = (isBoss ? "BOSS_" : "Creature_") + StableId;
        }

        public bool ApplyDamage(float amount)
        {
            if (isDead || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f) return false;
            Health = Mathf.Max(0f, Health - Mathf.Clamp(amount, 0f, 1000000f));
            if (Health > 0f) return true;

            isDead = true;
            if (bodyCollider != null) bodyCollider.enabled = false;
            RewardPlayer();
            if (isBoss && campaign != null) campaign.MarkBossDefeated(boss.stableId);
            Debug.Log(isBoss ? "[Boss] Đã đánh bại: " + DisplayName : "[Creature] Đã tiêu diệt: " + DisplayName, this);
            Destroy(gameObject, 1.2f);
            return true;
        }

        private void Update()
        {
            if (isDead || target == null || targetSurvival == null || targetSurvival.IsDead) return;
            CaveManager cave = CaveManager.Instance;
            // Surface enemies cannot attack through a cave ceiling. Only agents parented
            // to the active dungeon may pursue a player who is currently underground.
            if (cave != null && cave.IsInsideCave && !cave.IsInsideActiveCave(transform)) return;
            Vector3 flatToTarget = target.position - transform.position;
            flatToTarget.y = 0f;
            float distance = flatToTarget.magnitude;
            if (distance > detectionRange) return;

            float healthRatio = Health01;
            bool secondPhase = isBoss && healthRatio <= 0.5f;
            Vector3 direction = distance > 0.001f ? flatToTarget / distance : Vector3.zero;
            float effectiveAttackRange = isBoss && secondPhase
                ? Mathf.Max(attackRange, areaRadius)
                : attackRange;
            if (distance > effectiveAttackRange)
            {
                transform.position += direction * moveSpeed * (secondPhase ? 1.25f : 1f) * Time.deltaTime;
                if (direction.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
                SnapToGround();
                return;
            }

            if (Time.time < nextAttack) return;
            nextAttack = Time.time + attackCooldown * (secondPhase ? 0.78f : 1f);
            float nightMultiplier = campaign != null && campaign.Clock != null && campaign.Clock.IsNight ? 1.15f : 1f;
            float attackDamage = damage * nightMultiplier * (secondPhase ? phaseTwoMultiplier : 1f);
            // Phase-two area strike replaces the melee hit; never stacks a second damage packet.
            if (isBoss && secondPhase && areaRadius > 0f && areaDamage > 0f && distance <= areaRadius)
                attackDamage = Mathf.Max(attackDamage, areaDamage * nightMultiplier);
            targetSurvival.ApplyDamage(attackDamage);
        }

        private void SnapToGround()
        {
            CaveManager cave = CaveManager.Instance;
            if (cave != null && cave.IsInsideActiveCave(transform))
            {
                Vector3 cavePosition = transform.position;
                cavePosition.y = cave.ActiveFloorY + Mathf.Max(0.5f, transform.localScale.y);
                transform.position = cavePosition;
                return;
            }
            if (archipelago == null) return;
            IslandGenerator island = archipelago.GetIslandAt(transform.position);
            if (island == null || !island.ContainsWorldPosition(transform.position)) return;
            Vector3 position = transform.position;
            position.y = island.GetHeight(position.x, position.z) + transform.localScale.y;
            transform.position = position;
        }

        private void RewardPlayer()
        {
            ItemData item = isBoss ? boss.rewardItem : creature != null ? creature.rewardItem : null;
            int amount = isBoss ? boss.rewardAmount : creature != null ? creature.rewardAmount : 0;
            if (item == null || amount <= 0) return;
            int left = rewardInventory != null ? rewardInventory.Add(item, amount) : amount;
            int received = amount - left;
            if (received > 0) HudMessages.Toast("+" + received + " " + item.displayName);
            if (left > 0) WorldPickup.Spawn(item, left, transform.position + Vector3.up * 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isBoss ? new Color(0.9f, 0.15f, 0.1f, 0.18f) : new Color(0.9f, 0.6f, 0.1f, 0.08f);
            Gizmos.DrawSphere(transform.position, isBoss ? areaRadius : attackRange);
        }
    }
}
