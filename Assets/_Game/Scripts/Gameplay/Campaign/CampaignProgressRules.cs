using System.Collections.Generic;

namespace Game.Gameplay
{
    /// <summary>Pure campaign rules. Kept separate from scene objects so progression can be tested.</summary>
    public static class CampaignProgressRules
    {
        public static bool HasWon(int survivedDays, int requiredDays)
            => requiredDays > 0 && survivedDays >= requiredDays;

        public static bool CanCountSurvivedDay(int dayNumber, bool playerAlive, int lastCountedDay)
            => playerAlive && dayNumber > lastCountedDay && dayNumber > 0;

        public static bool IsIslandUnlocked(int survivedDays, int unlockDay, string requiredBossId,
            ISet<string> defeatedBossIds)
        {
            if (survivedDays < unlockDay) return false;
            if (string.IsNullOrWhiteSpace(requiredBossId)) return true;
            return defeatedBossIds != null && defeatedBossIds.Contains(requiredBossId);
        }

        public static bool CanAfford(int available, int required)
            => required > 0 && available >= required;

        public static bool IsValidPlacement(float distance, float maximumDistance, bool onLand, bool blocked)
        {
            return !float.IsNaN(distance) && !float.IsInfinity(distance) &&
                   !float.IsNaN(maximumDistance) && !float.IsInfinity(maximumDistance) &&
                   distance >= 0f && maximumDistance > 0f && distance <= maximumDistance &&
                   onLand && !blocked;
        }
    }
}
