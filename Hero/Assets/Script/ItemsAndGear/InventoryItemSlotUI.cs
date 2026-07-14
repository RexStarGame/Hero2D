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
    [Min(0f)] [SerializeField] private float iconPadding = 1f;

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
            FitIconToSlot();
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

        FitIconToSlot();
    }

    private void FitIconToSlot()
    {
        if (icon == null)
            return;

        RectTransform iconRect = icon.rectTransform;
        float padding = Mathf.Max(0f, iconPadding);

        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.offsetMin = new Vector2(padding, padding);
        iconRect.offsetMax = new Vector2(-padding, -padding);
        iconRect.localScale = Vector3.one;

        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Item == null) return;

        ItemTooltipUI.Instance?.Hide();
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

        ItemDragContext.CancelDrag();
    }

    public void RestoreAfterDrag()
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;
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
