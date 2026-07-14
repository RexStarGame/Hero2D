using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemSlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Image rarityBorder;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ItemHoverSource hover;

    private PlayerInventory inventory;

    public int InventoryIndex { get; private set; }
    public ItemDefinition Item => inventory == null ? null : inventory.GetItem(InventoryIndex);

    public void Bind(PlayerInventory source, int index)
    {
        inventory = source;
        InventoryIndex = index;

        ItemDefinition item = Item;
        if (icon != null)
        {
            icon.enabled = item != null;
            icon.sprite = item == null ? null : item.Icon;
        }

        if (rarityBorder != null)
            rarityBorder.enabled = item != null;

        if (hover != null)
            hover.SetItem(item, false);
    }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Item == null) return;

        ItemDragContext.Begin(this);
        ItemDragVisualUI.Instance?.Show(Item, eventData.position);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        EquipmentSlotUI.SetHighlights(true, Item as EquippableItemDefinition);
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
            inventory?.DropItemToWorld(InventoryIndex);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        EquipmentSlotUI.SetHighlights(false, null);
        ItemDragVisualUI.Instance?.Hide();
        ItemDragContext.Clear();
    }

    public void OnDrop(PointerEventData eventData)
    {
        EquipmentSlotUI source = ItemDragContext.EquipmentSource;
        if (source == null || inventory == null) return;

        if (inventory.UnequipToInventory(source.SlotType, source.SlotNumber))
            ItemDragContext.MarkDropHandled();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            inventory?.QuickEquip(InventoryIndex);
    }
}
