using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Touch-first inventory and chest UI. UI is constructed at runtime to avoid prefab setup.</summary>
    [DisallowMultipleComponent]
    public sealed class InventoryHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Transform playerRoot;
        [Header("Interaction")]
        [SerializeField, Min(0.5f)] private float chestRange = 2.8f;
        [SerializeField] private bool allowKeyboardToggle = true;

        private readonly Collider[] nearbyBuffer = new Collider[96];
        private readonly List<InventorySlotView> playerSlots = new List<InventorySlotView>();
        private readonly List<InventorySlotView> chestSlots = new List<InventorySlotView>();
        private Canvas canvas;
        private GameObject panel;
        private GameObject chestSection;
        private GameObject chestButtonObject;
        private Text playerWeightText;
        private Text chestWeightText;
        private Button useSelectedButton;
        private SurvivalController survivalController;
        private RectTransform playerGrid;
        private RectTransform chestGrid;
        private StorageContainer activeChest;
        private StorageContainer nearbyChest;
        private bool selected;
        private bool selectedFromChest;
        private int selectedIndex = -1;
        private bool dragging;
        private bool dragFromChest;
        private int dragIndex = -1;
        private float nextChestScan;
        private bool inputLockedByInventory;

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
            if (activeChest != null) activeChest.Changed += Refresh;
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
            if (activeChest != null) activeChest.Changed -= Refresh;
            activeChest = null;
            ClearSelection();
            if (panel != null) panel.SetActive(false);
            if (chestSection != null) chestSection.SetActive(false);
            SetGameplayInputLocked(false);
        }

        private void OnDestroy()
        {
            SetGameplayInputLocked(false);
            if (inventory != null) inventory.Changed -= Refresh;
            if (activeChest != null) activeChest.Changed -= Refresh;
        }

        private void Update()
        {
            if (IsOpen && inventory == null)
            {
                Close();
                HudMessages.Toast("Inventory người chơi không còn khả dụng.");
                return;
            }

            if (IsOpen && chestSection != null && chestSection.activeSelf && activeChest == null)
            {
                Close();
                HudMessages.Toast("Rương không còn khả dụng.");
                return;
            }

            if (allowKeyboardToggle && Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
                Toggle();

            if (Time.unscaledTime >= nextChestScan)
            {
                nextChestScan = Time.unscaledTime + 0.2f;
                FindNearbyChest();
            }
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (inventory == null || panel == null || chestSection == null) return;
            if (GameplayInputGate.IsLockedByOther(this))
            {
                HudMessages.Toast("Hãy đóng bảng đang mở trước.");
                return;
            }
            if (activeChest != null) activeChest.Changed -= Refresh;
            activeChest = null;
            panel.SetActive(true);
            chestSection.SetActive(false);
            SetGameplayInputLocked(true);
            ClearSelection();
            Refresh();
        }

        public void OpenStorage(StorageContainer chest)
        {
            if (inventory == null || chest == null || panel == null || chestSection == null) return;
            if (GameplayInputGate.IsLockedByOther(this))
            {
                HudMessages.Toast("Hãy đóng bảng đang mở trước.");
                return;
            }
            if (activeChest != null) activeChest.Changed -= Refresh;
            activeChest = chest;
            activeChest.Changed += Refresh;
            panel.SetActive(true);
            chestSection.SetActive(true);
            SetGameplayInputLocked(true);
            ClearSelection();
            BuildChestGrid();
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (activeChest != null) activeChest.Changed -= Refresh;
            activeChest = null;
            ClearSelection();
            SetGameplayInputLocked(false);
        }

        public void OnSlotClicked(bool fromChest, int index)
        {
            if (dragging) return;
            if (!selected)
            {
                if (GetStack(fromChest, index) == null) return;
                Select(fromChest, index);
                Refresh();
                return;
            }

            if (selectedFromChest == fromChest && selectedIndex == index)
            {
                ClearSelection();
                Refresh();
                return;
            }

            Move(selectedFromChest, selectedIndex, fromChest, index);
            ClearSelection();
            Refresh();
        }

        public void BeginDrag(bool fromChest, int index)
        {
            if (GetStack(fromChest, index) == null) return;
            dragging = true;
            dragFromChest = fromChest;
            dragIndex = index;
            Select(fromChest, index);
            Refresh();
        }

        public void DropOn(bool targetChest, int targetIndex)
        {
            if (!dragging || dragIndex < 0) return;
            if (dragFromChest == targetChest)
            {
                if (GetModel(targetChest)?.TryMove(dragIndex, targetIndex) == true)
                    NotifyModelChanged(targetChest);
            }
            else
            {
                Transfer(dragFromChest, dragIndex, targetChest, targetIndex);
            }
            ClearSelection();
            Refresh();
        }

        public void EndDrag()
        {
            dragging = false;
            dragIndex = -1;
        }

        public void AutoSortPlayer()
        {
            inventory?.AutoSort();
            Refresh();
        }

        public void AutoSortChest()
        {
            activeChest?.AutoSort();
            Refresh();
        }

        public void SplitSelectedHalf()
        {
            if (!selected) return;
            InventoryModel model = GetModel(selectedFromChest);
            if (model == null) return;
            ItemStack stack = model.GetSlot(selectedIndex);
            if (stack == null || stack.Tool != null || stack.count < 2) return;
            for (int i = 0; i < model.SlotCount; i++)
            {
                if (model.GetSlot(i) != null) continue;
                model.TrySplit(selectedIndex, i, stack.count / 2);
                NotifyModelChanged(selectedFromChest);
                ClearSelection();
                Refresh();
                return;
            }
        }

        private void Move(bool sourceChest, int sourceIndex, bool targetChest, int targetIndex)
        {
            if (sourceChest == targetChest)
            {
                GetModel(sourceChest)?.TryMove(sourceIndex, targetIndex);
                NotifyModelChanged(sourceChest);
            }
            else
            {
                Transfer(sourceChest, sourceIndex, targetChest, targetIndex);
            }
        }

        private void Transfer(bool sourceChest, int sourceIndex, bool targetChest, int targetIndex)
        {
            InventoryModel source = GetModel(sourceChest);
            InventoryModel destination = GetModel(targetChest);
            if (source == null || destination == null) return;
            ItemStack stack = source.GetSlot(sourceIndex);
            if (stack == null) return;

            // A cross-container tap/drop transfers into the selected target slot when empty,
            // otherwise it uses the same safe destination-add path as a normal stack transfer.
            source.TryTransferToAt(destination, sourceIndex, targetIndex, stack.count, out _);
            NotifyModelChanged(sourceChest);
            NotifyModelChanged(targetChest);
        }

        private void NotifyModelChanged(bool chest)
        {
            if (chest) activeChest?.NotifyChanged();
            else inventory?.NotifyChanged();
        }

        private void Select(bool fromChest, int index)
        {
            selected = true;
            selectedFromChest = fromChest;
            selectedIndex = index;
        }

        private void ClearSelection()
        {
            selected = false;
            selectedIndex = -1;
            dragging = false;
            dragIndex = -1;
        }

        private bool IsSelected(bool chest, int index)
            => selected && selectedFromChest == chest && selectedIndex == index;

        private InventoryModel GetModel(bool chest)
        {
            if (!chest) return inventory != null ? inventory.Model : null;
            return activeChest != null ? activeChest.Model : null;
        }

        private ItemStack GetStack(bool chest, int index)
        {
            InventoryModel model = GetModel(chest);
            return model != null && index >= 0 && index < model.SlotCount ? model.GetSlot(index) : null;
        }

        private void ResolveReferences()
        {
            if (inventory == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null) inventory = player.GetComponent<PlayerInventory>();
            }
            if (playerRoot == null && inventory != null) playerRoot = inventory.transform;
            if (survivalController == null && inventory != null) survivalController = inventory.GetComponent<SurvivalController>();
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = GetComponent<Canvas>();
        }

        private void SetGameplayInputLocked(bool locked)
        {
            if (locked == inputLockedByInventory) return;
            if (locked)
            {
                if (GameplayInputGate.IsLockedByOther(this)) return;
                GameplayInputGate.Acquire(this);
                inputLockedByInventory = true;
            }
            else
            {
                GameplayInputGate.Release(this);
                inputLockedByInventory = false;
            }
        }

        private void BuildUI()
        {
            if (canvas == null)
            {
                Debug.LogError("[InventoryHUD] Hãy gắn InventoryHUD lên Canvas hoặc object con của Canvas.");
                enabled = false;
                return;
            }

            RectTransform canvasRect = SafeAreaFitter.GetOrCreate(canvas.transform);
            if (canvasRect == null) canvasRect = canvas.transform as RectTransform;
            Button toggleButton = CreateButton("InventoryToggle", canvasRect, "TÚI", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-115f, -70f), new Vector2(180f, 76f));
            toggleButton.onClick.AddListener(Toggle);

            Button chestButton = CreateButton("ChestNearbyButton", canvasRect, "RƯƠNG", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-315f, -70f), new Vector2(180f, 76f));
            chestButton.onClick.AddListener(() =>
            {
                if (nearbyChest != null) OpenStorage(nearbyChest);
            });
            chestButtonObject = chestButton.gameObject;
            chestButtonObject.SetActive(false);

            panel = CreateImage("InventoryPanel", canvasRect, new Color(0.025f, 0.045f, 0.06f, 0.97f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 700f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            CreateText("Title", panelRect, "TÚI ĐỒ", 30, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -38f), new Vector2(320f, 52f));
            playerWeightText = CreateText("PlayerWeight", panelRect, "0 / 80 kg", 20, TextAnchor.MiddleRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-170f, -38f), new Vector2(280f, 44f));

            Button sortPlayer = CreateButton("SortPlayer", panelRect, "SẮP XẾP", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(100f, -105f), new Vector2(160f, 58f));
            sortPlayer.onClick.AddListener(AutoSortPlayer);
            Button split = CreateButton("SplitHalf", panelRect, "TÁCH 1/2", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, -105f), new Vector2(160f, 58f));
            split.onClick.AddListener(SplitSelectedHalf);
            useSelectedButton = CreateButton("UseSelectedItem", panelRect, "DÙNG", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(390f, -105f), new Vector2(120f, 58f));
            useSelectedButton.onClick.AddListener(UseSelectedItem);
            Button close = CreateButton("CloseInventory", panelRect, "ĐÓNG", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-95f, -38f), new Vector2(130f, 54f));
            close.onClick.AddListener(Close);

            playerGrid = CreateGridRoot("PlayerGrid", panelRect, new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(550f, 500f));
            BuildSlots(playerGrid, inventory != null ? inventory.SlotCount : 24, false, playerSlots);

            chestSection = new GameObject("ChestSection", typeof(RectTransform));
            RectTransform chestSectionRect = chestSection.transform as RectTransform;
            chestSectionRect.SetParent(panelRect, false);
            SetRect(chestSectionRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-25f, 45f), new Vector2(430f, 540f));
            CreateText("ChestTitle", chestSectionRect, "RƯƠNG", 24, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(400f, 46f));
            chestWeightText = CreateText("ChestWeight", chestSectionRect, "", 18, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(400f, 36f));
            Button sortChest = CreateButton("SortChest", chestSectionRect, "SẮP XẾP RƯƠNG", new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(220f, 52f));
            sortChest.onClick.AddListener(AutoSortChest);
            chestGrid = CreateGridRoot("ChestGrid", chestSectionRect, new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(410f, 390f));
            chestSection.SetActive(false);
            panel.SetActive(false);
        }

        private void BuildChestGrid()
        {
            if (chestGrid == null || activeChest == null) return;
            ClearGrid(chestGrid, chestSlots);
            GridLayoutGroup grid = chestGrid.GetComponent<GridLayoutGroup>();
            grid.constraintCount = 6;
            grid.cellSize = new Vector2(54f, 54f);
            grid.spacing = new Vector2(5f, 5f);
            BuildSlots(chestGrid, activeChest.Model.SlotCount, true, chestSlots);
        }

        private void BuildSlots(RectTransform root, int count, bool storage, List<InventorySlotView> collection)
        {
            ClearGrid(root, collection);
            for (int i = 0; i < count; i++)
            {
                GameObject cell = CreateImage("Slot_" + i, root, new Color(0.12f, 0.17f, 0.2f, 0.96f),
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                InventorySlotView view = cell.AddComponent<InventorySlotView>();
                Image icon = CreateChildImage("ItemIcon", cell.transform, new Color(1f, 1f, 1f, 1f),
                    new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.88f));
                icon.raycastTarget = false;
                Text name = CreateText("ItemName", cell.transform, "", 15, TextAnchor.MiddleCenter,
                    new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero);
                name.raycastTarget = false;
                Text amount = CreateText("Amount", cell.transform, "", 16, TextAnchor.LowerRight,
                    new Vector2(0.55f, 0.02f), new Vector2(0.98f, 0.32f), Vector2.zero, Vector2.zero);
                amount.raycastTarget = false;
                Text durability = CreateText("Durability", cell.transform, "", 11, TextAnchor.LowerLeft,
                    new Vector2(0.02f, 0.02f), new Vector2(0.6f, 0.24f), Vector2.zero, Vector2.zero);
                durability.raycastTarget = false;
                view.Initialize(this, storage, i, cell.GetComponent<Image>(), icon, name, amount, durability);
                collection.Add(view);
            }
        }

        private void ClearGrid(RectTransform root, List<InventorySlotView> collection)
        {
            collection.Clear();
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        private void Refresh()
        {
            if (inventory == null || playerWeightText == null) return;
            playerWeightText.text = $"{inventory.TotalWeight:0.0} / {inventory.MaxWeight:0.0} kg";
            for (int i = 0; i < playerSlots.Count; i++)
            {
                ItemStack stack = inventory.Model.GetSlot(i);
                playerSlots[i].Bind(stack, IsSelected(false, i));
            }
            UpdateUseButton();
            if (activeChest != null)
            {
                playerGrid.anchoredPosition = new Vector2(-225f, 50f);
                playerGrid.sizeDelta = new Vector2(450f, 500f);
                chestWeightText.text = $"{activeChest.TotalWeight:0.0} / {activeChest.MaxWeight:0.0} kg";
                for (int i = 0; i < chestSlots.Count; i++)
                    chestSlots[i].Bind(activeChest.Model.GetSlot(i), IsSelected(true, i));
            }
            else
            {
                playerGrid.anchoredPosition = new Vector2(0f, 50f);
                playerGrid.sizeDelta = new Vector2(550f, 500f);
            }
        }

        private void UpdateUseButton()
        {
            if (useSelectedButton == null) return;
            ItemStack selectedStack = selected && !selectedFromChest && selectedIndex >= 0 &&
                selectedIndex < inventory.SlotCount ? inventory.GetSlot(selectedIndex) : null;
            useSelectedButton.interactable = survivalController != null && selectedStack != null &&
                selectedStack.item is ConsumableData;
        }

        private void UseSelectedItem()
        {
            if (!selected || selectedFromChest || survivalController == null || inventory == null)
                return;
            if (survivalController.TryConsume(inventory, selectedIndex))
            {
                ClearSelection();
                Refresh();
            }
            else
            {
                HudMessages.Toast("Không thể sử dụng vật phẩm này lúc này.");
            }
        }

        private void FindNearbyChest()
        {
            if (playerRoot == null || chestButtonObject == null || (IsOpen && activeChest != null)) return;
            int count = Physics.OverlapSphereNonAlloc(playerRoot.position, chestRange, nearbyBuffer,
                ~0, QueryTriggerInteraction.Collide);
            StorageContainer nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider candidateCollider = nearbyBuffer[i];
                if (candidateCollider == null) continue;
                StorageContainer candidate = candidateCollider.GetComponentInParent<StorageContainer>();
                if (candidate == null) continue;
                float distance = (candidate.transform.position - playerRoot.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = candidate; }
            }
            nearbyChest = nearest;
            chestButtonObject.SetActive(nearbyChest != null);
        }

        private GameObject CreateImage(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = go.transform as RectTransform;
            rect.SetParent(parent, false);
            SetRect(rect, anchorMin, anchorMax, pivot, position, size);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return go;
        }

        private Image CreateChildImage(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = go.transform as RectTransform;
            rect.SetParent(parent, false);
            SetRect(rect, min, max, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform rect = go.transform as RectTransform;
            rect.SetParent(parent, false);
            SetRect(rect, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), position, dimensions);
            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label,
            Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject go = CreateImage(name, parent, new Color(0.1f, 0.31f, 0.38f, 0.96f), min, max, pivot, position, size);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.18f, 0.48f, 0.55f, 1f);
            colors.pressedColor = new Color(0.08f, 0.23f, 0.3f, 1f);
            button.colors = colors;
            Text text = CreateText("Label", go.transform, label, 20, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.raycastTarget = false;
            return button;
        }

        private RectTransform CreateGridRoot(string name, Transform parent,
            Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup));
            RectTransform rect = go.transform as RectTransform;
            rect.SetParent(parent, false);
            SetRect(rect, min, max, pivot, position, size);
            GridLayoutGroup grid = go.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(68f, 68f);
            grid.spacing = new Vector2(7f, 7f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void OnValidate()
        {
            chestRange = Mathf.Max(0.5f, chestRange);
        }
    }
}
