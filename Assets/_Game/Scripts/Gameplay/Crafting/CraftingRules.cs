using System.Collections.Generic;

namespace Game.Gameplay
{
    public enum CraftingFailure
    {
        None, InvalidRecipe, MissingIngredients, StationRequired, InventoryFull
    }

    /// <summary>Pure crafting rules. No MonoBehaviour, scene, time or UI dependency.</summary>
    public static class CraftingRules
    {
        public static CraftingFailure CanCraft(RecipeData recipe, InventoryModel inventory, int stationTier)
        {
            if (recipe == null || inventory == null || recipe.output == null || recipe.outputAmount <= 0 ||
                recipe.ingredients == null || recipe.ingredients.Length == 0)
                return CraftingFailure.InvalidRecipe;

            if (stationTier < recipe.requiredStationTier) return CraftingFailure.StationRequired;

            Dictionary<ItemData, int> costs;
            if (!TryAggregate(recipe, out costs)) return CraftingFailure.InvalidRecipe;
            foreach (KeyValuePair<ItemData, int> cost in costs)
                if (inventory.Count(cost.Key) < cost.Value) return CraftingFailure.MissingIngredients;

            // Simulate input consumption before checking output capacity. This allows a full
            // inventory to craft when consuming ingredients frees the required slots/weight.
            InventoryModel simulation = inventory.Clone();
            foreach (KeyValuePair<ItemData, int> cost in costs)
                if (!simulation.TryRemove(cost.Key, cost.Value)) return CraftingFailure.MissingIngredients;

            return simulation.CanAdd(recipe.output, recipe.outputAmount)
                ? CraftingFailure.None
                : CraftingFailure.InventoryFull;
        }

        public static bool TryCraft(RecipeData recipe, InventoryModel inventory, int stationTier,
            out CraftingFailure failure)
        {
            failure = CanCraft(recipe, inventory, stationTier);
            if (failure != CraftingFailure.None) return false;

            Dictionary<ItemData, int> costs;
            if (!TryAggregate(recipe, out costs))
            {
                failure = CraftingFailure.InvalidRecipe;
                return false;
            }

            InventoryModel snapshot = inventory.Clone();
            foreach (KeyValuePair<ItemData, int> cost in costs)
            {
                if (!inventory.TryRemove(cost.Key, cost.Value))
                {
                    inventory.RestoreFrom(snapshot);
                    failure = CraftingFailure.MissingIngredients;
                    return false;
                }
            }

            int leftover = inventory.Add(recipe.output, recipe.outputAmount);
            if (leftover > 0)
            {
                inventory.RestoreFrom(snapshot);
                failure = CraftingFailure.InventoryFull;
                return false;
            }

            failure = CraftingFailure.None;
            return true;
        }

        private static bool TryAggregate(RecipeData recipe, out Dictionary<ItemData, int> costs)
        {
            costs = new Dictionary<ItemData, int>();
            if (recipe.ingredients == null) return false;
            foreach (RecipeIngredient ingredient in recipe.ingredients)
            {
                if (ingredient == null || ingredient.item == null || ingredient.amount <= 0) return false;
                int current;
                costs.TryGetValue(ingredient.item, out current);
                if (current > int.MaxValue - ingredient.amount) return false;
                costs[ingredient.item] = current + ingredient.amount;
            }
            return costs.Count > 0;
        }
    }
}
