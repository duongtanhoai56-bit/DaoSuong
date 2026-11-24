using Game.Gameplay;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Game.Gameplay.Tests
{
    public sealed class SurvivalControllerTests
    {
        private GameObject playerObject;
        private PlayerInventory inventory;
        private SurvivalController survival;
        private ConsumableData food;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("SurvivalControllerTestPlayer");
            inventory = playerObject.AddComponent<PlayerInventory>();
            survival = playerObject.AddComponent<SurvivalController>();
            food = ScriptableObject.CreateInstance<ConsumableData>();
            food.id = "test_food";
            food.displayName = "Test Food";
            food.maxStack = 10;
            food.weight = 0.1f;
            food.healthRestore = 5f;
            food.hungerRestore = 12f;
            food.hungerDrainMultiplier = 1f;
            food.thirstDrainMultiplier = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObject != null) Object.DestroyImmediate(playerObject);
            if (food != null) Object.DestroyImmediate(food);
        }

        [Test]
        public void UseSelectedConsumableConsumesExactlyOneAndRestoresVitals()
        {
            survival.Model.ApplyDamage(20f);
            Assert.AreEqual(0, inventory.Add(food, 3));

            Assert.IsTrue(survival.TryConsume(inventory, 0));
            Assert.AreEqual(2, inventory.Count(food));
            Assert.AreEqual(85f, survival.Vitals.Health, 0.001f);
            Assert.AreEqual(100f, survival.Vitals.Hunger, 0.001f);
        }


        [Test]
        public void InvalidConsumableSlotReturnsFalseWithoutThrowing()
        {
            Assert.IsFalse(survival.TryConsume(inventory, -1));
            Assert.IsFalse(survival.TryConsume(inventory, inventory.SlotCount));
        }

        [Test]
        public void DeathEventIsRaisedExactlyOnce()
        {
            int deathEvents = 0;
            survival.Died += () => deathEvents++;

            survival.ApplyDamage(100f);
            survival.ApplyDamage(10f);

            Assert.IsTrue(survival.IsDead);
            Assert.AreEqual(1, deathEvents);
        }

        [Test]
        public void SprintCanBeConfiguredWithZeroStaminaCost()
        {
            SurvivalSettings settings = ScriptableObject.CreateInstance<SurvivalSettings>();
            settings.sprintStaminaPerSecond = 0f;
            typeof(SurvivalController).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(survival, settings);
            Assert.IsTrue(survival.TrySpendSprintStamina(0.1f));
            Assert.AreEqual(100f, survival.Vitals.Stamina, 0.001f);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void SprintStaminaCostUsesActualFrameDeltaUpToSafetyCap()
        {
            SurvivalSettings settings = ScriptableObject.CreateInstance<SurvivalSettings>();
            settings.sprintStaminaPerSecond = 10f;
            typeof(SurvivalController).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(survival, settings);
            Assert.IsTrue(survival.TrySpendSprintStamina(0.5f));
            Assert.AreEqual(95f, survival.Vitals.Stamina, 0.001f);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void UselessConsumableIsNotRemoved()
        {
            food.healthRestore = 0f;
            food.hungerRestore = 0f;
            food.thirstRestore = 0f;
            food.staminaRestore = 0f;
            food.buffDurationSeconds = 0f;
            Assert.AreEqual(0, inventory.Add(food, 1));

            Assert.IsFalse(survival.TryConsume(inventory, 0));
            Assert.AreEqual(1, inventory.Count(food));
        }
    }
}
