using System;
using Random = System.Random;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Gameplay.Tests
{
    public sealed class GatherRulesTests
    {
        [Test]
        public void HandsCanHarvestWhenNoToolIsRequired()
        {
            Assert.IsTrue(GatherRules.CanHarvest(ToolType.None, 1, ToolType.None, 0));
        }

        [Test]
        public void WrongToolTypeCannotHarvest()
        {
            Assert.IsFalse(GatherRules.CanHarvest(ToolType.Axe, 1, ToolType.Pickaxe, 2));
        }

        [Test]
        public void ToolBelowRequiredTierCannotHarvest()
        {
            Assert.IsFalse(GatherRules.CanHarvest(ToolType.Pickaxe, 2, ToolType.Pickaxe, 1));
        }

        [Test]
        public void DamageNeverProducesNegativeHealth()
        {
            Assert.AreEqual(0f, GatherRules.ApplyDamage(1f, 50f));
        }

        [Test]
        public void HitsNeededRoundsUp()
        {
            Assert.AreEqual(4, GatherRules.HitsNeeded(6f, 1.5f));
        }

        [Test]
        public void DurabilityNeverDropsBelowZero()
        {
            Assert.AreEqual(0, GatherRules.WearDurability(2, 5));
            Assert.AreEqual(2, GatherRules.WearDurability(2, -5));
        }

        [Test]
        public void RandomAmountIsInclusiveAtBothEnds()
        {
            Random random = new Random(1234);
            for (int i = 0; i < 100; i++)
                Assert.That(GatherRules.RollAmount(random, 2, 4), Is.InRange(2, 4));
        }

        [Test]
        public void FishingCatchSucceedsOnlyDuringBiteWindow()
        {
            FishingSession session = new FishingSession(new Random(1), 2f, 2f, 1f);
            session.Cast();
            session.Tick(2f);
            Assert.AreEqual(FishingPhase.Bite, session.Phase);
            session.Press();
            Assert.AreEqual(FishingResult.Caught, session.Result);
        }

        [Test]
        public void FishingEscapesAfterBiteWindowExpires()
        {
            FishingSession session = new FishingSession(new Random(1), 2f, 2f, 1f);
            session.Cast();
            session.Tick(2.1f);
            session.Tick(1.1f);
            Assert.AreEqual(FishingResult.Escaped, session.Result);
        }

        [Test]
        public void PressingBeforeBiteCancelsFishing()
        {
            FishingSession session = new FishingSession(new Random(1), 2f, 2f, 1f);
            session.Cast();
            session.Press();
            Assert.AreEqual(FishingResult.Cancelled, session.Result);
        }

        [Test]
        public void FishingTableReturnsOnlyConfiguredItemAndValidAmount()
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            FishingTable table = ScriptableObject.CreateInstance<FishingTable>();
            try
            {
                table.drops = new[] { new FishDrop { item = item, weight = 1f, min = 1, max = 3 } };
                ItemData result = table.Roll(new Random(5), out int amount);
                Assert.AreSame(item, result);
                Assert.That(amount, Is.InRange(1, 3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(table);
                UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void BackpackPicksStrongestUsableToolAndSkipsBrokenTools()
        {
            GameObject owner = new GameObject("BackpackTest");
            ToolData weak = ScriptableObject.CreateInstance<ToolData>();
            ToolData strong = ScriptableObject.CreateInstance<ToolData>();
            try
            {
                weak.toolType = ToolType.Axe;
                weak.tier = 1;
                weak.power = 8f;
                weak.maxDurability = 10;
                strong.toolType = ToolType.Axe;
                strong.tier = 2;
                strong.power = 2f;
                strong.maxDurability = 10;

                PlayerInventory backpack = owner.AddComponent<PlayerInventory>();
                backpack.Add(weak, 1);
                backpack.Add(strong, 1);
                Assert.AreSame(weak, backpack.PickTool(ToolType.Axe, 1).item);

                foreach (ItemStack stack in backpack.Tools)
                    if (stack.item == weak) stack.durability = 0;
                Assert.AreSame(strong, backpack.PickTool(ToolType.Axe, 1).item);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(weak);
                UnityEngine.Object.DestroyImmediate(strong);
            }
        }

        [Test]
        public void NonFiniteDamageInputsNeverProduceNaN()
        {
            Assert.AreEqual(0f, GatherRules.ApplyDamage(float.NaN, 5f));
            Assert.AreEqual(0f, GatherRules.ApplyDamage(float.PositiveInfinity, 5f));
            Assert.AreEqual(9.9f, GatherRules.ApplyDamage(10f, float.NaN), 0.001f);
            Assert.AreEqual(100, GatherRules.HitsNeeded(10f, float.PositiveInfinity));
        }

        [Test]
        public void FishingSessionSanitizesNonFiniteTimerInputs()
        {
            FishingSession session = new FishingSession(
                new System.Random(17), float.NaN, float.PositiveInfinity, float.NaN);
            session.Cast();
            Assert.AreEqual(FishingPhase.Waiting, session.Phase);
            session.Tick(float.NaN);
            Assert.AreEqual(FishingPhase.Waiting, session.Phase);
            session.Tick(100f);
            Assert.AreEqual(FishingPhase.Bite, session.Phase);
        }

        [Test]
        public void FishingTableIgnoresNonFiniteDropWeights()
        {
            ItemData invalidItem = ScriptableObject.CreateInstance<ItemData>();
            ItemData validItem = ScriptableObject.CreateInstance<ItemData>();
            FishingTable table = ScriptableObject.CreateInstance<FishingTable>();
            try
            {
                table.drops = new[]
                {
                    new FishDrop { item = invalidItem, weight = float.PositiveInfinity },
                    new FishDrop { item = invalidItem, weight = float.NaN },
                    new FishDrop { item = validItem, weight = 2f, min = 1, max = 1 }
                };
                ItemData result = table.Roll(new System.Random(23), out int amount);
                Assert.AreSame(validItem, result);
                Assert.AreEqual(1, amount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(table);
                UnityEngine.Object.DestroyImmediate(invalidItem);
                UnityEngine.Object.DestroyImmediate(validItem);
            }
        }

    }
}
