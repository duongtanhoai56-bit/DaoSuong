using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Gameplay.Tests
{
    public sealed class CampaignProgressRulesTests
    {
        [Test]
        public void WinningRequiresNinetyNineCompletedDays()
        {
            Assert.IsFalse(CampaignProgressRules.HasWon(98, 99));
            Assert.IsTrue(CampaignProgressRules.HasWon(99, 99));
            Assert.IsTrue(CampaignProgressRules.HasWon(120, 99));
        }

        [Test]
        public void InvalidRequiredDayCountNeverWins()
        {
            Assert.IsFalse(CampaignProgressRules.HasWon(99, 0));
            Assert.IsFalse(CampaignProgressRules.HasWon(99, -1));
        }

        [Test]
        public void AlivePlayerCanCountTheNextCompletedDay()
        {
            Assert.IsTrue(CampaignProgressRules.CanCountSurvivedDay(8, true, 7));
        }

        [Test]
        public void DeadPlayerCannotCountTheDayAsSurvived()
        {
            Assert.IsFalse(CampaignProgressRules.CanCountSurvivedDay(8, false, 7));
        }

        [Test]
        public void SameDayCannotBeCountedTwice()
        {
            Assert.IsFalse(CampaignProgressRules.CanCountSurvivedDay(8, true, 8));
        }

        [Test]
        public void ZeroOrNegativeDayCannotBeCounted()
        {
            Assert.IsFalse(CampaignProgressRules.CanCountSurvivedDay(0, true, 0));
            Assert.IsFalse(CampaignProgressRules.CanCountSurvivedDay(-1, true, 0));
        }

        [Test]
        public void IslandWithoutBossGateUnlocksWhenDayRequirementIsMet()
        {
            Assert.IsTrue(CampaignProgressRules.IsIslandUnlocked(5, 5, null, null));
            Assert.IsTrue(CampaignProgressRules.IsIslandUnlocked(5, 5, "  ", null));
        }

        [Test]
        public void BossGateRemainsLockedUntilRequiredBossIsDefeated()
        {
            HashSet<string> defeated = new HashSet<string>();
            Assert.IsFalse(CampaignProgressRules.IsIslandUnlocked(20, 5, "mire_queen", defeated));
            defeated.Add("mire_queen");
            Assert.IsTrue(CampaignProgressRules.IsIslandUnlocked(20, 5, "mire_queen", defeated));
        }

        [Test]
        public void IslandRemainsLockedBeforeUnlockDayEvenWhenBossIsDefeated()
        {
            HashSet<string> defeated = new HashSet<string> { "salt_warden" };
            Assert.IsFalse(CampaignProgressRules.IsIslandUnlocked(3, 4, "salt_warden", defeated));
        }

        [Test]
        public void MissingBossProgressCollectionDoesNotUnlockBossGatedIsland()
        {
            Assert.IsFalse(CampaignProgressRules.IsIslandUnlocked(99, 1, "ancient_core", null));
        }

        [Test]
        public void AffordabilityRequiresEnoughItemsAndPositiveCost()
        {
            Assert.IsTrue(CampaignProgressRules.CanAfford(10, 10));
            Assert.IsFalse(CampaignProgressRules.CanAfford(9, 10));
            Assert.IsFalse(CampaignProgressRules.CanAfford(10, 0));
        }

        [Test]
        public void PlacementRequiresLandWithinRangeAndNoBlocker()
        {
            Assert.IsTrue(CampaignProgressRules.IsValidPlacement(3f, 4f, true, false));
            Assert.IsFalse(CampaignProgressRules.IsValidPlacement(5f, 4f, true, false));
            Assert.IsFalse(CampaignProgressRules.IsValidPlacement(3f, 4f, false, false));
            Assert.IsFalse(CampaignProgressRules.IsValidPlacement(3f, 4f, true, true));
        }

        [Test]
        public void PlacementRejectsNonFiniteDistance()
        {
            Assert.IsFalse(CampaignProgressRules.IsValidPlacement(float.NaN, 4f, true, false));
            Assert.IsFalse(CampaignProgressRules.IsValidPlacement(float.PositiveInfinity, 4f, true, false));
        }

        [Test]
        public void PlacementRejectsNonFiniteMaximumDistance()
        {
            Assert.IsFalse(CampaignProgressRules.IsValidPlacement(1f, float.NaN, true, false));
            Assert.IsFalse(CampaignProgressRules.IsValidPlacement(1f, float.PositiveInfinity, true, false));
        }
    }
}
