using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Touch-first recipe browser. UI is built at runtime and uses PlayerInventory's model.</summary>
    [DisallowMultipleComponent]
    public sealed class CraftingHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CraftingCatalog catalog;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Transform playerRoot;
        [Header("Interaction")]
        [SerializeField, Min(1f)] private float stationRange = 5.2f;

        private Canvas canvas;
        private GameObject panel;
        private RectTransform recipeContent;
        private Text stationLabel;
        private Text messageLabel;
        private readonly List<GameObject> recipeRows = new List<GameObject>();
        private CraftingStation nearbyStation;
        private bool inputLocked;
        private float nextStationScan;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            ResolveReferences();
            BuildUI();
            Refresh();
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.Changed += Refresh;
            if (IsOpen)
            {
                if (GameplayInputGate.IsLockedByOther(this))
                    Close();
                else
                    SetGameplayInputLocked(true);
            }
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Refresh;
            if (panel != null) panel.SetActive(false);
            SetGameplayInputLocked(false);
        }

        private void OnDestroy()
        {
            SetGameplayInputLocked(false);
            if (inventory != null) inventory.Changed -= Refresh;
        }

        private void Update()
        {
            if (IsOpen && (inventory == null || catalog == null))
            {
                Close();
                HudMessages.Toast("Bảng chế tạo không còn dữ liệu cần thiết.");
                return;
            }

            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) Toggle();
            if (Time.unscaledTime >= nextStationScan)
            {
                nextStationScan = Time.unscaledTime + 0.25f;
                bool stationChanged = FindNearbyStation();
                if (IsOpen && stationChanged) Refresh();
            }
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            if (inventory == null || catalog == null || panel == null) return;
            if (GameplayInputGate.IsLockedByOther(this))
            {
                HudMessages.Toast("Hãy đóng bảng đang mở trước.");
                return;
            }
            panel.SetActive(true);
            SetGameplayInputLocked(true);
            FindNearbyStation();
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            SetGameplayInputLocked(false);
        }

        private void Craft(RecipeData recipe)
        {
            if (inventory == null || recipe == null)
            {
                HudMessages.Toast("Không thể chế tạo: thiếu dữ liệu.");
                return;
            }
            int tier = nearbyStation != null ? nearbyStation.Tier : 0;
            if (!CraftingRules.TryCraft(recipe, inventory.Model, tier, out CraftingFailure failure))
            {
                HudMessages.Toast(FailureText(failure));
                Refresh();
                return;
            }
            inventory.NotifyChanged();
            HudMessages.Toast($"Đã chế tạo: {recipe.output.displayName} x{recipe.outputAmount}");
            Refresh();
        }

        private bool FindNearbyStation()
        {
            if (playerRoot == null) return false;
            CraftingStation[] stations = Object.FindObjectsByType<CraftingStation>(FindObjectsSortMode.None);
            CraftingStation best = null;
            float bestDistance = stationRange * stationRange;
            foreach (CraftingStation station in stations)
            {
                if (station == null || !station.isActiveAndEnabled) continue;
                float distance = (station.transform.position - playerRoot.position).sqrMagnitude;
                if (distance > stationRange * stationRange) continue;
                // Prefer the highest tier among stations in reach, then the nearest of that tier.
                if (best == null || station.Tier > best.Tier ||
                    (station.Tier == best.Tier && distance < bestDistance))
                {
                    bestDistance = distance;
                    best = station;
                }
            }
            if (best == nearbyStation) return false;
            nearbyStation = best;
            return true;
        }

        private void Refresh()
        {
            if (panel == null || !IsOpen) return;
            stationLabel.text = nearbyStation == null
                ? "Chế tạo tay · Không có bàn gần đây"
                : $"{nearbyStation.DisplayName} · Cấp {nearbyStation.Tier}";
            messageLabel.text = inventory != null
                ? $"Túi: {inventory.TotalWeight:0.0}/{inventory.MaxWeight:0.0} kg"
                : "Chưa nối PlayerInventory";

            foreach (GameObject row in recipeRows) if (row != null) Destroy(row);
            recipeRows.Clear();
            if (catalog == null || catalog.recipes == null) return;

            int tier = nearbyStation != null ? nearbyStation.Tier : 0;
            foreach (RecipeData recipe in catalog.recipes)
            {
                if (recipe == null) continue;
                CraftingFailure failure = CraftingRules.CanCraft(recipe, inventory != null ? inventory.Model : null, tier);
                Button button = CreateRecipeButton(recipe, failure);
                button.interactable = failure == CraftingFailure.None;
                RecipeData captured = recipe;
                button.onClick.AddListener(() => Craft(captured));
                recipeRows.Add(button.gameObject);
            }
        }

        private Button CreateRecipeButton(RecipeData recipe, CraftingFailure failure)
        {
            GameObject row = new GameObject("Recipe_" + recipe.id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rect = row.transform as RectTransform;
            rect.SetParent(recipeContent, false);
            rect.sizeDelta = new Vector2(0f, 104f);
            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 104f; layout.preferredHeight = 104f;
            Image image = row.GetComponent<Image>();
            image.color = failure == CraftingFailure.None ? new Color(0.10f, 0.29f, 0.32f, 0.98f) : new Color(0.12f, 0.15f, 0.18f, 0.98f);
            Button button = row.GetComponent<Button>(); button.targetGraphic = image;

            string outputName = recipe.output != null ? recipe.output.displayName : "[thiếu vật phẩm đầu ra]";
            Text title = CreateText("Title", rect, $"{recipe.displayName}  →  {outputName} x{recipe.outputAmount}", 22, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(18f, -4f), new Vector2(-36f, -8f));
            Text details = CreateText("Ingredients", rect, IngredientSummary(recipe), 17, TextAnchor.MiddleLeft);
            SetRect(details.rectTransform, new Vector2(0f, 0f), new Vector2(0.72f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 5f), new Vector2(-24f, -4f));
            Text state = CreateText("Availability", rect, failure == CraftingFailure.None ? "SẴN SÀNG" : FailureText(failure), 14, TextAnchor.MiddleRight);
            SetRect(state.rectTransform, new Vector2(0.72f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 5f), new Vector2(-36f, -4f));
            return button;
        }

        private static string IngredientSummary(RecipeData recipe)
        {
            if (recipe.ingredients == null || recipe.ingredients.Length == 0) return "Không cần nguyên liệu";
            List<string> parts = new List<string>();
            foreach (RecipeIngredient ingredient in recipe.ingredients)
                if (ingredient != null && ingredient.item != null)
                    parts.Add($"{ingredient.item.displayName} x{ingredient.amount}");
            string station = recipe.requiredStationTier == 0 ? "Tay" : recipe.requiredStationTier == 1 ? "Bàn cấp 1" : "Lò rèn cấp 2";
            return string.Join(" · ", parts) + "     |     " + station;
        }

        private static string FailureText(CraftingFailure failure)
        {
            switch (failure)
            {
                case CraftingFailure.InvalidRecipe: return "Công thức không hợp lệ";
                case CraftingFailure.MissingIngredients: return "Thiếu nguyên liệu";
                case CraftingFailure.StationRequired: return "Cần bàn chế tạo cấp cao hơn";
                case CraftingFailure.InventoryFull: return "Túi đầy hoặc quá tải";
                default: return "Không thể chế tạo";
            }
        }

        private void ResolveReferences()
        {
            if (inventory == null) inventory = Object.FindFirstObjectByType<PlayerInventory>();
            if (playerRoot == null && inventory != null) playerRoot = inventory.transform;
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        }

        private void SetGameplayInputLocked(bool locked)
        {
            if (locked == inputLocked) return;
            if (locked)
            {
                if (GameplayInputGate.IsLockedByOther(this)) return;
                GameplayInputGate.Acquire(this);
                inputLocked = true;
            }
            else
            {
                GameplayInputGate.Release(this);
                inputLocked = false;
            }
        }

        private void BuildUI()
        {
            if (canvas == null)
            {
                Debug.LogError("[CraftingHUD] Cần Canvas trong Scene.", this);
                enabled = false; return;
            }
            RectTransform canvasRect = SafeAreaFitter.GetOrCreate(canvas.transform);
            if (canvasRect == null) canvasRect = canvas.transform as RectTransform;
            Button toggle = CreateButton("CraftingToggle", canvasRect, "CHẾ TẠO", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-115f, -160f), new Vector2(180f, 72f));
            toggle.onClick.AddListener(Toggle);

            panel = CreatePanel("CraftingPanel", canvasRect, new Color(0.025f, 0.045f, 0.06f, 0.98f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 760f));
            RectTransform panelRect = panel.transform as RectTransform;
            CreateText("Title", panelRect, "CHẾ TẠO", 30, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -36f), new Vector2(350f, 52f));
            stationLabel = CreateText("Station", panelRect, "Chế tạo tay", 19, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -92f), new Vector2(-56f, 42f));
            messageLabel = CreateText("InventoryStatus", panelRect, "", 17, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -132f), new Vector2(-56f, 34f));
            Button close = CreateButton("CloseCrafting", panelRect, "ĐÓNG", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-90f, -38f), new Vector2(130f, 54f));
            close.onClick.AddListener(Close);

            GameObject scrollObject = new GameObject("RecipeScroll", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            RectTransform scrollRect = scrollObject.transform as RectTransform;
            scrollRect.SetParent(panelRect, false);
            SetRect(scrollRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(-48f, -210f));
            Image scrollImage = scrollObject.GetComponent<Image>(); scrollImage.color = new Color(0f, 0f, 0f, 0.18f); scrollImage.raycastTarget = true;
            scrollObject.GetComponent<Mask>().showMaskGraphic = false;
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            recipeContent = content.transform as RectTransform; recipeContent.SetParent(scrollRect, false);
            recipeContent.anchorMin = new Vector2(0f, 1f); recipeContent.anchorMax = new Vector2(1f, 1f); recipeContent.pivot = new Vector2(0.5f, 1f); recipeContent.anchoredPosition = Vector2.zero; recipeContent.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>(); layout.spacing = 10f; layout.padding = new RectOffset(12, 12, 12, 12); layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>(); scroll.viewport = scrollRect; scroll.content = recipeContent; scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            panel.SetActive(false);
        }

        private GameObject CreatePanel(string name, Transform parent, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = go.transform as RectTransform; rect.SetParent(parent, false); SetRect(rect, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
            go.GetComponent<Image>().color = color; return go;
        }

        private Button CreateButton(string name, Transform parent, string text, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = go.transform as RectTransform; rect.SetParent(parent, false); SetRect(rect, min, max, pivot, position, size);
            Image image = go.GetComponent<Image>(); image.color = new Color(0.1f, 0.31f, 0.38f, 0.97f);
            Button button = go.GetComponent<Button>(); button.targetGraphic = image;
            CreateText("Label", rect, text, 19, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            return CreateText(name, parent, value, size, alignment, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Vector2 min, Vector2 max, Vector2 position, Vector2 dimensions)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform rect = go.transform as RectTransform; rect.SetParent(parent, false); SetRect(rect, min, max, new Vector2(0.5f, 0.5f), position, dimensions);
            Text text = go.GetComponent<Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.color = Color.white; text.alignment = alignment; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        { rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; }
    }
}
