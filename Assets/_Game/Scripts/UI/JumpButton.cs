using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    /// <summary>Tap-to-jump input. No Unity UI Button component or OnClick event is used.</summary>
    [DisallowMultipleComponent]
    public sealed class JumpButton : MonoBehaviour, IPointerDownHandler
    {
        private bool queued;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!GameplayInputGate.IsLocked && !HudEditMode.Active)
                queued = true;
        }

        public bool Consume()
        {
            bool result = queued;
            queued = false;
            return result;
        }

        public void ClearInput() => queued = false;

        private void OnDisable()
        {
            queued = false;
        }
    }
}
