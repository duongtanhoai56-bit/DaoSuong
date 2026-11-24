using Game.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Compact runtime-built survival HUD; all decorative graphics ignore raycasts.</summary>
    [DisallowMultipleComponent]
    public sealed class SurvivalHUD : MonoBehaviour
    {
        [SerializeField] private SurvivalController survival;
        private Text summaryText;
        private Text statusText;
        private Image healthFill;
        private Image hungerFill;
        private Image thirstFill;
        private Image staminaFill;
        private Image temperatureFill;
        private float nextRefresh;

        private void Awake()
        {
            if (survival == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null) survival = player.GetComponent<SurvivalController>();
            }
            BuildUI();
        }

        private void OnEnable()
        {
            if (survival != null) survival.Changed += OnVitalsChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (survival != null) survival.Changed -= OnVitalsChanged;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh();
        }

        private void OnVitalsChanged(SurvivalVitals _) => Refresh();

        private void BuildUI()
        {
            Transform existing = transform.Find("SurvivalPanel");
            if (existing != null) Destroy(existing.gameObject);
            Transform safeRoot = SafeAreaFitter.GetOrCreate(transform);
            if (safeRoot == null) safeRoot = transform;
            GameObject panel = new GameObject("SurvivalPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(safeRoot, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(250f, -18f);
            rect.sizeDelta = new Vector2(300f, 260f);
            Image background = panel.GetComponent<Image>();
            background.color = new Color(0.025f, 0.04f, 0.055f, 0.76f);
            background.raycastTarget = false;

            summaryText = CreateText(panel.transform, "SurvivalSummary", 18, new Vector2(12f, -8f), new Vector2(276f, 36f));
            statusText = CreateText(panel.transform, "SurvivalStatus", 12, new Vector2(12f, -216f), new Vector2(276f, 40f));
            healthFill = CreateBar(panel.transform, "Health", "MÁU", 48f, new Color(0.85f, 0.22f, 0.2f, 1f));
            hungerFill = CreateBar(panel.transform, "HUNGER", "ĐÓI", 82f, new Color(0.88f, 0.62f, 0.22f, 1f));
            thirstFill = CreateBar(panel.transform, "THIRST", "KHÁT", 116f, new Color(0.2f, 0.62f, 0.9f, 1f));
            staminaFill = CreateBar(panel.transform, "STAMINA", "THỂ LỰC", 150f, new Color(0.28f, 0.78f, 0.42f, 1f));
            temperatureFill = CreateBar(panel.transform, "TEMP", "NHIỆT", 184f, new Color(0.87f, 0.42f, 0.22f, 1f));
        }

        private Image CreateBar(Transform parent, string name, string label, float y, Color color)
        {
            Text title = CreateText(parent, name + "Label", 14, new Vector2(12f, -y), new Vector2(68f, 25f));
            title.text = label;
            GameObject bar = new GameObject(name + "Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.anchoredPosition = new Vector2(84f, -y - 3f);
            barRect.sizeDelta = new Vector2(198f, 18f);
            Image bg = bar.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.12f, 1f);
            bg.raycastTarget = false;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image image = fill.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text CreateText(Transform parent, string name, int size, Vector2 position, Vector2 dimensions)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            return text;
        }

        private void Refresh()
        {
            if (survival == null || summaryText == null) return;
            SurvivalVitals v = survival.Vitals;
            summaryText.text = $"HP {v.Health:0}   NHIỆT {v.BodyTemperatureC:0.0}°C";
            SetFill(healthFill, v.Health / 100f);
            SetFill(hungerFill, v.Hunger / 100f);
            SetFill(thirstFill, v.Thirst / 100f);
            SetFill(staminaFill, v.Stamina / 100f);
            SetFill(temperatureFill, Mathf.InverseLerp(34f, 40f, v.BodyTemperatureC));
            statusText.text = FormatConditions(v.Conditions);
        }

        private static void SetFill(Image image, float value)
        {
            if (image == null) return;
            RectTransform rect = image.rectTransform;
            rect.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        }

        private static string FormatConditions(SurvivalCondition condition)
        {
            if (condition == SurvivalCondition.None) return "Trạng thái: Bình thường";
            string result = "";
            Append(ref result, condition, SurvivalCondition.Bleeding, "Chảy máu");
            Append(ref result, condition, SurvivalCondition.Sick, "Bệnh");
            Append(ref result, condition, SurvivalCondition.Cold, "Hạ thân nhiệt");
            Append(ref result, condition, SurvivalCondition.HeatStress, "Quá nóng");
            Append(ref result, condition, SurvivalCondition.WellFed, "No lâu");
            Append(ref result, condition, SurvivalCondition.Hydrated, "Bù nước");
            Append(ref result, condition, SurvivalCondition.Exhausted, "Kiệt sức");
            Append(ref result, condition, SurvivalCondition.Dead, "Gục ngã");
            return result;
        }

        private static void Append(ref string result, SurvivalCondition source, SurvivalCondition flag, string label)
        {
            if ((source & flag) == 0) return;
            if (result.Length > 0) result += " · ";
            result += label;
        }
    }
}
