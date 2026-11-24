using System;

namespace Game.UI
{
    public static class HudEditMode
    {
        public static bool Active { get; private set; }
        public static HudElement Selected { get; private set; }
        public static event Action Changed;

        public static void SetActive(bool active)
        {
            if (Active == active && Selected == null)
                return;

            Active = active;
            Selected = null;
            Changed?.Invoke();
        }

        public static void Select(HudElement element)
        {
            if (!Active || Selected == element)
                return;

            Selected = element;
            Changed?.Invoke();
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = false;
            Selected = null;
            Changed = null;
        }
    }
}
