using System;

namespace Game.Gameplay
{
    /// <summary>Runtime stack. Tool stacks always contain one item and track durability.</summary>
    [Serializable]
    public sealed class ItemStack
    {
        public ItemData item;
        public int count;
        public int durability;

        public ItemStack(ItemData item, int count)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            this.item = item;
            this.count = item is ToolData ? 1 : count;
            ToolData tool = item as ToolData;
            durability = tool != null ? tool.maxDurability : 0;
        }

        public ToolData Tool => item as ToolData;
        public bool IsBroken => Tool != null && durability <= 0;
    }
}
