using UnityEngine;

namespace Game.Core
{
    /// <summary>Input boundary consumed by gameplay, independent of touch/UI implementation.</summary>
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        Vector2 ConsumeLook();
        bool ConsumeJump();
        bool SprintHeld { get; }
        bool ActionHeld { get; }
        bool ConsumeAction();
        void ClearPendingInput();
    }

    public static class InputService
    {
        public static IPlayerInput Current { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;
    }
}
