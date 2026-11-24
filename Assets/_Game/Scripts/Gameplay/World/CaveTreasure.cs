using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>One-time cave loot or lore fragment. Uses the existing mobile Action interaction.</summary>
    [DisallowMultipleComponent]
    public sealed class CaveTreasure : GatherTarget
    {
        private CaveManager manager;
        private string uniqueId;
        private ItemData item;
        private ItemData requiredKey;
        private bool consumeKey;
        private string secretText;
        private int amount;
        private bool collected;
        private bool keyUnlocked;

        public ItemData Item => item;
        public int Amount => amount;
        public override bool IsAvailable => isActiveAndEnabled && !collected;

        public void Initialize(CaveManager caveManager, string id, ItemData reward, int rewardAmount,
            ItemData key, bool shouldConsumeKey, string story)
        {
            manager = caveManager;
            uniqueId = id;
            item = reward;
            amount = manager != null ? manager.GetRemainingAmount(id, rewardAmount) : Mathf.Clamp(rewardAmount, 0, 999);
            requiredKey = key;
            consumeKey = shouldConsumeKey;
            secretText = story ?? string.Empty;
            keyUnlocked = requiredKey == null || (manager != null && manager.HasUnlocked(uniqueId));
            collected = manager == null || manager.HasCollected(uniqueId);
            if (collected) gameObject.SetActive(false);
        }

        public bool Interact(PlayerInventory inventory)
        {
            if (!IsAvailable || manager == null || inventory == null) return false;
            bool needsKey = requiredKey != null && !keyUnlocked;
            int keySlot = needsKey ? FindItemSlot(inventory, requiredKey) : -1;
            if (!CaveRules.CanOpenLockedTreasure(needsKey, keySlot >= 0))
            {
                HudMessages.Toast("Bị khóa. Cần " + requiredKey.displayName + ".");
                HudMessages.SetPrompt("Kho báu bị phong ấn · cần " + requiredKey.displayName);
                return false;
            }

            if (item != null && amount > 0)
            {
                // Treat reward insertion + key consumption as one transaction. If the
                // key slot changed or removal fails, restore the inventory snapshot so
                // a failed unlock cannot duplicate loot or silently spend the key.
                InventoryModel snapshot = needsKey && consumeKey ? inventory.Model.Clone() : null;
                int requested = amount;
                int left = Mathf.Clamp(inventory.Add(item, requested), 0, requested);
                int accepted = CaveRules.AcceptedAmount(requested, left);
                if (accepted <= 0)
                {
                    HudMessages.Toast("Túi đồ đã đầy. Hãy dọn chỗ trống trước.");
                    return false;
                }
                // Adding loot can fill an earlier empty slot. Re-resolve the key slot
                // after insertion instead of trusting the pre-insertion index.
                int currentKeySlot = needsKey && consumeKey ? FindItemSlot(inventory, requiredKey) : -1;
                if (needsKey && consumeKey &&
                    (currentKeySlot < 0 || !inventory.TryRemoveAt(currentKeySlot, 1)))
                {
                    if (snapshot != null)
                    {
                        inventory.Model.RestoreFrom(snapshot);
                        inventory.NotifyChanged();
                    }
                    Debug.LogWarning("[Cave] Giao dịch mở khóa thất bại; đã hoàn tác túi đồ.", this);
                    HudMessages.Toast("Không thể mở khóa. Túi đồ đã được khôi phục.");
                    return false;
                }
                amount = left;
                if (needsKey)
                {
                    // The item branch has already removed the key (when configured) and
                    // atomically recorded the unlock. Do not execute the key branch again.
                    keyUnlocked = true;
                    needsKey = false;
                }
                manager.RecordTreasureProgress(uniqueId, keyUnlocked, amount, amount <= 0 ? secretText : null);
                HudMessages.Toast("Nhặt được " + accepted + " " + item.displayName + ".");
                if (amount > 0)
                {
                    HudMessages.SetPrompt("Còn lại " + amount + " " + item.displayName + " · chạm TÁC ĐỘNG để nhặt tiếp");
                    return true;
                }
            }

            if (needsKey)
            {
                if (consumeKey && keySlot >= 0 && !inventory.TryRemoveAt(keySlot, 1))
                {
                    HudMessages.Toast("Không thể mở khóa. Hãy thử lại.");
                    return false;
                }
                keyUnlocked = true;
                manager.MarkUnlocked(uniqueId);
            }
            collected = true;
            manager.MarkCollected(uniqueId, secretText);
            if (!string.IsNullOrWhiteSpace(secretText))
                HudMessages.Toast(item == null
                    ? "Đã mở lối bí mật. Ghi chép đã được lưu vào nhật ký."
                    : "Đã ghi vào nhật ký khám phá.");
            gameObject.SetActive(false);
            return true;
        }

        public override void Respawn() { }

        private static int FindItemSlot(PlayerInventory inventory, ItemData wanted)
        {
            if (inventory == null || wanted == null) return -1;
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack != null && stack.item == wanted && stack.count > 0) return i;
            }
            return -1;
        }
    }
}
