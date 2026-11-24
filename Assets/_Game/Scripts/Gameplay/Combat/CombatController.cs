using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CombatController : MonoBehaviour
    {
        [SerializeField] private SurvivalController survival;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField, Min(0.5f)] private float attackRange = 3.8f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 0.55f;
        [SerializeField, Min(1f)] private float baseDamage = 18f;
        [SerializeField, Range(-1f, 1f)] private float minimumFacingDot = 0.1f;

        private float nextAttackTime;
        public float AttackRange => attackRange;

        private void Awake()
        {
            if (survival == null) survival = GetComponent<SurvivalController>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
        }

        public bool TryAttack()
        {
            if ((survival != null && survival.IsDead) || Time.timeScale <= 0f || GameplayInputGate.IsLocked) return false;
            if (Time.time < nextAttackTime) return false;
            nextAttackTime = Time.time + attackCooldown;

            CreatureAgent target = FindTarget();
            if (target == null)
            {
                HudMessages.Toast("Không có quái trong tầm đánh.");
                return false;
            }

            float damage = baseDamage;
            if (inventory != null)
            {
                ItemStack axe = inventory.PickTool(ToolType.Axe, 1);
                ItemStack pickaxe = inventory.PickTool(ToolType.Pickaxe, 1);
                ItemStack weaponStack = axe != null && (pickaxe == null || axe.Tool.power >= pickaxe.Tool.power) ? axe : pickaxe;
                ToolData weapon = weaponStack != null ? weaponStack.Tool : null;
                if (weapon != null)
                {
                    damage += weapon.power * 8f + weapon.tier * 2f;
                    weaponStack.durability = Mathf.Max(0, weaponStack.durability - 1);
                    if (weaponStack.durability <= 0) inventory.RemoveBroken();
                    else inventory.NotifyChanged();
                }
            }
            target.ApplyDamage(damage);
            HudMessages.Toast("Đánh trúng " + target.DisplayName + " (-" + Mathf.RoundToInt(damage) + " HP)");
            return true;
        }

        private CreatureAgent FindTarget()
        {
            CreatureAgent[] agents = FindObjectsByType<CreatureAgent>(FindObjectsSortMode.None);
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            CreatureAgent best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < agents.Length; i++)
            {
                CreatureAgent candidate = agents[i];
                if (candidate == null || candidate.IsDead) continue;
                Vector3 delta = candidate.transform.position - transform.position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance > attackRange || distance < 0.01f || distance >= bestDistance) continue;
                if (Vector3.Dot(forward, delta / distance) < minimumFacingDot) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        private void OnValidate()
        {
            attackRange = Mathf.Clamp(attackRange, 0.5f, 10f);
            attackCooldown = Mathf.Clamp(attackCooldown, 0.1f, 5f);
            baseDamage = Mathf.Clamp(baseDamage, 1f, 1000f);
        }
    }
}
