using Game.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Runtime-created inventory cell; supports both touch drag and tap-to-move.</summary>
    public sealed class InventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler,
        IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        private InventoryHUD owner;
        private bool isStorage;
        private int slotIndex;
        private Image background;
        private Image itemIcon;
        private Text itemName;
        private Text amountLabel;
        private Text durabilityLabel;

        public void Initialize(InventoryHUD hud, bool storage, int index,
            Image cellBackground, Image icon, Text nameLabel, Text amount, Text durability)
        {
            owner = hud;
            isStorage = storage;
            slotIndex = index;
            background = cellBackground;
            itemIcon = icon;
            itemName = nameLabel;
            amountLabel = amount;
            durabilityLabel = durability;
        }

        public void Bind(ItemStack stack, bool selected)
        {
            background.color = selected ? new Color(0.95f, 0.72f, 0.27f, 0.98f) : new Color(0.12f, 0.17f, 0.2f, 0.96f);
            bool occupied = stack != null && stack.item != null;
            itemIcon.enabled = occupied;
            itemName.enabled = occupied && stack.item.icon == null;
            itemName.text = occupied ? ShortName(stack.item.displayName) : string.Empty;
            amountLabel.text = occupied && stack.Tool == null && stack.count > 1 ? stack.count.ToString() : string.Empty;
            durabilityLabel.text = occupied && stack.Tool != null ? $"{stack.durability}/{stack.Tool.maxDurability}" : string.Empty;
            if (occupied)
            {
                itemIcon.sprite = stack.item.icon;
                itemIcon.color = stack.item.color;
                itemIcon.preserveAspect = true;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                owner?.OnSlotClicked(isStorage, slotIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            owner?.BeginDrag(isStorage, slotIndex);
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;
            owner?.DropOn(isStorage, slotIndex);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.EndDrag();
        }

        private static string ShortName(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= 9 ? value : value.Substring(0, 8) + "…";
        }
    }
}
