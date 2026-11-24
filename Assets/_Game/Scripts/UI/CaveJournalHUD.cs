using Game.Core;
using Game.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Touch-friendly, scrollable journal for cave lore collected across sessions.</summary>
    [DisallowMultipleComponent]
    public sealed class CaveJournalHUD : MonoBehaviour
    {
        private CaveManager manager;
        private GameObject panel;
        private Text contentText;
        private Button toggleButton;
        private float nextRefresh;
        private bool panelOpen;

        private void Start()
        {
            manager = FindFirstObjectByType<CaveManager>();
            BuildUI();
            RefreshJournal();
        }

        private void OnDisable()
        {
            panelOpen = false;
            GameplayInputGate.Release(this);
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            GameplayInputGate.Release(this);
        }

        private void Update()
        {
            if (!panelOpen || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 1f;
            RefreshJournal();
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            Transform safeRoot = SafeAreaFitter.GetOrCreate(canvas.transform);
            if (safeRoot == null) safeRoot = canvas.transform;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Keep the journal in the upper-right HUD stack, below ONLINE/PTT and
            // away from the lower-right attack/build/island controls.
            toggleButton = CreateButton("CaveJournalToggle", safeRoot, "NHẬT KÝ",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-100f, -198f),
                new Vector2(170f, 54f), font, TogglePanel);

            panel = new GameObject("CaveJournalPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(safeRoot, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(980f, 790f);
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.025f, 0.045f, 0.065f, 0.98f);

            CreateText("JournalTitle", panel.transform, "NHẬT KÝ KHÁM PHÁ", 30,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -18f),
                new Vector2(700f, 48f), font, TextAnchor.MiddleLeft, Color.white);

            GameObject scrollObject = new GameObject("JournalScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(panel.transform, false);
            RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(24f, 100f);
            scrollRect.offsetMax = new Vector2(-24f, -82f);
            Image scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = new Color(0.05f, 0.075f, 0.09f, 0.9f);
            scrollImage.raycastTarget = true;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject content = new GameObject("JournalContent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
            contentText = content.GetComponent<Text>();
            contentText.font = font;
            contentText.fontSize = 23;
            contentText.color = new Color(0.9f, 0.93f, 0.95f);
            contentText.alignment = TextAnchor.UpperLeft;
            contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            contentText.verticalOverflow = VerticalWrapMode.Overflow;
            contentText.lineSpacing = 1.12f;
            contentText.raycastTarget = false;
            contentText.supportRichText = false;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 45f;

            CreateButton("CloseCaveJournal", panel.transform, "ĐÓNG",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f),
                new Vector2(230f, 60f), font, () => SetPanelOpen(false));
            panel.SetActive(false);
        }

        private void TogglePanel() => SetPanelOpen(!panelOpen);

        private void SetPanelOpen(bool open)
        {
            if (open && GameplayInputGate.IsLockedByOther(this))
            {
                HudMessages.Toast("Hãy đóng cửa sổ đang mở trước khi xem nhật ký.");
                return;
            }
            panelOpen = open;
            if (panel != null) panel.SetActive(open);
            if (toggleButton != null) toggleButton.interactable = !open;
            if (open)
            {
                GameplayInputGate.Acquire(this);
                RefreshJournal();
            }
            else
            {
                GameplayInputGate.Release(this);
            }
        }

        private void RefreshJournal()
        {
            if (contentText == null) return;
            if (manager == null) manager = FindFirstObjectByType<CaveManager>();
            if (manager == null || manager.DiscoveredLore == null || manager.DiscoveredLore.Count == 0)
            {
                contentText.text = "Chưa tìm thấy ghi chép nào. Hãy khám phá hang động, mở rương bí mật và tương tác với các tinh thể ký ức.";
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < manager.DiscoveredLore.Count; i++)
            {
                CaveManager.CaveLoreRecord entry = manager.DiscoveredLore[i];
                if (entry == null) continue;
                builder.Append("GHI CHÉP ");
                builder.Append(i + 1);
                builder.Append("  ·  ");
                builder.AppendLine(entry.id);
                builder.AppendLine(entry.text);
                builder.AppendLine();
                builder.AppendLine("────────────────────────────────────────");
                builder.AppendLine();
            }
            contentText.text = builder.ToString();
        }

        private static Button CreateButton(string name, Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Font font, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.35f, 0.42f, 0.96f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (action != null) button.onClick.AddListener(action);

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(go.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = label;
            return button;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Font font,
            TextAnchor alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }
    }
}
