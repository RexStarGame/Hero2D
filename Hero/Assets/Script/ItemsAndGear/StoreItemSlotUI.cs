using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StoreItemSlotUI : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler,
    IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Image selectionHighlight;

    private StorePanelUI store;
    private ItemDefinition item;

    public ItemDefinition Item => item;

    public void Bind(StorePanelUI owner, ItemDefinition value)
    {
        store = owner;
        item = value;

        if (icon != null)
        {
            icon.sprite = item == null ? null : item.Icon;
            icon.enabled = item != null;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
            selectionHighlight.enabled = selected;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && item != null)
            store?.Select(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item != null)
            ItemTooltipUI.Instance?.Show(item, eventData.position, false);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        ItemTooltipUI.Instance?.Move(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUI.Instance?.Hide();
    }

    private void OnDisable()
    {
        ItemTooltipUI.Instance?.Hide();
    }
}
