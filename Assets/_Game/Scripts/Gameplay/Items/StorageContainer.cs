using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Inventory storage adapter for chests and future shared containers.</summary>
    [DisallowMultipleComponent]
    public sealed class StorageContainer : MonoBehaviour, IItemReceiver
    {
        [SerializeField, Min(1)] private int slotCount = 36;
        [SerializeField, Min(0f)] private float maxWeight = 160f;
        [SerializeField] private string displayName = "Rương";
        [SerializeField] private string persistenceKey;
        [SerializeField] private ItemData[] persistenceItems = Array.Empty<ItemData>();
        [Header("Tùy chọn kiểm thử")]
        [SerializeField] private bool placeNearPlayerSpawn;
        [SerializeField] private IslandGenerator spawnIsland;
        [SerializeField] private Transform spawnPlayer;
        [SerializeField, Min(1f)] private float spawnOffset = 3f;
        private InventoryModel model;
        private bool persistenceLoaded;
        private bool persistenceCleared;

        [Serializable]
        private sealed class StoredStack
        {
            public string itemId;
            public string assetName; // Legacy fallback for an earlier save written before item IDs were used.
            public int count;
            public int durability;
        }

        [Serializable]
        private sealed class StorageSave
        {
            public int version = 2;
            public StoredStack[] stacks = Array.Empty<StoredStack>();
        }

        public event Action Changed;
        public string DisplayName => displayName;
        public InventoryModel Model
        {
            get
            {
                if (model == null)
                {
                    float safeWeight = float.IsNaN(maxWeight) || float.IsInfinity(maxWeight)
                        ? 0f
                        : Mathf.Max(0f, maxWeight);
                    model = new InventoryModel(Mathf.Max(1, slotCount), safeWeight);
                }
                EnsureLoaded();
                return model;
            }
        }
        public float TotalWeight => Model.TotalWeight;
        public float MaxWeight => Model.MaxWeight;

        private void Awake()
        {
            if (GetComponent<Collider>() == null)
                gameObject.AddComponent<BoxCollider>();
        }

        /// <summary>Enables per-chest persistence using stable ScriptableObject asset names.</summary>
        public void ConfigurePersistence(string stableKey, ItemData[] knownItems)
        {
            persistenceKey = string.IsNullOrWhiteSpace(stableKey) ? string.Empty : stableKey.Trim();
            persistenceItems = knownItems ?? Array.Empty<ItemData>();
            persistenceLoaded = false;
            persistenceCleared = false;
            EnsureLoaded();
        }

        private void Start()
        {
            if (!placeNearPlayerSpawn || spawnIsland == null || spawnPlayer == null) return;
            if (spawnIsland.IsGenerated) PlaceNearSpawn();
            else spawnIsland.Generated += OnIslandGenerated;
        }

        private void OnDestroy()
        {
            if (spawnIsland != null) spawnIsland.Generated -= OnIslandGenerated;
        }

        private void OnIslandGenerated() => PlaceNearSpawn();

        private void PlaceNearSpawn()
        {
            if (spawnIsland != null) spawnIsland.Generated -= OnIslandGenerated;
            Vector3 forward = Vector3.ProjectOnPlane(spawnPlayer.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Vector3 point = spawnPlayer.position + forward * spawnOffset;
            float height = spawnIsland.GetHeight(point.x, point.z);
            transform.position = new Vector3(point.x, height + 0.5f, point.z);
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        public int Add(ItemData item, int amount)
        {
            int before = Model.Count(item);
            int left = Model.Add(item, amount);
            if (Model.Count(item) != before) { SavePersistence(); Changed?.Invoke(); }
            return left;
        }

        public bool TryTransferTo(InventoryModel destination, int sourceSlot, int amount, out int moved)
        {
            bool changed = Model.TryTransferTo(destination, sourceSlot, amount, out moved);
            if (changed) { SavePersistence(); Changed?.Invoke(); }
            return changed;
        }

        public bool TryTransferFrom(InventoryModel source, int sourceSlot, int amount, out int moved)
        {
            moved = 0;
            bool changed = source != null && source.TryTransferTo(Model, sourceSlot, amount, out moved);
            if (changed) { SavePersistence(); Changed?.Invoke(); }
            return changed;
        }

        public bool TryTransferFromAt(InventoryModel source, int sourceSlot, int targetSlot, int amount, out int moved)
        {
            moved = 0;
            bool changed = source != null && source.TryTransferToAt(Model, sourceSlot, targetSlot, amount, out moved);
            if (changed) { SavePersistence(); Changed?.Invoke(); }
            return changed;
        }

        public bool AutoSort()
        {
            bool changed = Model.TryAutoSort();
            if (changed) { SavePersistence(); Changed?.Invoke(); }
            return changed;
        }

        public void NotifyChanged() { SavePersistence(); Changed?.Invoke(); }

        /// <summary>Deletes this chest's local save when a new campaign run is started.</summary>
        public void ClearPersistence()
        {
            persistenceCleared = true;
            persistenceLoaded = true;
            if (!string.IsNullOrWhiteSpace(persistenceKey))
            {
                PlayerPrefs.DeleteKey(persistenceKey);
                PlayerPrefs.Save();
            }

            float safeWeight = float.IsNaN(maxWeight) || float.IsInfinity(maxWeight)
                ? 0f
                : Mathf.Max(0f, maxWeight);
            model = new InventoryModel(Mathf.Max(1, slotCount), safeWeight);
            Changed?.Invoke();
        }

        private void EnsureLoaded()
        {
            if (persistenceLoaded || persistenceCleared)
            {
                persistenceLoaded = true;
                return;
            }
            persistenceLoaded = true;
            if (string.IsNullOrWhiteSpace(persistenceKey) || !PlayerPrefs.HasKey(persistenceKey)) return;
            try
            {
                StorageSave save = JsonUtility.FromJson<StorageSave>(PlayerPrefs.GetString(persistenceKey));
                if (save == null || (save.version != 1 && save.version != 2) || save.stacks == null) return;
                for (int i = 0; i < save.stacks.Length; i++)
                {
                    StoredStack record = save.stacks[i];
                    if (record == null || (string.IsNullOrWhiteSpace(record.itemId) && string.IsNullOrWhiteSpace(record.assetName))) continue;
                    ItemData item = FindItem(record.itemId, record.assetName);
                    if (item == null) continue;
                    int amount = Mathf.Clamp(record.count, 1, 9999);
                    ItemStack stack = new ItemStack(item, item is ToolData ? 1 : amount);
                    if (stack.Tool != null) stack.durability = Mathf.Clamp(record.durability, 0, stack.Tool.maxDurability);
                    Model.AddStack(stack, stack.count);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Storage] Save rương không hợp lệ: " + exception.Message, this);
            }
        }

        private ItemData FindItem(string itemId, string legacyAssetName)
        {
            if (persistenceItems == null) return null;
            for (int i = 0; i < persistenceItems.Length; i++)
            {
                ItemData candidate = persistenceItems[i];
                if (candidate == null) continue;
                if (!string.IsNullOrWhiteSpace(itemId) && string.Equals(candidate.id, itemId, StringComparison.Ordinal))
                    return candidate;
                if (string.IsNullOrWhiteSpace(itemId) && !string.IsNullOrWhiteSpace(legacyAssetName) &&
                    string.Equals(candidate.name, legacyAssetName, StringComparison.Ordinal))
                    return candidate;
            }
            return null;
        }

        private void SavePersistence()
        {
            if (persistenceCleared || string.IsNullOrWhiteSpace(persistenceKey)) return;
            List<StoredStack> stacks = new List<StoredStack>();
            InventoryModel inventory = Model;
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack == null || stack.item == null || stack.count <= 0) continue;
                stacks.Add(new StoredStack
                {
                    itemId = stack.item.id,
                    assetName = stack.item.name,
                    count = stack.count,
                    durability = stack.durability
                });
            }
            PlayerPrefs.SetString(persistenceKey, JsonUtility.ToJson(new StorageSave { stacks = stacks.ToArray() }));
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused) { if (paused) SavePersistence(); }
        private void OnApplicationQuit() => SavePersistence();

        private void OnValidate()
        {
            slotCount = Mathf.Max(1, slotCount);
            if (float.IsNaN(maxWeight) || float.IsInfinity(maxWeight)) maxWeight = 0f;
            maxWeight = Mathf.Max(0f, maxWeight);
            if (string.IsNullOrWhiteSpace(displayName)) displayName = "Rương";
        }
    }
}
