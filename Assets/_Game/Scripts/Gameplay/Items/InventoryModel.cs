using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Gameplay
{
    /// <summary>
    /// Slot-based inventory rules. This class has no scene or MonoBehaviour dependency.
    /// ItemData remains a ScriptableObject asset reference, while stack state is runtime data.
    /// </summary>
    public sealed class InventoryModel
    {
        private readonly ItemStack[] slots;

        public int SlotCount => slots.Length;
        public float MaxWeight { get; }
        public float TotalWeight
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < slots.Length; i++)
                {
                    ItemStack stack = slots[i];
                    if (stack?.item != null)
                        total += SafeWeight(stack.item) * stack.count;
                }
                return total;
            }
        }
        public float RemainingWeight => Math.Max(0f, MaxWeight - TotalWeight);

        public InventoryModel(int slotCount, float maxWeight)
        {
            if (slotCount < 1) throw new ArgumentOutOfRangeException(nameof(slotCount));
            if (maxWeight < 0f || float.IsNaN(maxWeight) || float.IsInfinity(maxWeight))
                throw new ArgumentOutOfRangeException(nameof(maxWeight));
            slots = new ItemStack[slotCount];
            MaxWeight = maxWeight;
        }

        public ItemStack GetSlot(int index)
        {
            ValidateIndex(index);
            return slots[index];
        }

        /// <summary>Creates an independent snapshot for transactional gameplay operations.</summary>
        public InventoryModel Clone()
        {
            InventoryModel copy = new InventoryModel(slots.Length, MaxWeight);
            for (int i = 0; i < slots.Length; i++)
                copy.slots[i] = slots[i] == null ? null : Clone(slots[i], slots[i].count);
            return copy;
        }

        /// <summary>Restores a snapshot after a failed multi-step operation.</summary>
        public void RestoreFrom(InventoryModel snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.SlotCount != SlotCount || Math.Abs(snapshot.MaxWeight - MaxWeight) > 0.0001f)
                throw new ArgumentException("Snapshot inventory layout does not match.", nameof(snapshot));
            for (int i = 0; i < slots.Length; i++)
                slots[i] = snapshot.slots[i] == null ? null : Clone(snapshot.slots[i], snapshot.slots[i].count);
        }

        /// <summary>Returns true only when the complete amount can be inserted without mutation.</summary>
        public bool CanAdd(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;
            InventoryModel simulation = Clone();
            return simulation.Add(item, amount) == 0;
        }

        public int Count(ItemData item)
        {
            if (item == null) return 0;
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i]?.item == item) count += slots[i].count;
            return count;
        }

        /// <summary>Returns the amount that could not be inserted.</summary>
        public int Add(ItemData item, int amount)
        {
            if (amount <= 0) return Math.Max(0, amount);
            if (item == null) return amount;

            if (item is ToolData)
            {
                int remaining = amount;
                ItemStack prototype = new ItemStack(item, 1);
                while (remaining > 0)
                {
                    int accepted = AddStack(prototype, 1);
                    if (accepted != 0) break;
                    remaining--;
                }
                return remaining;
            }

            // The temporary source stack carries the full request. AddStack clamps the
            // requested count to source.count, while stored stacks are still split by maxStack.
            ItemStack stack = new ItemStack(item, amount);
            int acceptedCount = AddStack(stack, amount);
            return amount - acceptedCount;
        }

        /// <summary>Accepts a stack while preserving tool durability. Returns inserted count.</summary>
        public int AddStack(ItemStack source, int amount)
        {
            if (source == null || source.item == null || amount <= 0) return 0;
            ItemData item = source.item;
            bool isTool = item is ToolData;
            int requested = Math.Min(amount, source.count);
            if (isTool) requested = Math.Min(1, requested);
            if (requested <= 0) return 0;
            int byWeight = LimitByWeight(item, requested);
            if (byWeight <= 0) return 0;

            int remaining = byWeight;
            if (!isTool)
            {
                int maxStack = Math.Max(1, item.maxStack);
                for (int i = 0; i < slots.Length && remaining > 0; i++)
                {
                    ItemStack current = slots[i];
                    if (current == null || current.item != item || current.Tool != null) continue;
                    int free = maxStack - current.count;
                    if (free <= 0) continue;
                    int move = Math.Min(free, remaining);
                    current.count += move;
                    remaining -= move;
                }
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i] != null) continue;
                int move = isTool ? 1 : Math.Min(Math.Max(1, item.maxStack), remaining);
                ItemStack created = new ItemStack(item, move);
                if (isTool) created.durability = source.durability;
                slots[i] = created;
                remaining -= move;
            }
            return byWeight - remaining;
        }

        public bool RemoveAt(int index)
        {
            ValidateIndex(index);
            if (slots[index] == null) return false;
            slots[index] = null;
            return true;
        }

        /// <summary>Removes an exact amount from a slot without disturbing other stacks.</summary>
        public bool TryRemoveAt(int index, int amount)
        {
            ValidateIndex(index);
            if (amount <= 0) return false;
            ItemStack stack = slots[index];
            if (stack == null || amount > stack.count) return false;
            if (stack.Tool != null && amount != 1) return false;
            stack.count -= amount;
            if (stack.count <= 0) slots[index] = null;
            return true;
        }

        public bool TryRemove(ItemData item, int amount)
        {
            if (item == null || amount <= 0 || Count(item) < amount) return false;
            int remaining = amount;
            for (int i = slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                ItemStack current = slots[i];
                if (current == null || current.item != item) continue;
                int take = Math.Min(current.count, remaining);
                current.count -= take;
                remaining -= take;
                if (current.count <= 0) slots[i] = null;
            }
            return remaining == 0;
        }

        /// <summary>Moves a full stack, merges compatible stacks, or swaps unlike stacks.</summary>
        public bool TryMove(int sourceIndex, int targetIndex)
        {
            ValidateIndex(sourceIndex);
            ValidateIndex(targetIndex);
            if (sourceIndex == targetIndex) return false;
            ItemStack source = slots[sourceIndex];
            if (source == null) return false;

            ItemStack target = slots[targetIndex];
            if (target == null)
            {
                slots[targetIndex] = source;
                slots[sourceIndex] = null;
                return true;
            }

            if (source.item == target.item && source.Tool == null && target.Tool == null)
            {
                int free = Math.Max(0, Math.Max(1, target.item.maxStack) - target.count);
                if (free <= 0) return false;
                int moved = Math.Min(free, source.count);
                target.count += moved;
                source.count -= moved;
                if (source.count <= 0) slots[sourceIndex] = null;
                return moved > 0;
            }

            slots[sourceIndex] = target;
            slots[targetIndex] = source;
            return true;
        }

        public bool TrySplit(int sourceIndex, int targetIndex, int amount)
        {
            ValidateIndex(sourceIndex);
            ValidateIndex(targetIndex);
            if (sourceIndex == targetIndex || amount <= 0 || slots[targetIndex] != null) return false;
            ItemStack source = slots[sourceIndex];
            if (source == null || source.Tool != null || amount >= source.count) return false;
            slots[targetIndex] = Clone(source, amount);
            source.count -= amount;
            return true;
        }

        /// <summary>Transfers up to amount into another inventory, preserving tool durability.</summary>
        public bool TryTransferTo(InventoryModel destination, int sourceIndex, int amount, out int moved)
        {
            moved = 0;
            if (destination == null || ReferenceEquals(destination, this) || amount <= 0) return false;
            ValidateIndex(sourceIndex);
            ItemStack source = slots[sourceIndex];
            if (source == null) return false;

            int request = Math.Min(source.count, source.Tool != null ? 1 : amount);
            moved = destination.AddStack(source, request);
            if (moved <= 0) return false;

            source.count -= moved;
            if (source.count <= 0) slots[sourceIndex] = null;
            return true;
        }

        /// <summary>Transfers to a specific target slot. A different occupied item is never overwritten.</summary>
        public bool TryTransferToAt(InventoryModel destination, int sourceIndex, int targetIndex,
            int amount, out int moved)
        {
            moved = 0;
            if (destination == null || ReferenceEquals(destination, this) || amount <= 0) return false;
            ValidateIndex(sourceIndex);
            destination.ValidateIndex(targetIndex);
            ItemStack source = slots[sourceIndex];
            if (source == null) return false;

            // Never transfer more than the source stack owns. Without this clamp an
            // oversized drag request could create items in the destination and then
            // delete the smaller source stack, duplicating the difference.
            int requested = Math.Min(amount, source.count);
            moved = destination.AddStackAt(source, requested, targetIndex);
            if (moved <= 0) return false;
            source.count -= moved;
            if (source.count <= 0) slots[sourceIndex] = null;
            return true;
        }

        public bool TryAutoSort()
        {
            List<ItemStack> entries = slots.Where(s => s != null).ToList();
            if (entries.Count == 0) return false;

            entries = entries.OrderBy(s => s.Tool != null ? 0 : 1)
                .ThenBy(s => s.item.id ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(s => s.durability)
                .ToList();

            Array.Clear(slots, 0, slots.Length);
            for (int i = 0; i < entries.Count; i++)
                InsertWithoutWeightLimit(entries[i]);
            return true;
        }

        public IReadOnlyList<ItemStack> GetToolStacks()
        {
            List<ItemStack> result = new List<ItemStack>();
            for (int i = 0; i < slots.Length; i++)
                if (slots[i]?.Tool != null) result.Add(slots[i]);
            return result;
        }

        private int AddStackAt(ItemStack source, int amount, int targetIndex)
        {
            ItemStack target = slots[targetIndex];
            if (target != null && (target.item != source.item || target.Tool != null || source.Tool != null))
                return 0;
            int limit = source.Tool != null ? 1 : Math.Min(amount, Math.Max(1, source.item.maxStack));
            limit = LimitByWeight(source.item, limit);
            if (limit <= 0) return 0;
            if (target == null)
            {
                slots[targetIndex] = Clone(source, limit);
                return limit;
            }
            int free = Math.Max(0, Math.Max(1, target.item.maxStack) - target.count);
            int moved = Math.Min(free, limit);
            target.count += moved;
            return moved;
        }

        private int LimitByWeight(ItemData item, int requested)
        {
            float weight = SafeWeight(item);
            if (weight <= 0f) return requested;
            float available = RemainingWeight;
            if (available <= 0f) return 0;
            double byWeight = Math.Floor((available + 0.00001d) / weight);
            if (byWeight <= 0d) return 0;
            return Math.Min(requested, byWeight >= int.MaxValue ? int.MaxValue : (int)byWeight);
        }

        private static float SafeWeight(ItemData item)
        {
            if (item == null) return 0f;
            float weight = item.weight;
            if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0f)
                return float.MaxValue;
            return weight;
        }

        private void InsertWithoutWeightLimit(ItemStack source)
        {
            if (source.Tool == null)
            {
                int remaining = source.count;
                for (int i = 0; i < slots.Length && remaining > 0; i++)
                {
                    if (slots[i] == null || slots[i].item != source.item || slots[i].Tool != null) continue;
                    int free = Math.Max(0, source.item.maxStack - slots[i].count);
                    int add = Math.Min(free, remaining);
                    slots[i].count += add;
                    remaining -= add;
                }
                while (remaining > 0)
                {
                    int empty = FirstEmpty();
                    if (empty < 0) break;
                    int count = Math.Min(Math.Max(1, source.item.maxStack), remaining);
                    slots[empty] = Clone(source, count);
                    remaining -= count;
                }
            }
            else
            {
                int empty = FirstEmpty();
                if (empty >= 0) slots[empty] = Clone(source, 1);
            }
        }

        private int FirstEmpty()
        {
            for (int i = 0; i < slots.Length; i++) if (slots[i] == null) return i;
            return -1;
        }

        private static ItemStack Clone(ItemStack source, int count)
        {
            ItemStack clone = new ItemStack(source.item, count);
            clone.durability = source.durability;
            return clone;
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= slots.Length) throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
