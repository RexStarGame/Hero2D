using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RingSlotUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    public enum SlotKind { Inventory, Equipment }

    public static RingSlotUI DraggedSlot { get; private set; }

    [SerializeField] private SlotKind slotKind;
    [SerializeField] private int slotIndex;
    [SerializeField] private RingInventory inventory;
    [SerializeField] private PlayerEquipment equipment;
    [SerializeField] private Image icon;
    [SerializeField] private Image highlight;
    [SerializeField] private TMP_Text emptyLabel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Color validDropColor = new Color(0.13f, 0.77f, 0.37f, 0.55f);

    private void OnEnable()
    {
        if (inventory != null) inventory.InventoryChanged += Refresh;
        if (equipment != null) equipment.EquipmentChanged += Refresh;
        Refresh();
    }

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.InventoryChanged -= Refresh;
        if (equipment != null) equipment.EquipmentChanged -= Refresh;
    }

    public void Refresh()
    {
        RingDefinition ring = GetRing();
        if (icon != null)
        {
            icon.sprite = ring == null ? null : ring.Icon;
            icon.enabled = ring != null;
        }
        if (emptyLabel != null) emptyLabel.gameObject.SetActive(ring == null);
        if (highlight != null) highlight.enabled = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GetRing() == null) return;
        DraggedSlot = this;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        ShowEquipmentTargets(true);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ShowEquipmentTargets(false);
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        DraggedSlot = null;
        Refresh();
    }

    public void OnDrop(PointerEventData eventData)
    {
        RingSlotUI source = DraggedSlot;
        if (source == null || source == this) return;

        if (slotKind == SlotKind.Equipment && source.slotKind == SlotKind.Inventory)
            inventory.EquipFromInventory(source.slotIndex, slotIndex);
        else if (slotKind == SlotKind.Inventory && source.slotKind == SlotKind.Equipment)
            inventory.UnequipToInventory(source.slotIndex);
        else if (slotKind == SlotKind.Equipment && source.slotKind == SlotKind.Equipment)
            equipment.SwapRingSlots();

        Refresh();
        source.Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (slotKind == SlotKind.Equipment) inventory.UnequipToInventory(slotIndex);
        else
        {
            int target = equipment.GetRing(0) == null ? 0 : (equipment.GetRing(1) == null ? 1 : 0);
            inventory.EquipFromInventory(slotIndex, target);
        }
    }

    private RingDefinition GetRing()
    {
        return slotKind == SlotKind.Inventory ? inventory?.GetRing(slotIndex) : equipment?.GetRing(slotIndex);
    }

    private void ShowEquipmentTargets(bool show)
    {
        RingSlotUI[] slots = FindObjectsByType<RingSlotUI>(FindObjectsSortMode.None);
        foreach (RingSlotUI slot in slots)
        {
            if (slot.slotKind != SlotKind.Equipment || slot.highlight == null) continue;
            slot.highlight.color = validDropColor;
            slot.highlight.enabled = show;
        }
    }
}
