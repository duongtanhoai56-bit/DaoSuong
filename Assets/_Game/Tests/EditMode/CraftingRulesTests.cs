using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Gameplay.Tests
{
    public sealed class CraftingRulesTests
    {
        private ItemData wood;
        private ItemData stone;
        private ItemData rope;
        private ToolData axe;

        [SetUp]
        public void SetUp()
        {
            wood = MakeItem("wood", 10, 0.5f);
            stone = MakeItem("stone", 10, 1f);
            rope = MakeItem("rope", 20, 0.2f);
            axe = ScriptableObject.CreateInstance<ToolData>();
            axe.id = "axe"; axe.displayName = "Rìu"; axe.maxStack = 1; axe.weight = 2f; axe.maxDurability = 60; axe.power = 2f; axe.toolType = ToolType.Axe;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(wood); Object.DestroyImmediate(stone); Object.DestroyImmediate(rope); Object.DestroyImmediate(axe);
        }

        [Test]
        public void CraftConsumesIngredientsAndAddsOutput()
        {
            InventoryModel inventory = new InventoryModel(8, 100f); inventory.Add(wood, 4);
            RecipeData recipe = MakeRecipe(wood, 3, rope, 2, 0);
            try { Assert.IsTrue(CraftingRules.TryCraft(recipe, inventory, 0, out CraftingFailure failure)); Assert.AreEqual(CraftingFailure.None, failure); Assert.AreEqual(1, inventory.Count(wood)); Assert.AreEqual(2, inventory.Count(rope)); }
            finally { Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void MissingIngredientsDoesNotMutateInventory()
        {
            InventoryModel inventory = new InventoryModel(8, 100f); inventory.Add(wood, 1);
            RecipeData recipe = MakeRecipe(wood, 3, rope, 1, 0);
            try { Assert.IsFalse(CraftingRules.TryCraft(recipe, inventory, 0, out CraftingFailure failure)); Assert.AreEqual(CraftingFailure.MissingIngredients, failure); Assert.AreEqual(1, inventory.Count(wood)); Assert.AreEqual(0, inventory.Count(rope)); }
            finally { Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void RequiresCorrectStationTier()
        {
            InventoryModel inventory = new InventoryModel(8, 100f); inventory.Add(wood, 4);
            RecipeData recipe = MakeRecipe(wood, 2, rope, 1, 1);
            try { Assert.AreEqual(CraftingFailure.StationRequired, CraftingRules.CanCraft(recipe, inventory, 0)); Assert.AreEqual(CraftingFailure.None, CraftingRules.CanCraft(recipe, inventory, 1)); }
            finally { Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void HigherTierStationCanCraftLowerTierRecipe()
        {
            InventoryModel inventory = new InventoryModel(8, 100f); inventory.Add(wood, 4);
            RecipeData recipe = MakeRecipe(wood, 2, rope, 1, 1);
            try { Assert.AreEqual(CraftingFailure.None, CraftingRules.CanCraft(recipe, inventory, 2)); }
            finally { Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void FullInventoryCanCraftWhenIngredientsFreeSlot()
        {
            InventoryModel inventory = new InventoryModel(1, 10f); inventory.Add(wood, 2);
            RecipeData recipe = MakeRecipe(wood, 2, rope, 3, 0);
            try { Assert.AreEqual(CraftingFailure.None, CraftingRules.CanCraft(recipe, inventory, 0)); Assert.IsTrue(CraftingRules.TryCraft(recipe, inventory, 0, out _)); Assert.AreEqual(3, inventory.Count(rope)); }
            finally { Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void RejectsOutputWhenWeightWouldExceedLimit()
        {
            InventoryModel inventory = new InventoryModel(4, 1.1f); inventory.Add(wood, 1);
            RecipeData recipe = MakeRecipe(wood, 1, stone, 2, 0);
            try { Assert.AreEqual(CraftingFailure.InventoryFull, CraftingRules.CanCraft(recipe, inventory, 0)); Assert.AreEqual(1, inventory.Count(wood)); }
            finally { Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void DuplicateIngredientsAreAggregated()
        {
            InventoryModel inventory = new InventoryModel(4, 100f); inventory.Add(wood, 3);
            RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.output = rope; recipe.outputAmount = 1; recipe.requiredStationTier = 0;
            recipe.ingredients = new[] { new RecipeIngredient { item = wood, amount = 2 }, new RecipeIngredient { item = wood, amount = 2 } };
            try { Assert.AreEqual(CraftingFailure.MissingIngredients, CraftingRules.CanCraft(recipe, inventory, 0)); }
            finally { Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void CraftingToolCreatesSeparateDurableStack()
        {
            InventoryModel inventory = new InventoryModel(4, 100f); inventory.Add(wood, 2);
            RecipeData recipe = MakeRecipe(wood, 2, axe, 1, 0);
            try { Assert.IsTrue(CraftingRules.TryCraft(recipe, inventory, 0, out _)); Assert.AreEqual(1, inventory.Count(axe)); Assert.AreEqual(60, inventory.GetSlot(0).durability); }
            finally { Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void InvalidRecipeIsRejected()
        {
            InventoryModel inventory = new InventoryModel(4, 100f);
            RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.output = rope; recipe.outputAmount = 1; recipe.ingredients = new[] { new RecipeIngredient { item = null, amount = 1 } };
            try { Assert.AreEqual(CraftingFailure.InvalidRecipe, CraftingRules.CanCraft(recipe, inventory, 0)); }
            finally { Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void MissingOneOfSeveralIngredientsNeverConsumesAnyInput()
        {
            InventoryModel inventory = new InventoryModel(3, 100f); inventory.Add(wood, 6); inventory.Add(stone, 1);
            InventoryModel snapshot = inventory.Clone();
            RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.output = rope; recipe.outputAmount = 1; recipe.ingredients = new[] { new RecipeIngredient { item = wood, amount = 2 }, new RecipeIngredient { item = stone, amount = 2 } };
            try { Assert.IsFalse(CraftingRules.TryCraft(recipe, inventory, 0, out CraftingFailure failure)); Assert.AreEqual(CraftingFailure.MissingIngredients, failure); Assert.AreEqual(snapshot.Count(wood), inventory.Count(wood)); Assert.AreEqual(snapshot.Count(stone), inventory.Count(stone)); Assert.AreEqual(0, inventory.Count(rope)); }
            finally { Object.DestroyImmediate(recipe); }
        }

        private static ItemData MakeItem(string id, int maxStack, float weight)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>(); item.id = id; item.displayName = id; item.maxStack = maxStack; item.weight = weight; return item;
        }

        private static RecipeData MakeRecipe(ItemData input, int inputAmount, ItemData output, int outputAmount, int stationTier)
        {
            RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>(); recipe.displayName = "Test"; recipe.output = output; recipe.outputAmount = outputAmount; recipe.requiredStationTier = stationTier; recipe.ingredients = new[] { new RecipeIngredient { item = input, amount = inputAmount } }; return recipe;
        }
    }
}
