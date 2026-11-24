using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Compatibility shim for scenes created during Phase 3. New gameplay must use PlayerInventory.
    /// The Editor migration tool removes this component after wiring PlayerInventory.
    /// </summary>
    [Obsolete("Use PlayerInventory. This component only forwards old scene references during migration.")]
    [DisallowMultipleComponent]
    public sealed class SimpleBackpack : MonoBehaviour, IItemReceiver
    {
        [SerializeField] private PlayerInventory target;
        private PlayerInventory Inventory
        {
            get
            {
                if (target == null) target = GetComponent<PlayerInventory>();
                if (target == null) target = gameObject.AddComponent<PlayerInventory>();
                return target;
            }
        }

        public event Action Changed
        {
            add { Inventory.Changed += value; }
            remove { Inventory.Changed -= value; }
        }

        public IReadOnlyList<ItemStack> Tools => Inventory.Tools;
        public int Add(ItemData item, int amount) => Inventory.Add(item, amount);
        public int Count(ItemData item) => Inventory.Count(item);
        public ItemStack PickTool(ToolType type, int minTier) => Inventory.PickTool(type, minTier);
        public void RemoveBroken() => Inventory.RemoveBroken();
        public void NotifyChanged() => Inventory.NotifyChanged();
        public string Summary() => Inventory.Summary();

        private void Awake()
        {
            if (target == null) target = GetComponent<PlayerInventory>();
            if (target == null) target = gameObject.AddComponent<PlayerInventory>();
        }
    }
}
