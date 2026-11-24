using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    /// <summary>Hold-to-gather control; pointer down queues one press and maintains held state.</summary>
    [DisallowMultipleComponent]
    public sealed class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private bool pressed;
        private int activePointerId = int.MinValue;

        public bool Held { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (GameplayInputGate.IsLocked || HudEditMode.Active || activePointerId != int.MinValue) return;
            activePointerId = eventData.pointerId;
            pressed = true;
            Held = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            activePointerId = int.MinValue;
            Held = false;
        }

        public bool Consume()
        {
            bool value = pressed;
            pressed = false;
            return value;
        }

        public void ResetInput()
        {
            pressed = false;
            Held = false;
            activePointerId = int.MinValue;
        }

        private void OnDisable()
        {
            pressed = false;
            Held = false;
            activePointerId = int.MinValue;
        }
    }
}
