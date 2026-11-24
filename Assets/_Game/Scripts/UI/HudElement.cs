using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class HudElement : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private string id = "element";
        [SerializeField] private bool customizable = true;

        public string Id => id;
        public bool Customizable => customizable;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 baseAnchorMin;
        private Vector2 baseAnchorMax;
        private Vector2 basePivot;
        private Vector2 basePosition;
        private bool initialized;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvasGroup = GetComponent<CanvasGroup>();
            baseAnchorMin = rectTransform.anchorMin;
            baseAnchorMax = rectTransform.anchorMax;
            basePivot = rectTransform.pivot;
            basePosition = rectTransform.anchoredPosition;
            initialized = true;
        }

        private void OnEnable()
        {
            ControlsSettingsStore.Changed += Apply;
            HudEditMode.Changed += RefreshHighlight;
            Apply();
            RefreshHighlight();
        }

        private void OnDisable()
        {
            ControlsSettingsStore.Changed -= Apply;
            HudEditMode.Changed -= RefreshHighlight;
        }

        private void Apply()
        {
            if (!initialized)
                return;

            ControlsSettings settings = ControlsSettingsStore.Current;
            bool leftHanded = settings.leftHanded;
            float mirror = leftHanded ? -1f : 1f;

            rectTransform.anchorMin = leftHanded
                ? new Vector2(1f - baseAnchorMax.x, baseAnchorMin.y)
                : baseAnchorMin;
            rectTransform.anchorMax = leftHanded
                ? new Vector2(1f - baseAnchorMin.x, baseAnchorMax.y)
                : baseAnchorMax;
            rectTransform.pivot = leftHanded
                ? new Vector2(1f - basePivot.x, basePivot.y)
                : basePivot;

            ElementLayout layout = null;
            Vector2 offset = Vector2.zero;
            float scale = 1f;
            if (customizable)
            {
                layout = settings.Get(id);
                offset = layout.offset;
                scale = Mathf.Clamp(layout.scale, 0.5f, 2f);
            }

            Vector2 position = new Vector2((basePosition.x + offset.x) * mirror, basePosition.y + offset.y);
            rectTransform.localScale = Vector3.one * scale;
            Vector2 appliedPosition = customizable ? ClampPositionToCanvas(position, scale) : position;
            rectTransform.anchoredPosition = appliedPosition;

            // Persist the clamped offset too, so dragging against the screen edge does not
            // accumulate an invisible offset that must later be dragged back through.
            if (layout != null)
            {
                layout.offset = new Vector2(appliedPosition.x * mirror - basePosition.x,
                                            appliedPosition.y - basePosition.y);
            }
        }

        private Vector2 ClampPositionToCanvas(Vector2 anchoredPosition, float scale)
        {
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null)
                return anchoredPosition;

            Vector2 anchorReference = parentRect.rect.min + Vector2.Scale(
                parentRect.rect.size,
                Vector2.Lerp(rectTransform.anchorMin, rectTransform.anchorMax, rectTransform.pivot));
            Vector2 pivotPosition = anchorReference + anchoredPosition;
            Vector2 halfSize = rectTransform.rect.size * (0.5f * scale);
            const float safeMargin = 12f;

            float minX = parentRect.rect.xMin + halfSize.x + safeMargin;
            float maxX = parentRect.rect.xMax - halfSize.x - safeMargin;
            float minY = parentRect.rect.yMin + halfSize.y + safeMargin;
            float maxY = parentRect.rect.yMax - halfSize.y - safeMargin;

            if (minX > maxX) minX = maxX = parentRect.rect.center.x;
            if (minY > maxY) minY = maxY = parentRect.rect.center.y;

            pivotPosition.x = Mathf.Clamp(pivotPosition.x, minX, maxX);
            pivotPosition.y = Mathf.Clamp(pivotPosition.y, minY, maxY);
            return pivotPosition - anchorReference;
        }

        private void RefreshHighlight()
        {
            if (canvasGroup == null)
                return;

            bool isOtherElement = HudEditMode.Active && customizable && HudEditMode.Selected != this;
            canvasGroup.alpha = isOtherElement ? 0.48f : 1f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (HudEditMode.Active && customizable)
                HudEditMode.Select(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!HudEditMode.Active || !customizable)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            float scaleFactor = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            if (scaleFactor <= 0f)
                scaleFactor = 1f;

            ControlsSettings settings = ControlsSettingsStore.Current;
            ElementLayout layout = settings.Get(id);
            float mirror = settings.leftHanded ? -1f : 1f;
            layout.offset += new Vector2(eventData.delta.x / scaleFactor * mirror, eventData.delta.y / scaleFactor);
            Apply();
            ControlsSettingsStore.NotifyChanged();
        }
    }
}
