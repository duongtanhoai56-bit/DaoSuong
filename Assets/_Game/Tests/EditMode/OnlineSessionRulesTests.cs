using Game.Core;
using NUnit.Framework;

public class OnlineSessionRulesTests
{
    [TestCase(" ab12cd ", "AB12CD")]
    [TestCase("ab-12-cd", "AB12CD")]
    [TestCase("ab\t12\ncd", "AB12CD")]
    [TestCase(" 9xyz ", "9XYZ")]
    public void JoinCodeNormalizesSpacesAndHyphens(string input, string expected)
    {
        Assert.IsTrue(OnlineSessionRules.TryNormalizeJoinCode(input, out string normalized));
        Assert.AreEqual(expected, normalized);
    }

    [TestCase(1, 2)]
    [TestCase(4, 4)]
    [TestCase(8, 4)]
    public void PlayerCountClampsToFirstReleaseRange(int input, int expected)
    {
        Assert.AreEqual(expected, OnlineSessionRules.ClampPlayerCount(input));
    }

    [TestCase("development", "development")]
    [TestCase("Production_01", "production_01")]
    [TestCase("bad environment!", "development")]
    [TestCase("", "development")]
    public void EnvironmentNameIsNormalizedAndRestricted(string input, string expected)
    {
        Assert.AreEqual(expected, OnlineSessionRules.NormalizeEnvironment(input));
    }

    [Test]
    public void DisplayNameFallsBackWhenEmptyOrControlOnly()
    {
        Assert.AreEqual("NguoiChoi", OnlineSessionRules.NormalizeDisplayName("  "));
        Assert.AreEqual("NguoiChoi", OnlineSessionRules.NormalizeDisplayName("\n\t"));
    }

    [Test]
    public void DisplayNameTrimsAndLimitsLength()
    {
        string value = OnlineSessionRules.NormalizeDisplayName("  NgườiChơi12345678901234567890  ");
        Assert.AreEqual(24, value.Length);
        Assert.AreEqual(value.Trim(), value);
    }

    [Test]
    public void VoiceChannelNameRemovesUnsafeCharacters()
    {
        Assert.AreEqual("island-session1234", OnlineSessionRules.BuildVoiceChannelName("session-12#34"));
    }

    [Test]
    public void VoiceChannelNameRejectsTooShortSanitizedIds()
    {
        Assert.Throws<System.ArgumentException>(() => OnlineSessionRules.BuildVoiceChannelName("---"));
    }

    [Test]
    public void VoiceChannelNameRejectsTooLongSanitizedIds()
    {
        Assert.Throws<System.ArgumentException>(() => OnlineSessionRules.BuildVoiceChannelName(new string('a', 49)));
    }

    [Test]
    public void VoiceChannelNameIsSafeAndDeterministic()
    {
        string first = OnlineSessionRules.BuildVoiceChannelName("SESSION-ABC-1234");
        string second = OnlineSessionRules.BuildVoiceChannelName("SESSION-ABC-1234");
        Assert.AreEqual(first, second);
        Assert.AreEqual("island-sessionabc1234", first);
    }

    [TestCase("")]
    [TestCase("***")]
    [TestCase("A")]
    [TestCase("THISCODEISWAYTOOLONG")]
    public void InvalidJoinCodeIsRejected(string input)
    {
        Assert.IsFalse(OnlineSessionRules.TryNormalizeJoinCode(input, out _));
    }
}
