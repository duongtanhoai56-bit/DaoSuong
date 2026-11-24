using UnityEngine;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlacedBuilding : MonoBehaviour
    {
        public string DefinitionId { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }

        public void Initialize(string id, float maxHealth)
        {
            DefinitionId = id ?? string.Empty;
            MaxHealth = float.IsNaN(maxHealth) || float.IsInfinity(maxHealth) ? 100f : Mathf.Clamp(maxHealth, 1f, 100000f);
            Health = MaxHealth;
        }

        public bool ApplyDamage(float damage)
        {
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) return false;
            Health = Mathf.Max(0f, Health - Mathf.Min(damage, 100000f));
            if (Health > 0f) return true;
            Destroy(gameObject);
            return false;
        }
    }
}
