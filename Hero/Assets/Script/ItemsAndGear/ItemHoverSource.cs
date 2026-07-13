using UnityEngine;
using UnityEngine.EventSystems;

public class ItemHoverSource : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private bool equipped;
    public void SetItem(ItemDefinition value, bool isEquipped) { item = value; equipped = isEquipped; }
    public void OnPointerEnter(PointerEventData e) { if (RingSlotUI.DraggedSlot == null) ItemTooltipUI.Instance?.Show(item, e.position, equipped); }
    public void OnPointerMove(PointerEventData e) { ItemTooltipUI.Instance?.Move(e.position); }
    public void OnPointerExit(PointerEventData e) { ItemTooltipUI.Instance?.Hide(); }
}
