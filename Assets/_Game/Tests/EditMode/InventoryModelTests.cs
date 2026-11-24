using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Gameplay.Tests
{
    public sealed class InventoryModelTests
    {
        private ItemData wood;
        private ItemData stone;
        private ToolData axe;
        private ToolData pickaxe;

        [SetUp]
        public void SetUp()
        {
            wood = ScriptableObject.CreateInstance<ItemData>();
            wood.id = "wood"; wood.displayName = "Gỗ"; wood.maxStack = 10; wood.weight = 0.5f;
            stone = ScriptableObject.CreateInstance<ItemData>();
            stone.id = "stone"; stone.displayName = "Đá"; stone.maxStack = 5; stone.weight = 1f;
            axe = ScriptableObject.CreateInstance<ToolData>();
            axe.id = "axe"; axe.displayName = "Rìu"; axe.maxStack = 1; axe.weight = 2f;
            axe.toolType = ToolType.Axe; axe.tier = 1; axe.power = 2f; axe.maxDurability = 20;
            pickaxe = ScriptableObject.CreateInstance<ToolData>();
            pickaxe.id = "pickaxe"; pickaxe.displayName = "Cuốc"; pickaxe.maxStack = 1; pickaxe.weight = 3f;
            pickaxe.toolType = ToolType.Pickaxe; pickaxe.tier = 1; pickaxe.power = 3f; pickaxe.maxDurability = 30;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(wood);
            Object.DestroyImmediate(stone);
            Object.DestroyImmediate(axe);
            Object.DestroyImmediate(pickaxe);
        }

        [Test]
        public void AddMergesStacksAndUsesNewSlotAfterLimit()
        {
            InventoryModel model = new InventoryModel(4, 100f);
            Assert.AreEqual(0, model.Add(wood, 17));
            Assert.AreEqual(10, model.GetSlot(0).count);
            Assert.AreEqual(7, model.GetSlot(1).count);
            Assert.AreEqual(17, model.Count(wood));
        }

        [Test]
        public void AddReturnsLeftoverWhenSlotCapacityIsFull()
        {
            InventoryModel model = new InventoryModel(1, 100f);
            Assert.AreEqual(0, model.Add(wood, 10));
            Assert.AreEqual(3, model.Add(wood, 3));
            Assert.AreEqual(10, model.Count(wood));
        }

        [Test]
        public void AddRespectsWeightLimitAndReturnsRemainder()
        {
            InventoryModel model = new InventoryModel(4, 2f);
            Assert.AreEqual(6, model.Add(wood, 10));
            Assert.AreEqual(2f, model.TotalWeight, 0.001f);
            Assert.AreEqual(4, model.Count(wood));
        }

        [Test]
        public void AddToolsAsUniqueNonStackableSlots()
        {
            InventoryModel model = new InventoryModel(3, 100f);
            Assert.AreEqual(0, model.Add(axe, 2));
            Assert.AreEqual(1, model.GetSlot(0).count);
            Assert.AreEqual(1, model.GetSlot(1).count);
            Assert.AreNotSame(model.GetSlot(0), model.GetSlot(1));
        }

        [Test]
        public void MoveIntoEmptySlotPreservesStack()
        {
            InventoryModel model = new InventoryModel(3, 100f);
            model.Add(wood, 7);
            Assert.IsTrue(model.TryMove(0, 2));
            Assert.IsNull(model.GetSlot(0));
            Assert.AreEqual(7, model.GetSlot(2).count);
        }

        [Test]
        public void MoveMergesSameItemUpToMaxStack()
        {
            InventoryModel model = new InventoryModel(3, 100f);
            model.Add(wood, 14);
            Assert.IsTrue(model.TryMove(0, 1));
            Assert.AreEqual(10, model.GetSlot(1).count);
            Assert.AreEqual(4, model.GetSlot(0).count);
            Assert.AreEqual(14, model.Count(wood));
        }

        [Test]
        public void MoveSwapsDifferentItems()
        {
            InventoryModel model = new InventoryModel(3, 100f);
            model.Add(wood, 4);
            model.Add(stone, 2);
            Assert.IsTrue(model.TryMove(0, 1));
            Assert.AreSame(stone, model.GetSlot(0).item);
            Assert.AreSame(wood, model.GetSlot(1).item);
        }

        [Test]
        public void SplitCreatesSecondStackWithoutChangingTotal()
        {
            InventoryModel model = new InventoryModel(3, 100f);
            model.Add(wood, 8);
            Assert.IsTrue(model.TrySplit(0, 1, 3));
            Assert.AreEqual(5, model.GetSlot(0).count);
            Assert.AreEqual(3, model.GetSlot(1).count);
            Assert.AreEqual(8, model.Count(wood));
        }

        [Test]
        public void SplitCannotSplitToolsOrEmptySlots()
        {
            InventoryModel model = new InventoryModel(3, 100f);
            model.Add(axe, 1);
            Assert.IsFalse(model.TrySplit(0, 1, 1));
            Assert.IsFalse(model.TrySplit(2, 1, 1));
        }

        [Test]
        public void AddStackNeverInsertsMoreThanSourceStackOwns()
        {
            InventoryModel model = new InventoryModel(3, 100f);
            ItemStack source = new ItemStack(wood, 3);

            Assert.AreEqual(3, model.AddStack(source, 99));
            Assert.AreEqual(3, model.Count(wood));
            Assert.AreEqual(3, source.count);
        }

        [Test]
        public void TransferToChestPreservesDurabilityAndRespectsWeight()
        {
            InventoryModel player = new InventoryModel(4, 100f);
            InventoryModel chest = new InventoryModel(4, 1f);
            player.Add(axe, 1);
            player.GetSlot(0).durability = 7;
            Assert.IsFalse(player.TryTransferTo(chest, 0, 1, out int moved));
            Assert.AreEqual(0, moved);
            Assert.AreEqual(1, player.Count(axe));

            InventoryModel chest2 = new InventoryModel(4, 100f);
            Assert.IsTrue(player.TryTransferTo(chest2, 0, 1, out moved));
            Assert.AreEqual(1, moved);
            Assert.AreEqual(7, chest2.GetSlot(0).durability);
            Assert.AreEqual(0, player.Count(axe));
        }

        [Test]
        public void TransferMovesOnlyAmountThatFits()
        {
            InventoryModel player = new InventoryModel(4, 100f);
            InventoryModel chest = new InventoryModel(1, 2f);
            player.Add(wood, 10);
            Assert.IsTrue(player.TryTransferTo(chest, 0, 10, out int moved));
            Assert.AreEqual(4, moved);
            Assert.AreEqual(6, player.Count(wood));
            Assert.AreEqual(4, chest.Count(wood));
        }

        [Test]
        public void AutoSortMergesStacksAndOrdersByItemId()
        {
            InventoryModel model = new InventoryModel(5, 100f);
            model.Add(stone, 3);
            model.Add(wood, 4);
            model.Add(stone, 2);
            Assert.IsTrue(model.TryAutoSort());
            Assert.AreSame(stone, model.GetSlot(0).item);
            Assert.AreEqual(5, model.GetSlot(0).count);
            Assert.AreSame(wood, model.GetSlot(1).item);
            Assert.AreEqual(4, model.GetSlot(1).count);
        }

        [Test]
        public void RemoveConsumesStacksAcrossSlots()
        {
            InventoryModel model = new InventoryModel(4, 100f);
            model.Add(wood, 18);
            Assert.IsTrue(model.TryRemove(wood, 13));
            Assert.AreEqual(5, model.Count(wood));
            Assert.IsFalse(model.TryRemove(wood, 6));
        }

        [Test]
        public void WeightDoesNotChangeWhenMovingWithinSameInventory()
        {
            InventoryModel model = new InventoryModel(4, 100f);
            model.Add(wood, 6);
            float before = model.TotalWeight;
            Assert.IsTrue(model.TryMove(0, 3));
            Assert.AreEqual(before, model.TotalWeight, 0.001f);
        }


        [Test]
        public void TransferToSpecificSlotMergesOnlyIntoThatSlot()
        {
            InventoryModel source = new InventoryModel(3, 100f);
            InventoryModel destination = new InventoryModel(3, 100f);
            source.Add(wood, 10);
            destination.Add(wood, 9);

            Assert.IsTrue(source.TryTransferToAt(destination, 0, 0, 10, out int moved));
            Assert.AreEqual(1, moved);
            Assert.AreEqual(10, destination.GetSlot(0).count);
            Assert.IsNull(destination.GetSlot(1));
            Assert.AreEqual(9, source.Count(wood));
        }

        [Test]
        public void TransferToSpecificSlotNeverOverwritesDifferentItem()
        {
            InventoryModel source = new InventoryModel(3, 100f);
            InventoryModel destination = new InventoryModel(3, 100f);
            source.Add(wood, 4);
            destination.Add(stone, 2);

            Assert.IsFalse(source.TryTransferToAt(destination, 0, 0, 4, out int moved));
            Assert.AreEqual(0, moved);
            Assert.AreEqual(4, source.Count(wood));
            Assert.AreEqual(2, destination.Count(stone));
        }

        [Test]
        public void ZeroWeightItemsCanStillBeAddedWhenWeightLimitIsZero()
        {
            ItemData quest = ScriptableObject.CreateInstance<ItemData>();
            try
            {
                quest.id = "quest"; quest.maxStack = 5; quest.weight = 0f;
                InventoryModel model = new InventoryModel(2, 0f);
                Assert.AreEqual(0, model.Add(quest, 3));
                Assert.AreEqual(3, model.Count(quest));
            }
            finally { Object.DestroyImmediate(quest); }
        }


        [Test]
        public void TransferToSpecificSlotNeverMovesMoreThanSourceStackOwns()
        {
            InventoryModel source = new InventoryModel(2, 100f);
            InventoryModel destination = new InventoryModel(2, 100f);
            source.Add(wood, 3);

            Assert.IsTrue(source.TryTransferToAt(destination, 0, 1, 99, out int moved));
            Assert.AreEqual(3, moved);
            Assert.AreEqual(0, source.Count(wood));
            Assert.AreEqual(3, destination.Count(wood));
            Assert.AreEqual(3, destination.GetSlot(1).count);
        }

        [Test]
        public void OversizedSpecificSlotTransferOnlyFillsAvailableTargetSpace()
        {
            InventoryModel source = new InventoryModel(2, 100f);
            InventoryModel destination = new InventoryModel(2, 100f);
            source.Add(wood, 3);
            destination.Add(wood, 9);

            Assert.IsTrue(source.TryTransferToAt(destination, 0, 0, 99, out int moved));
            Assert.AreEqual(1, moved);
            Assert.AreEqual(2, source.Count(wood));
            Assert.AreEqual(10, destination.Count(wood));
        }

        [Test]
        public void InvalidItemWeightCannotPoisonInventoryWeightWithNaNOrInfinity()
        {
            InventoryModel model = new InventoryModel(2, 0f);
            wood.weight = float.NaN;
            Assert.AreEqual(2, model.Add(wood, 2));
            Assert.AreEqual(0, model.Count(wood));
            Assert.IsFalse(float.IsNaN(model.TotalWeight));

            wood.weight = float.PositiveInfinity;
            Assert.AreEqual(2, model.Add(wood, 2));
            Assert.AreEqual(0, model.Count(wood));
            Assert.IsFalse(float.IsInfinity(model.TotalWeight));

            wood.weight = -1f;
            Assert.AreEqual(2, model.Add(wood, 2));
            Assert.AreEqual(0, model.Count(wood));
            Assert.AreEqual(0f, model.TotalWeight);
        }

        [Test]
        public void RemoveAtConsumesOnlyRequestedAmountFromSlot()
        {
            InventoryModel model = new InventoryModel(3, 100f);
            model.Add(wood, 7);
            Assert.IsTrue(model.TryRemoveAt(0, 3));
            Assert.AreEqual(4, model.GetSlot(0).count);
            Assert.AreEqual(4, model.Count(wood));
        }

        [Test]
        public void RemoveAtRejectsOversizedOrInvalidToolRemovalWithoutMutation()
        {
            InventoryModel model = new InventoryModel(3, 100f);
            model.Add(wood, 5);
            model.Add(axe, 1);
            Assert.IsFalse(model.TryRemoveAt(0, 9));
            Assert.IsFalse(model.TryRemoveAt(1, 2));
            Assert.AreEqual(5, model.Count(wood));
            Assert.AreEqual(1, model.Count(axe));
        }

        [Test]
        public void PickToolChoosesBestPowerAndSkipsBroken()
        {
            GameObject owner = new GameObject("PlayerInventoryTest");
            try
            {
                PlayerInventory inventory = owner.AddComponent<PlayerInventory>();
                inventory.Add(axe, 1);
                inventory.Add(pickaxe, 1);
                Assert.AreSame(pickaxe, inventory.PickTool(ToolType.Pickaxe, 1).item);
                inventory.GetSlot(1).durability = 0;
                Assert.IsNull(inventory.PickTool(ToolType.Pickaxe, 1));
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}
