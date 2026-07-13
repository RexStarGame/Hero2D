using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
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

    private void Awake() { if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>(); }
    private void OnEnable() { if (equipment != null) equipment.EquipmentChanged += Refresh; Refresh(); }
    private void OnDisable() { if (equipment != null) equipment.EquipmentChanged -= Refresh; }
    public void Refresh()
    {
        EquippableItemDefinition item = Item;
        if (icon != null) { icon.enabled = item != null; icon.sprite = item == null ? null : item.Icon; }
        if (emptyLabel != null) emptyLabel.gameObject.SetActive(item == null);
        if (validHighlight != null) validHighlight.enabled = false;
        if (hover != null) hover.SetItem(item, true);
    }
    public void OnBeginDrag(PointerEventData e) { if (Item == null) return; ItemDragContext.Begin(this); if (canvasGroup != null) canvasGroup.blocksRaycasts = false; SetHighlights(true, Item); }
    public void OnEndDrag(PointerEventData e) { if (canvasGroup != null) canvasGroup.blocksRaycasts = true; SetHighlights(false, null); ItemDragContext.Clear(); }
    public void OnDrop(PointerEventData e)
    {
        if (ItemDragContext.InventorySource != null)
            inventory.EquipFromInventory(ItemDragContext.InventorySource.InventoryIndex, slotType, slotNumber);
        else if (ItemDragContext.EquipmentSource != null && slotType == EquipmentSlotType.Ring && ItemDragContext.EquipmentSource.slotType == EquipmentSlotType.Ring)
            equipment.SwapRings();
    }
    public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Right) inventory.UnequipToInventory(slotType, slotNumber); }
    public static void SetHighlights(bool visible, EquippableItemDefinition dragged)
    {
        foreach (EquipmentSlotUI slot in FindObjectsByType<EquipmentSlotUI>(FindObjectsSortMode.None))
            if (slot.validHighlight != null) slot.validHighlight.enabled = visible && dragged != null && dragged.EquipmentSlot == slot.slotType;
    }
}
