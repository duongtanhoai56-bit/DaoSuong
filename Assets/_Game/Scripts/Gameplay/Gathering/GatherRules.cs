using System;

namespace Game.Gameplay
{
    /// <summary>Pure gameplay rules: no MonoBehaviour, scene state, or wall-clock dependency.</summary>
    public static class GatherRules
    {
        public static bool CanHarvest(ToolType required, int minimumTier, ToolType held, int heldTier)
        {
            return required == ToolType.None || (held == required && heldTier >= minimumTier);
        }

        public static float ApplyDamage(float health, float power)
        {
            if (!IsFinite(health) || health <= 0f) return 0f;
            float safePower = IsFinite(power) ? Math.Max(0.1f, power) : 0.1f;
            return Math.Max(0f, health - safePower);
        }

        public static int HitsNeeded(float health, float power)
        {
            if (!IsFinite(health) || health <= 0f) return 0;
            float safePower = IsFinite(power) ? Math.Max(0.1f, power) : 0.1f;
            double hits = Math.Ceiling((double)health / safePower);
            return hits >= int.MaxValue ? int.MaxValue : (int)hits;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static int WearDurability(int durability, int cost)
        {
            int safeDurability = Math.Max(0, durability);
            int safeCost = Math.Max(0, cost);
            return safeCost >= safeDurability ? 0 : safeDurability - safeCost;
        }

        public static int RollAmount(Random random, int minimum, int maximum)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            minimum = Math.Max(0, minimum);
            maximum = Math.Max(minimum, maximum);
            if (maximum == int.MaxValue)
                return minimum + (int)(random.NextDouble() * (maximum - (double)minimum + 1d));
            return random.Next(minimum, maximum + 1);
        }
    }

    public enum FishingPhase { Idle, Waiting, Bite, Done }
    public enum FishingResult { None, Caught, Escaped, Cancelled }

    /// <summary>Deterministic timer state machine. The caller supplies dt and owns the RNG.</summary>
    public sealed class FishingSession
    {
        private readonly Random random;
        private readonly float minimumWait;
        private readonly float maximumWait;
        private readonly float biteWindow;
        private float timer;

        public FishingPhase Phase { get; private set; } = FishingPhase.Idle;
        public FishingResult Result { get; private set; } = FishingResult.None;
        public float BiteFraction => Phase == FishingPhase.Bite ? Clamp01(timer / biteWindow) : 0f;

        public FishingSession(Random random, float minimumWait = 3f, float maximumWait = 8f, float biteWindow = 1.3f)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.minimumWait = IsFinite(minimumWait) ? Math.Max(0f, minimumWait) : 3f;
            float requestedMaximum = IsFinite(maximumWait) ? maximumWait : 8f;
            this.maximumWait = Math.Max(this.minimumWait, requestedMaximum);
            this.biteWindow = IsFinite(biteWindow) ? Math.Max(0.1f, biteWindow) : 1.3f;
        }

        public void Cast()
        {
            if (Phase == FishingPhase.Waiting || Phase == FishingPhase.Bite) return;
            timer = minimumWait + (maximumWait - minimumWait) * (float)random.NextDouble();
            Phase = FishingPhase.Waiting;
            Result = FishingResult.None;
        }

        public void Tick(float deltaTime)
        {
            float dt = IsFinite(deltaTime) ? Math.Max(0f, deltaTime) : 0f;
            if (Phase == FishingPhase.Waiting)
            {
                timer -= dt;
                if (timer <= 0f)
                {
                    Phase = FishingPhase.Bite;
                    timer = biteWindow;
                }
            }
            else if (Phase == FishingPhase.Bite)
            {
                timer -= dt;
                if (timer <= 0f) Finish(FishingResult.Escaped);
            }
        }

        public void Press()
        {
            if (Phase == FishingPhase.Bite) Finish(FishingResult.Caught);
            else if (Phase == FishingPhase.Waiting) Finish(FishingResult.Cancelled);
        }

        public void Cancel()
        {
            if (Phase == FishingPhase.Waiting || Phase == FishingPhase.Bite)
                Finish(FishingResult.Cancelled);
        }

        public void Reset()
        {
            Phase = FishingPhase.Idle;
            Result = FishingResult.None;
            timer = 0f;
        }

        private void Finish(FishingResult result)
        {
            Phase = FishingPhase.Done;
            Result = result;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }

    public static class ToolNames
    {
        public static string Of(ToolType type)
        {
            switch (type)
            {
                case ToolType.Axe: return "Rìu";
                case ToolType.Pickaxe: return "Cuốc";
                case ToolType.FishingRod: return "Cần câu";
                default: return "Tay không";
            }
        }
    }
}
