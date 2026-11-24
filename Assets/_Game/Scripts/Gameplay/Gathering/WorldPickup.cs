using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WorldPickup : GatherTarget
    {
        public static Material DefaultMaterial { get; set; }
        private static readonly Dictionary<ItemData, Mesh> cachedMeshes = new Dictionary<ItemData, Mesh>();

        public ItemData Item { get; private set; }
        public int Amount { get; private set; }
        public float RespawnDelay { get; set; }
        public bool Collected { get; private set; }
        public override bool IsAvailable => !Collected;

        private int initialAmount;
        private MeshRenderer meshRenderer;
        private Collider pickupCollider;

        public static WorldPickup Spawn(ItemData item, int amount, Vector3 worldPosition, Transform parent = null)
        {
            return Spawn(item, amount, worldPosition, parent, Random.Range(0f, 360f));
        }

        public static WorldPickup Spawn(ItemData item, int amount, Vector3 worldPosition, Transform parent, float deterministicYaw)
        {
            if (item == null || amount <= 0)
            {
                Debug.LogWarning("WorldPickup.Spawn: item hoặc amount không hợp lệ.");
                return null;
            }
            if (DefaultMaterial == null)
            {
                Debug.LogError("WorldPickup: DefaultMaterial chưa được gán trong ResourceSpawner.");
                return null;
            }

            GameObject go = new GameObject("Pickup_" + item.name);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0f, deterministicYaw, 0f));

            go.AddComponent<MeshFilter>().sharedMesh = GetMesh(item);
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = DefaultMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.7f, 0.5f, 0.7f);

            WorldPickup pickup = go.AddComponent<WorldPickup>();
            pickup.Item = item;
            pickup.Amount = amount;
            pickup.initialAmount = amount;
            pickup.meshRenderer = renderer;
            pickup.pickupCollider = box;
            return pickup;
        }

        public bool Collect(IItemReceiver receiver)
        {
            if (Collected || Item == null || receiver == null) return false;

            int unaccepted = Mathf.Clamp(receiver.Add(Item, Amount), 0, Amount);
            int accepted = Amount - unaccepted;
            if (accepted <= 0)
            {
                Game.Core.HudMessages.Toast("Không thể nhặt thêm vật phẩm: túi đã đầy.");
                return false;
            }

            Game.Core.HudMessages.Toast($"+{accepted} {Item.displayName}");
            Amount = unaccepted;
            if (Amount > 0) return true;

            Collected = true;
            if (RespawnDelay > 0f)
            {
                SetVisible(false);
                RespawnAt = Time.time + RespawnDelay;
                RaiseDepleted();
            }
            else
            {
                Destroy(gameObject);
            }
            return true;
        }

        public override void Respawn()
        {
            if (initialAmount <= 0 || Item == null) return;
            Amount = initialAmount;
            Collected = false;
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (meshRenderer != null) meshRenderer.enabled = visible;
            if (pickupCollider != null) pickupCollider.enabled = visible;
        }

        private static Mesh GetMesh(ItemData item)
        {
            if (!cachedMeshes.TryGetValue(item, out Mesh mesh) || mesh == null)
            {
                mesh = LowPolyMeshes.Box(new Vector3(0.35f, 0.22f, 0.35f), item.color);
                mesh.name = "PickupMesh_" + item.name;
                cachedMeshes[item] = mesh;
            }
            return mesh;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            foreach (Mesh mesh in cachedMeshes.Values)
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }
            cachedMeshes.Clear();
            DefaultMaterial = null;
        }
    }
}
