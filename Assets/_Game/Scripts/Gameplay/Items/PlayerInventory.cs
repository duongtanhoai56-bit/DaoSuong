using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Scene adapter around the pure InventoryModel used by player gameplay.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour, IItemReceiver
    {
        [SerializeField, Min(1)] private int slotCount = 24;
        [SerializeField, Min(0f)] private float maxWeight = 80f;
        private InventoryModel model;

        public event Action Changed;
        public InventoryModel Model
        {
            get
            {
                EnsureModel();
                return model;
            }
        }
        public int SlotCount => Model.SlotCount;
        public float TotalWeight => Model.TotalWeight;
        public float MaxWeight => Model.MaxWeight;
        public IReadOnlyList<ItemStack> Tools => Model.GetToolStacks();

        private void Awake() => EnsureModel();

        public int Add(ItemData item, int amount)
        {
            int before = Model.Count(item);
            int left = Model.Add(item, amount);
            if (Model.Count(item) != before) Changed?.Invoke();
            return left;
        }

        public int Count(ItemData item) => Model.Count(item);
        public ItemStack GetSlot(int index) => Model.GetSlot(index);
        public bool TryMove(int source, int target)
        {
            bool changed = Model.TryMove(source, target);
            if (changed) Changed?.Invoke();
            return changed;
        }

        public bool TryRemoveAt(int slotIndex, int amount)
        {
            bool changed = Model.TryRemoveAt(slotIndex, amount);
            if (changed) Changed?.Invoke();
            return changed;
        }
        public bool TryTransferToAt(InventoryModel destination, int source, int target, int amount, out int moved)
        {
            bool changed = Model.TryTransferToAt(destination, source, target, amount, out moved);
            if (changed) Changed?.Invoke();
            return changed;
        }
        public bool TrySplit(int source, int target, int amount)
        {
            bool changed = Model.TrySplit(source, target, amount);
            if (changed) Changed?.Invoke();
            return changed;
        }
        public bool TryTransferTo(InventoryModel destination, int source, int amount, out int moved)
        {
            bool changed = Model.TryTransferTo(destination, source, amount, out moved);
            if (changed) Changed?.Invoke();
            return changed;
        }
        public bool AutoSort()
        {
            bool changed = Model.TryAutoSort();
            if (changed) Changed?.Invoke();
            return changed;
        }

        /// <summary>Best power wins; tier breaks ties. Broken/incompatible tools are skipped.</summary>
        public ItemStack PickTool(ToolType type, int minimumTier)
        {
            ItemStack best = null;
            for (int i = 0; i < Model.SlotCount; i++)
            {
                ItemStack stack = Model.GetSlot(i);
                ToolData candidate = stack?.Tool;
                if (candidate == null || candidate.toolType != type || candidate.tier < minimumTier || stack.durability <= 0)
                    continue;
                if (best == null || candidate.power > best.Tool.power ||
                    (Mathf.Approximately(candidate.power, best.Tool.power) && candidate.tier > best.Tool.tier))
                    best = stack;
            }
            return best;
        }

        public void RemoveBroken()
        {
            bool removed = false;
            for (int i = Model.SlotCount - 1; i >= 0; i--)
            {
                ItemStack stack = Model.GetSlot(i);
                if (stack?.IsBroken == true)
                {
                    Model.RemoveAt(i);
                    removed = true;
                }
            }
            if (removed) Changed?.Invoke();
        }

        public void NotifyChanged() => Changed?.Invoke();

        public string Summary()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < Model.SlotCount; i++)
            {
                ItemStack stack = Model.GetSlot(i);
                if (stack == null || stack.item == null) continue;
                if (stack.Tool != null)
                    builder.AppendLine($"{stack.item.displayName} {stack.durability}/{stack.Tool.maxDurability}");
                else
                    builder.AppendLine($"{stack.item.displayName}: {stack.count}");
            }
            builder.Append($"Khối lượng: {TotalWeight:0.0}/{MaxWeight:0.0} kg");
            return builder.ToString();
        }

        private void EnsureModel()
        {
            if (model != null) return;
            float safeMaxWeight = float.IsNaN(maxWeight) || float.IsInfinity(maxWeight)
                ? 0f
                : Mathf.Max(0f, maxWeight);
            model = new InventoryModel(Mathf.Max(1, slotCount), safeMaxWeight);
        }

        private void OnValidate()
        {
            slotCount = Mathf.Max(1, slotCount);
            if (float.IsNaN(maxWeight) || float.IsInfinity(maxWeight)) maxWeight = 0f;
            maxWeight = Mathf.Max(0f, maxWeight);
        }
    }
}
