using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemSlotUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
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
        inventory = source; InventoryIndex = index;
        ItemDefinition item = Item;
        icon.enabled = item != null; icon.sprite = item == null ? null : item.Icon;
        if (rarityBorder != null) rarityBorder.enabled = item != null;
        if (hover != null) hover.SetItem(item, false);
    }
    private void Awake() { if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>(); }
    public void OnBeginDrag(PointerEventData e) { if (Item == null) return; ItemDragContext.Begin(this); if (canvasGroup != null) canvasGroup.blocksRaycasts = false; EquipmentSlotUI.SetHighlights(true, Item as EquippableItemDefinition); }
    public void OnEndDrag(PointerEventData e) { if (canvasGroup != null) canvasGroup.blocksRaycasts = true; EquipmentSlotUI.SetHighlights(false, null); ItemDragContext.Clear(); }
    public void OnDrop(PointerEventData e)
    {
        if (ItemDragContext.EquipmentSource != null)
            inventory.UnequipToInventory(ItemDragContext.EquipmentSource.SlotType, ItemDragContext.EquipmentSource.SlotNumber);
    }
    public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Right) inventory?.QuickEquip(InventoryIndex); }
}
