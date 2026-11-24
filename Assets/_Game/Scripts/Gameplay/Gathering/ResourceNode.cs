using System;
using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    /// <summary>One generated tree/rock. The owner uses SetActive so GPU-instanced props hide too.</summary>
    [DisallowMultipleComponent]
    public sealed class ResourceNode : GatherTarget
    {
        private const float ShakeDuration = 0.22f;

        public int Id { get; private set; }
        public ResourceNodeData Data { get; private set; }
        public float Health { get; private set; }
        public bool IsDepleted { get; private set; }
        public override bool IsAvailable => !IsDepleted;

        private Quaternion baseLocalRotation;
        private float shakeTimer;
        private bool initialized;

        public void Initialize(int id, ResourceNodeData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            Id = id;
            Data = data;
            Health = data.health;
            IsDepleted = false;
            baseLocalRotation = transform.localRotation;
            initialized = true;
            enabled = false;
        }

        /// <summary>Authority moves to the server in Phase 19.</summary>
        public bool ApplyHit(float power, IItemReceiver receiver, System.Random random)
        {
            if (!initialized || IsDepleted) return false;

            Health = GatherRules.ApplyDamage(Health, power);
            shakeTimer = ShakeDuration;
            enabled = true;
            if (Health > 0f) return false;

            IsDepleted = true;
            DropLoot(receiver, random);
            RespawnAt = Time.time + Data.respawnSeconds;
            RaiseDepleted();
            SetVisible(false);
            return true;
        }

        public override void Respawn()
        {
            if (!initialized) return;
            transform.localRotation = baseLocalRotation;
            Health = Data.health;
            IsDepleted = false;
            shakeTimer = 0f;
            SetVisible(true);
        }

        private void Update()
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0f)
            {
                transform.localRotation = baseLocalRotation;
                enabled = false;
                return;
            }

            float fade = shakeTimer / ShakeDuration;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(
                Mathf.Sin(shakeTimer * 70f) * 5f * fade,
                0f,
                Mathf.Cos(shakeTimer * 63f) * 5f * fade);
        }

        private void SetVisible(bool visible)
        {
            // Tree/rock geometry is submitted by IslandInstanceRenderer, not by per-object Renderers.
            // Inactive transforms are skipped by that renderer and their colliders are disabled too.
            gameObject.SetActive(visible);
        }

        private void DropLoot(IItemReceiver receiver, System.Random random)
        {
            if (Data.drops == null) return;
            System.Random rng = random ?? new System.Random(Id ^ Environment.TickCount);

            foreach (DropEntry entry in Data.drops)
            {
                if (entry == null || entry.item == null || float.IsNaN(entry.chance) ||
                    float.IsInfinity(entry.chance)) continue;
                float chance = Mathf.Clamp01(entry.chance);
                if (chance <= 0f) continue;
                if (chance < 1f && rng.NextDouble() >= chance) continue;

                int amount = GatherRules.RollAmount(rng, entry.min, entry.max);
                if (amount <= 0) continue;
                int left = receiver != null ? Mathf.Clamp(receiver.Add(entry.item, amount), 0, amount) : amount;
                int received = amount - left;
                if (received > 0) HudMessages.Toast($"+{received} {entry.item.displayName}");
                if (left > 0)
                    WorldPickup.Spawn(entry.item, left, transform.position + Vector3.up * 0.45f);
            }
        }
    }
}
