using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Reference-counted-by-owner gate for gameplay controls while modal UI is open.
    /// Multiple UI systems can acquire the gate independently; releasing one owner
    /// never unlocks gameplay while another owner still holds it.
    /// </summary>
    public static class GameplayInputGate
    {
        private static readonly HashSet<object> owners = new HashSet<object>();

        public static bool IsLocked => owners.Count > 0;
        public static int ActiveLockCount => owners.Count;

        public static bool IsLockedByOther(object owner)
        {
            foreach (object activeOwner in owners)
            {
                if (!ReferenceEquals(activeOwner, owner))
                    return true;
            }
            return false;
        }

        public static bool Acquire(object owner)
        {
            if (owner == null)
                return false;
            return owners.Add(owner);
        }

        public static bool Release(object owner)
        {
            if (owner == null)
                return false;
            return owners.Remove(owner);
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            owners.Clear();
        }
    }
}
