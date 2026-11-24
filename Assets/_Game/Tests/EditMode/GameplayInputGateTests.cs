using Game.Core;
using NUnit.Framework;

namespace Game.Gameplay.Tests
{
    public sealed class GameplayInputGateTests
    {
        [Test]
        public void ReleasingOneModalDoesNotUnlockAnotherModal()
        {
            object inventoryPanel = new object();
            object craftingPanel = new object();
            try
            {
                Assert.IsTrue(GameplayInputGate.Acquire(inventoryPanel));
                Assert.IsTrue(GameplayInputGate.Acquire(craftingPanel));
                Assert.IsTrue(GameplayInputGate.IsLocked);
                Assert.IsTrue(GameplayInputGate.IsLockedByOther(inventoryPanel));
                Assert.IsFalse(GameplayInputGate.IsLockedByOther(craftingPanel));

                Assert.IsTrue(GameplayInputGate.Release(inventoryPanel));
                Assert.IsTrue(GameplayInputGate.IsLocked);
                Assert.IsFalse(GameplayInputGate.IsLockedByOther(craftingPanel));

                Assert.IsTrue(GameplayInputGate.Release(craftingPanel));
                Assert.IsFalse(GameplayInputGate.IsLocked);
            }
            finally
            {
                GameplayInputGate.Release(inventoryPanel);
                GameplayInputGate.Release(craftingPanel);
            }
        }

        [Test]
        public void AcquiringSameOwnerIsIdempotentAndUnknownReleaseIsSafe()
        {
            object panel = new object();
            object unknown = new object();
            try
            {
                Assert.IsTrue(GameplayInputGate.Acquire(panel));
                Assert.IsFalse(GameplayInputGate.Acquire(panel));
                Assert.IsFalse(GameplayInputGate.Release(unknown));
                Assert.IsTrue(GameplayInputGate.IsLocked);
                Assert.IsTrue(GameplayInputGate.Release(panel));
                Assert.IsFalse(GameplayInputGate.IsLocked);
            }
            finally
            {
                GameplayInputGate.Release(panel);
            }
        }
    }
}
