using Game.Gameplay;
using NUnit.Framework;

namespace Game.Gameplay.Tests
{
    public sealed class SurvivalModelTests
    {
        private SurvivalModel model;

        [SetUp]
        public void SetUp() => model = new SurvivalModel(SurvivalTuning.Default);

        [Test]
        public void StartsAtFullVitalsAndNormalTemperature()
        {
            SurvivalVitals v = model.Vitals;
            Assert.AreEqual(100f, v.Health);
            Assert.AreEqual(100f, v.Hunger);
            Assert.AreEqual(100f, v.Thirst);
            Assert.AreEqual(100f, v.Stamina);
            Assert.AreEqual(37f, v.BodyTemperatureC);
            Assert.IsFalse(v.IsDead);
        }

        [Test]
        public void HungerAndThirstDrainOverTime()
        {
            model.Tick(10f, 24f, 1f, false);
            Assert.Less(model.Hunger, 100f);
            Assert.Less(model.Thirst, 100f);
        }

        [Test]
        public void SprintIncreasesHungerAndThirstDrain()
        {
            SurvivalModel walking = new SurvivalModel(SurvivalTuning.Default);
            SurvivalModel sprinting = new SurvivalModel(SurvivalTuning.Default);
            walking.Tick(10f, 24f, 1f, false);
            sprinting.Tick(10f, 24f, 1f, true);
            Assert.Less(sprinting.Hunger, walking.Hunger);
            Assert.Less(sprinting.Thirst, walking.Thirst);
        }

        [Test]
        public void StarvationCausesHealthDamageOnlyAfterHungerReachesZero()
        {
            SurvivalTuning tuning = new SurvivalTuning(100f, 0f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 35f, 39f);
            SurvivalModel starving = new SurvivalModel(tuning);
            starving.Tick(0.5f, 37f, 0f, false);
            Assert.AreEqual(100f, starving.Health);
            starving.Tick(1f, 37f, 0f, false);
            Assert.Less(starving.Health, 100f);
        }

        [Test]
        public void DehydrationCausesHealthDamage()
        {
            SurvivalTuning tuning = new SurvivalTuning(0f, 200f, 1f, 1f, 0f, 2f, 0f, 0f, 0f, 0f, 0f, 35f, 39f);
            SurvivalModel thirsty = new SurvivalModel(tuning);
            thirsty.Tick(1f, 37f, 0f, false);
            Assert.Less(thirsty.Health, 100f);
        }

        [Test]
        public void StaminaCannotBeSpentBelowZeroAndRegeneratesWhenNotSprinting()
        {
            Assert.IsFalse(model.TrySpendStamina(120f));
            Assert.AreEqual(100f, model.Stamina);
            Assert.IsTrue(model.TrySpendStamina(60f));
            Assert.AreEqual(40f, model.Stamina);
            model.Tick(2f, 37f, 0f, false);
            Assert.Greater(model.Stamina, 40f);
        }

        [Test]
        public void StaminaDoesNotRegenerateWhileSprinting()
        {
            model.TrySpendStamina(30f);
            model.Tick(2f, 37f, 0f, true);
            Assert.AreEqual(70f, model.Stamina);
        }

        [Test]
        public void ExposureMovesBodyTemperatureTowardAmbient()
        {
            model.Tick(2f, 5f, 2f, false);
            Assert.Less(model.BodyTemperatureC, 37f);
            model.Tick(2f, 48f, 2f, false);
            Assert.Greater(model.BodyTemperatureC, 37f);
        }

        [Test]
        public void ColdAndHeatConditionsAreReported()
        {
            SurvivalTuning tuning = new SurvivalTuning(0f, 0f, 1f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 36f, 38f);
            SurvivalModel cold = new SurvivalModel(tuning);
            SurvivalModel hot = new SurvivalModel(tuning);
            cold.Tick(2f, 5f, 2f, false);
            hot.Tick(2f, 48f, 2f, false);
            // Temperature change is gradual; a custom large simulation step is intentionally capped per Tick.
            for (int i = 0; i < 2000; i++) cold.Tick(2f, -5f, 2f, false);
            for (int i = 0; i < 2000; i++) hot.Tick(2f, 50f, 2f, false);
            Assert.IsTrue((cold.Vitals.Conditions & SurvivalCondition.Cold) != 0);
            Assert.IsTrue((hot.Vitals.Conditions & SurvivalCondition.HeatStress) != 0);
        }

        [Test]
        public void BleedingDealsDamageAndStopsWhenDurationExpires()
        {
            SurvivalTuning tuning = new SurvivalTuning(0f, 0f, 1f, 1f, 0f, 0f, 0f, 2f, 0f, 0f, 0f, 35f, 39f);
            SurvivalModel bleeding = new SurvivalModel(tuning);
            Assert.IsTrue(bleeding.ApplyBleeding(1f, 2f));
            bleeding.Tick(0.5f, 37f, 0f, false);
            float afterHalfSecond = bleeding.Health;
            bleeding.Tick(1f, 37f, 0f, false);
            float afterExpired = bleeding.Health;
            bleeding.Tick(2f, 37f, 0f, false);
            Assert.Less(afterHalfSecond, 100f);
            Assert.AreEqual(afterExpired, bleeding.Health);
            Assert.IsFalse((bleeding.Vitals.Conditions & SurvivalCondition.Bleeding) != 0);
        }

        [Test]
        public void SicknessIncreasesThirstDrainAndExpires()
        {
            SurvivalModel healthy = new SurvivalModel(SurvivalTuning.Default);
            SurvivalModel sick = new SurvivalModel(SurvivalTuning.Default);
            sick.ApplySickness(2f, 2f);
            sick.Tick(1f, 37f, 0f, false);
            healthy.Tick(1f, 37f, 0f, false);
            Assert.Less(sick.Thirst, healthy.Thirst);
            sick.Tick(2f, 37f, 0f, false);
            Assert.IsFalse((sick.Vitals.Conditions & SurvivalCondition.Sick) != 0);
        }

        [Test]
        public void ConsumableRestoresVitalsAndAppliesBuff()
        {
            model.ApplyDamage(20f);
            model.Tick(120f, 37f, 0f, false);
            bool applied = model.ApplyConsumable(5f, 20f, 30f, 10f, 0f, 60f, 0.5f, 0.6f, false, false);
            Assert.IsTrue(applied);
            Assert.Greater(model.Health, 80f);
            Assert.Greater(model.Hunger, 90f);
            Assert.Greater(model.Thirst, 90f);
            Assert.IsTrue((model.Vitals.Conditions & SurvivalCondition.WellFed) != 0);
            Assert.IsTrue((model.Vitals.Conditions & SurvivalCondition.Hydrated) != 0);
        }

        [Test]
        public void ConsumableIsRejectedWhenItHasNoUsefulEffect()
        {
            Assert.IsFalse(model.CanApplyConsumable(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false, false));
            Assert.IsFalse(model.ApplyConsumable(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false, false));
        }

        [Test]
        public void BleedingAndSicknessCanBeCuredByConsumable()
        {
            model.ApplyBleeding(30f, 1f);
            model.ApplySickness(30f, 1.8f);
            Assert.IsTrue(model.ApplyConsumable(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, true, true));
            Assert.IsFalse((model.Vitals.Conditions & SurvivalCondition.Bleeding) != 0);
            Assert.IsFalse((model.Vitals.Conditions & SurvivalCondition.Sick) != 0);
        }

        [Test]
        public void DeadPlayerCannotSprintOrRecoverThroughTick()
        {
            model.ApplyDamage(100f);
            model.Tick(10f, 37f, 0f, false);
            Assert.IsTrue(model.IsDead);
            Assert.IsFalse(model.TrySpendStamina(1f));
            Assert.AreEqual(0f, model.Health);
        }

        [Test]
        public void TimedSicknessMultiplierStopsPartWayThroughLongTick()
        {
            SurvivalTuning tuning = new SurvivalTuning(0f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 35f, 39f);
            SurvivalModel sick = new SurvivalModel(tuning);
            sick.ApplySickness(0.5f, 2f);
            sick.Tick(1f, 37f, 0f, false);
            Assert.AreEqual(98.5f, sick.Thirst, 0.001f);
            Assert.IsFalse((sick.Vitals.Conditions & SurvivalCondition.Sick) != 0);
        }



        [Test]
        public void ThirstConsumableRestoresThirstWithoutChangingHunger()
        {
            model.Tick(2f, 37f, 0f, false);
            float hungerBefore = model.Hunger;
            float thirstBefore = model.Thirst;
            Assert.IsTrue(model.ApplyConsumable(0f, 0f, 25f, 0f, 0f, 0f, 1f, 1f, false, false));
            Assert.AreEqual(hungerBefore, model.Hunger, 0.001f);
            Assert.Greater(model.Thirst, thirstBefore);
        }


        [Test]
        public void ConsumableRejectsNegativeRestorationValues()
        {
            Assert.IsFalse(model.CanApplyConsumable(-10f, 20f, 0f, 0f, 0f, 0f, 1f, 1f, false, false));
            Assert.IsFalse(model.ApplyConsumable(0f, -10f, 20f, 0f, 0f, 0f, 1f, 1f, false, false));
        }

        [Test]
        public void WeakerLongBuffDoesNotExtendStrongerActiveBuff()
        {
            SurvivalTuning fastDrain = new SurvivalTuning(1f, 0f, 1f, 1f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f, 35f, 39f);
            model = new SurvivalModel(fastDrain);
            Assert.IsTrue(model.ApplyConsumable(0f, 0f, 0f, 0f, 0f, 5f, 0.5f, 1f, false, false));
            model.Tick(1f, 37f, 0f, false);
            // This consumable has a useful hunger restore, but its weaker buff must not extend x0.5.
            Assert.IsTrue(model.ApplyConsumable(0f, 10f, 0f, 0f, 0f, 30f, 0.8f, 1f, false, false));
            model.Tick(2f, 37f, 0f, false);
            model.Tick(2f, 37f, 0f, false);
            model.Tick(2f, 37f, 0f, false);
            // Four seconds at x0.5, then two seconds at the base drain rate.
            Assert.AreEqual(96f, model.Hunger, 0.001f);
        }

        [Test]
        public void EqualStrengthBuffRefreshesDuration()
        {
            Assert.IsTrue(model.ApplyConsumable(0f, 0f, 0f, 0f, 0f, 2f, 0.5f, 1f, false, false));
            model.Tick(1f, 37f, 0f, false);
            Assert.IsTrue(model.ApplyConsumable(0f, 1f, 0f, 0f, 0f, 5f, 0.5f, 1f, false, false));
            float before = model.Hunger;
            model.Tick(2f, 37f, 0f, false);
            Assert.AreEqual(before - 0.0125f, model.Hunger, 0.001f);
        }

        [Test]
        public void TuningClampsInvalidTemperatureThresholds()
        {
            SurvivalTuning tuning = new SurvivalTuning(0f, 0f, 1f, 1f, 0f, 0f, 0f, 0f,
                0f, 0f, 0f, 1000f, float.NaN);
            Assert.AreEqual(37f, tuning.ColdThresholdC);
            Assert.AreEqual(38.5f, tuning.HeatThresholdC);
        }

        [Test]
        public void InvalidNumbersAreIgnoredOrSanitized()
        {
            model.Tick(float.NaN, float.NaN, float.PositiveInfinity, false);
            model.ApplyDamage(float.NaN);
            Assert.AreEqual(100f, model.Health);
            Assert.AreEqual(100f, model.Hunger);
            Assert.AreEqual(37f, model.BodyTemperatureC);
            Assert.IsFalse(model.ApplyBleeding(float.NaN, 3f));
        }
    }
}
