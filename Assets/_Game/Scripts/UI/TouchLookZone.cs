using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public sealed class TouchLookZone : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private Vector2 pendingDelta;
        private int activePointerId = int.MinValue;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (GameplayInputGate.IsLocked || HudEditMode.Active || activePointerId != int.MinValue)
                return;

            activePointerId = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!GameplayInputGate.IsLocked && !HudEditMode.Active && eventData.pointerId == activePointerId)
                pendingDelta += eventData.delta;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
                activePointerId = int.MinValue;
        }

        public Vector2 Consume()
        {
            Vector2 result = pendingDelta;
            pendingDelta = Vector2.zero;
            return result;
        }

        public void ClearInput()
        {
            pendingDelta = Vector2.zero;
            activePointerId = int.MinValue;
        }

        private void OnDisable()
        {
            pendingDelta = Vector2.zero;
            activePointerId = int.MinValue;
        }
    }
}
