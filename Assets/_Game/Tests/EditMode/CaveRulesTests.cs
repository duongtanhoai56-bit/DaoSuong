using Game.Gameplay;
using NUnit.Framework;

public class CaveRulesTests
{
    [TestCase(-10, CaveRules.MinimumRooms)]
    [TestCase(0, CaveRules.MinimumRooms)]
    [TestCase(3, 3)]
    [TestCase(5, CaveRules.MaximumRooms)]
    [TestCase(99, CaveRules.MaximumRooms)]
    public void RoomCountAlwaysStaysWithinSafeBounds(int requested, int expected)
    {
        Assert.AreEqual(expected, CaveRules.ClampRoomCount(requested));
    }

    [Test]
    public void LootWithZeroChanceNeverSpawns()
    {
        Assert.IsFalse(CaveRules.ShouldSpawnLoot(0f, 0f));
        Assert.IsFalse(CaveRules.ShouldSpawnLoot(0.5f, 0f));
    }

    [Test]
    public void LootWithFullChanceAlwaysSpawnsForValidRoll()
    {
        Assert.IsTrue(CaveRules.ShouldSpawnLoot(0f, 1f));
        Assert.IsTrue(CaveRules.ShouldSpawnLoot(0.9999f, 1f));
    }

    [Test]
    public void LootChanceClampsToUnitInterval()
    {
        Assert.IsTrue(CaveRules.ShouldSpawnLoot(0.4f, 2f));
        Assert.IsFalse(CaveRules.ShouldSpawnLoot(0.4f, -2f));
    }

    [Test]
    public void NonFiniteLootInputsNeverSpawn()
    {
        Assert.IsFalse(CaveRules.ShouldSpawnLoot(float.NaN, 1f));
        Assert.IsFalse(CaveRules.ShouldSpawnLoot(float.PositiveInfinity, 1f));
        Assert.IsFalse(CaveRules.ShouldSpawnLoot(0.1f, float.NaN));
        Assert.IsFalse(CaveRules.ShouldSpawnLoot(0.1f, float.NegativeInfinity));
    }

    [TestCase(true, false, false, true)]
    [TestCase(false, false, false, false)]
    [TestCase(true, true, false, false)]
    [TestCase(true, false, true, false)]
    public void CaveEntryRequiresDefinitionAndAvailableCampaign(bool hasDefinition, bool inside, bool finished, bool expected)
    {
        Assert.AreEqual(expected, CaveRules.CanEnter(hasDefinition, inside, finished));
    }

    [TestCase(false, false, true)]
    [TestCase(false, true, true)]
    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    public void LockedTreasureRequiresItsKey(bool requiresKey, bool hasKey, bool expected)
    {
        Assert.AreEqual(expected, CaveRules.CanOpenLockedTreasure(requiresKey, hasKey));
    }

    [TestCase(10, 0, 10)]
    [TestCase(10, 4, 6)]
    [TestCase(10, 10, 0)]
    [TestCase(10, 99, 0)]
    [TestCase(-4, 0, 0)]
    public void AcceptedLootAmountNeverExceedsRequest(int requested, int unaccepted, int expected)
    {
        Assert.AreEqual(expected, CaveRules.AcceptedAmount(requested, unaccepted));
    }

    [TestCase(true, 0, false, false)]
    [TestCase(true, -10, false, false)]
    [TestCase(true, 1, false, true)]
    [TestCase(false, 0, true, true)]
    [TestCase(false, 5, false, false)]
    public void TreasureOnlySpawnsForARewardOrLore(bool hasItem, int amount, bool hasLore, bool expected)
    {
        Assert.AreEqual(expected, CaveRules.ShouldCreateTreasure(hasItem, amount, hasLore));
    }

    [TestCase(-1, 0)]
    [TestCase(0, 0)]
    [TestCase(25, 25)]
    [TestCase(5000, 999)]
    public void SavedPartialLootIsBounded(int stored, int expected)
    {
        Assert.AreEqual(expected, CaveRules.ClampSavedRemainingAmount(stored));
    }

    [Test]
    public void StableIdsRejectBlankWhitespaceAndOverlongValues()
    {
        Assert.IsFalse(CaveRules.IsStableIdValid(null));
        Assert.IsFalse(CaveRules.IsStableIdValid("   "));
        Assert.IsFalse(CaveRules.IsStableIdValid(" id"));
        Assert.IsFalse(CaveRules.IsStableIdValid(new string('x', 81)));
    }

    [Test]
    public void StableIdsAcceptTrimmedDeterministicIdentifiers()
    {
        Assert.IsTrue(CaveRules.IsStableIdValid("tidal_vault/hidden_cache"));
        Assert.IsTrue(CaveRules.IsStableIdValid("echo_sanctum/relic_01"));
    }
    [TestCase(true, -120f, false)]
    [TestCase(false, -120f, true)]
    [TestCase(false, -10f, false)]
    [TestCase(false, float.NaN, false)]
    [TestCase(false, float.NegativeInfinity, false)]
    public void SurfaceFallRecoveryNeverTeleportsPlayersOutOfCaves(bool insideCave, float y, bool expected)
    {
        Assert.AreEqual(expected, CaveRules.ShouldRecoverFromSurfaceFall(insideCave, y, -25f));
    }

}
