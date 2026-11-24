using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public sealed class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform handle;
        [SerializeField, Min(1f)] private float radius = 100f;

        public Vector2 Value { get; private set; }

        private RectTransform area;
        private int activePointerId = int.MinValue;

        private void Awake()
        {
            area = transform as RectTransform;
            ResetJoystick();
            if (handle == null)
                Debug.LogError("VirtualJoystick: Handle reference is missing.", this);
        }

        private void OnDisable()
        {
            activePointerId = int.MinValue;
            ResetJoystick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (GameplayInputGate.IsLocked || HudEditMode.Active || activePointerId != int.MinValue)
                return;

            activePointerId = eventData.pointerId;
            UpdateJoystick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (GameplayInputGate.IsLocked || HudEditMode.Active || eventData.pointerId != activePointerId)
                return;

            UpdateJoystick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
                return;

            activePointerId = int.MinValue;
            ResetJoystick();
        }

        private void UpdateJoystick(PointerEventData eventData)
        {
            if (area == null || handle == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    area, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
                return;

            float safeRadius = Mathf.Max(1f, radius);
            Vector2 clamped = Vector2.ClampMagnitude(localPoint, safeRadius);
            handle.anchoredPosition = clamped;
            Value = clamped / safeRadius;
        }

        public void ResetInput()
        {
            activePointerId = int.MinValue;
            ResetJoystick();
        }

        private void ResetJoystick()
        {
            if (handle != null)
                handle.anchoredPosition = Vector2.zero;
            Value = Vector2.zero;
        }

        private void OnValidate()
        {
            radius = Mathf.Max(1f, radius);
        }
    }
}
