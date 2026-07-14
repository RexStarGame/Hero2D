using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerClickHandler
{
    [SerializeField] private EquipmentSlotType slotType;
    [SerializeField] private int slotNumber;
    [SerializeField] private PlayerEquipment equipment;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Image icon;
    [SerializeField] private Image validHighlight;
    [SerializeField] private TMP_Text emptyLabel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ItemHoverSource hover;

    public EquipmentSlotType SlotType => slotType;
    public int SlotNumber => slotNumber;
    public EquippableItemDefinition Item => equipment?.GetItem(slotType, slotNumber);

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (equipment != null)
            equipment.EquipmentChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (equipment != null)
            equipment.EquipmentChanged -= Refresh;
    }

    public void Refresh()
    {
        EquippableItemDefinition item = Item;

        if (icon != null)
        {
            icon.enabled = item != null;
            icon.sprite = item == null ? null : item.Icon;
        }

        if (emptyLabel != null)
            emptyLabel.gameObject.SetActive(item == null);

        if (validHighlight != null)
            validHighlight.enabled = false;

        if (hover != null)
            hover.SetItem(item, true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Item == null) return;

        ItemDragContext.Begin(this);
        ItemDragVisualUI.Instance?.Show(Item, eventData.position);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        SetHighlights(true, Item);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ItemDragVisualUI.Instance?.Move(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bool droppedOutside = !ItemDragContext.DropHandled &&
            InventoryPanelUI.Instance != null &&
            !InventoryPanelUI.Instance.IsScreenPointInside(
                eventData.position,
                eventData.pressEventCamera);

        if (droppedOutside)
            inventory?.DropEquippedItemToWorld(slotType, slotNumber);

        ItemDragContext.CancelDrag();
    }

    public void RestoreAfterDrag()
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (ItemDragContext.InventorySource != null)
        {
            bool equippedItem = inventory != null &&
                inventory.EquipFromInventory(
                    ItemDragContext.InventorySource.InventoryIndex,
                    slotType,
                    slotNumber);

            if (equippedItem)
                ItemDragContext.MarkDropHandled();

            return;
        }

        EquipmentSlotUI source = ItemDragContext.EquipmentSource;
        if (source == null) return;

        if (source == this)
        {
            ItemDragContext.MarkDropHandled();
            return;
        }

        if (slotType == EquipmentSlotType.Ring &&
            source.slotType == EquipmentSlotType.Ring &&
            equipment != null)
        {
            equipment.SwapRings();
            ItemDragContext.MarkDropHandled();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            inventory?.UnequipToInventory(slotType, slotNumber);
    }

    public static void SetHighlights(bool visible, EquippableItemDefinition dragged)
    {
        foreach (EquipmentSlotUI slot in FindObjectsByType<EquipmentSlotUI>(FindObjectsSortMode.None))
        {
            if (slot.validHighlight != null)
            {
                slot.validHighlight.enabled =
                    visible &&
                    dragged != null &&
                    dragged.EquipmentSlot == slot.slotType;
            }
        }
    }
}
