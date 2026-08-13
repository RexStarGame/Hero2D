using UnityEngine;
using UnityEngine.EventSystems;

public class ItemHoverSource : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private bool equipped;

    public void SetItem(ItemDefinition value, bool isEquipped)
    {
        item = value;
        equipped = isEquipped;

        if (item == null)
            ItemTooltipUI.Instance?.Hide();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (item != null && !ItemDragContext.IsDragging && RingSlotUI.DraggedSlot == null)
            ItemTooltipUI.Instance?.Show(item, e.position, equipped);
    }

    public void OnPointerMove(PointerEventData e)
    {
        if (item != null && !ItemDragContext.IsDragging)
            ItemTooltipUI.Instance?.Move(e.position);
    }

    public void OnPointerExit(PointerEventData e)
    {
        ItemTooltipUI.Instance?.Hide();
    }

    private void OnDisable()
    {
        // Unity may disable the inventory before it sends OnPointerExit.
        ItemTooltipUI.Instance?.Hide();
    }
}
