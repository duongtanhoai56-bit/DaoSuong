using System;
using Game.Core;

namespace DaoSuong.Crafting
{
    public readonly struct RecipeRequirement
    {
        public StableId ItemId { get; }
        public int Quantity { get; }

        public RecipeRequirement(StableId itemId, int quantity)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            ItemId = itemId;
            Quantity = quantity;
        }
    }
}
