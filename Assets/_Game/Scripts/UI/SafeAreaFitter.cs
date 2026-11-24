using UnityEngine;

namespace Game.UI
{
    /// <summary>Reusable canvas child that keeps touch targets inside notches and rounded-screen safe areas.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        public static RectTransform GetOrCreate(Transform canvasRoot)
        {
            if (canvasRoot == null) return null;
            Transform existing = canvasRoot.Find("MobileSafeAreaRoot");
            if (existing != null)
            {
                RectTransform existingRect = existing as RectTransform;
                if (existingRect == null) return null;
                // Older or manually edited scenes may already contain the named root
                // without the fitter component. Repair it instead of silently returning
                // a root that never applies Android/iOS cutout insets.
                if (existingRect.GetComponent<SafeAreaFitter>() == null)
                    existingRect.gameObject.AddComponent<SafeAreaFitter>();
                return existingRect;
            }

            GameObject root = new GameObject("MobileSafeAreaRoot", typeof(RectTransform), typeof(SafeAreaFitter));
            root.transform.SetParent(canvasRoot, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void OnEnable()
        {
            rectTransform = transform as RectTransform;
            ApplySafeArea(true);
        }

        private void Update() => ApplySafeArea(false);

        private void ApplySafeArea(bool force)
        {
            if (rectTransform == null) rectTransform = transform as RectTransform;
            if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0) return;

            Rect safe = Application.isMobilePlatform ? Screen.safeArea : new Rect(0f, 0f, Screen.width, Screen.height);
            Vector2Int size = new Vector2Int(Screen.width, Screen.height);
            if (!force && safe == lastSafeArea && size == lastScreenSize) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x = Mathf.Clamp01(min.x / Screen.width);
            min.y = Mathf.Clamp01(min.y / Screen.height);
            max.x = Mathf.Clamp01(max.x / Screen.width);
            max.y = Mathf.Clamp01(max.y / Screen.height);
            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            lastSafeArea = safe;
            lastScreenSize = size;
        }
    }
}
