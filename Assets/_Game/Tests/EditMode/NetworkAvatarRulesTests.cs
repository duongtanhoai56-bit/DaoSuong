using Game.Network;
using NUnit.Framework;
using UnityEngine;

public class NetworkAvatarRulesTests
{
    [Test]
    public void RejectsNaNPosition()
    {
        Assert.IsFalse(NetworkAvatarRules.IsFinite(new Vector3(float.NaN, 0f, 0f)));
    }

    [Test]
    public void RejectsInfiniteRotation()
    {
        Assert.IsFalse(NetworkAvatarRules.IsValidRotation(new Quaternion(0f, float.PositiveInfinity, 0f, 1f)));
    }

    [Test]
    public void AcceptsIdentityRotation()
    {
        Assert.IsTrue(NetworkAvatarRules.IsValidRotation(Quaternion.identity));
    }

    [Test]
    public void NormalizeSafeReturnsIdentityForInvalidRotation()
    {
        Assert.AreEqual(Quaternion.identity, NetworkAvatarRules.NormalizeSafe(new Quaternion(0f, 0f, 0f, 0f)));
    }

    [Test]
    public void RejectsPositionOutsideIslandBounds()
    {
        Assert.IsFalse(NetworkAvatarRules.IsWithinWorldBounds(new Vector3(50f, 0f, 0f), 40f, 5f, -15f, 100f));
    }

    [Test]
    public void AcceptsPositionInsideIslandBounds()
    {
        Assert.IsTrue(NetworkAvatarRules.IsWithinWorldBounds(new Vector3(42f, 4f, -20f), 40f, 5f, -15f, 100f));
    }

    [Test]
    public void RejectsNonFiniteWorldBounds()
    {
        Assert.IsFalse(NetworkAvatarRules.IsWithinWorldBounds(Vector3.zero, float.NaN, 5f, -15f, 100f));
    }

    [Test]
    public void FirstPoseInsideSpawnRadiusIsAccepted()
    {
        Assert.IsTrue(NetworkAvatarRules.IsWithinSpawnRadius(
            new Vector3(1f, 1.2f, 1f), new Vector3(0f, 1.2f, 0f), 2f));
    }

    [Test]
    public void FirstPoseOutsideSpawnRadiusIsRejected()
    {
        Assert.IsFalse(NetworkAvatarRules.IsWithinSpawnRadius(
            new Vector3(3f, 1.2f, 0f), new Vector3(0f, 1.2f, 0f), 2f));
    }

    [Test]
    public void SpawnRadiusRejectsNonFiniteInputs()
    {
        Assert.IsFalse(NetworkAvatarRules.IsWithinSpawnRadius(
            new Vector3(float.NaN, 0f, 0f), Vector3.zero, 2f));
    }

    [Test]
    public void SpeedToleranceScalesWithElapsedTime()
    {
        // 0.8m in 50ms is over the configured 12.5m/s + 5cm allowance.
        Assert.IsFalse(NetworkAvatarRules.IsWithinSpeed(Vector3.zero, new Vector3(0.8f, 0f, 0f), 0.05d, 10f, 2.5f, 0.05f));
        Assert.IsTrue(NetworkAvatarRules.IsWithinSpeed(Vector3.zero, new Vector3(0.6f, 0f, 0f), 0.05d, 10f, 2.5f, 0.05f));
    }

    [Test]
    public void RejectsInvalidSpeedInputs()
    {
        Assert.IsFalse(NetworkAvatarRules.IsWithinSpeed(Vector3.zero, Vector3.one, 0d, 10f, 2f, 0.05f));
        Assert.IsFalse(NetworkAvatarRules.IsWithinSpeed(Vector3.zero, Vector3.one, 0.1d, float.NaN, 2f, 0.05f));
    }

    [Test]
    public void SequenceComparisonHandlesNormalOrder()
    {
        Assert.IsTrue(NetworkAvatarRules.IsNewerSequence(11u, 10u));
        Assert.IsFalse(NetworkAvatarRules.IsNewerSequence(10u, 11u));
        Assert.IsFalse(NetworkAvatarRules.IsNewerSequence(11u, 11u));
    }

    [Test]
    public void SequenceComparisonHandlesWrapAround()
    {
        Assert.IsTrue(NetworkAvatarRules.IsNewerSequence(0u, uint.MaxValue));
    }

    [Test]
    public void InitialPoseIsRejectedUntilWorldIsReady()
    {
        Assert.IsFalse(NetworkAvatarRules.IsValidInitialPose(false, Vector3.zero, Vector3.zero, 10f));
    }

    [Test]
    public void InitialPoseIsAcceptedInsideSpawnRadiusWhenWorldIsReady()
    {
        Assert.IsTrue(NetworkAvatarRules.IsValidInitialPose(true, new Vector3(2f, 0f, 0f), Vector3.zero, 10f));
    }

    [Test]
    public void VoiceTransmissionRequiresEveryPrivacyGate()
    {
        Assert.IsTrue(VoiceTransmissionRules.CanTransmit(true, true, false, false, true, false, true));
    }

    [TestCase(false, true, false, false, true, false, true)]
    [TestCase(true, false, false, false, true, false, true)]
    [TestCase(true, true, true, false, true, false, true)]
    [TestCase(true, true, false, true, true, false, true)]
    [TestCase(true, true, false, false, false, false, true)]
    [TestCase(true, true, false, false, true, true, true)]
    [TestCase(true, true, false, false, true, false, false)]
    public void VoiceTransmissionIsDeniedWhenAnyPrivacyGateFails(bool loggedIn, bool inChannel,
        bool leaving, bool muted, bool micGranted, bool background, bool ptt)
    {
        Assert.IsFalse(VoiceTransmissionRules.CanTransmit(loggedIn, inChannel, leaving,
            muted, micGranted, background, ptt));
    }

}
