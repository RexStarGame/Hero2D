using UnityEngine;
using UnityEngine.UI;

public class ItemDragVisualUI : MonoBehaviour
{
    public static ItemDragVisualUI Instance { get; private set; }

    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image icon;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Vector2 pointerOffset = new Vector2(18f, -18f);

    private void Awake()
    {
        Instance = this;
        if (rectTransform == null) rectTransform = transform as RectTransform;
        if (icon == null) icon = GetComponent<Image>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(ItemDefinition item, Vector2 screenPosition)
    {
        if (item == null || icon == null) return;
        icon.sprite = item.Icon;
        icon.enabled = item.Icon != null;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.9f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        transform.SetAsLastSibling();
        Move(screenPosition);
    }

    public void Move(Vector2 screenPosition)
    {
        if (rectTransform != null)
            rectTransform.position = screenPosition + pointerOffset;
    }

    public void Hide()
    {
        if (icon != null) icon.enabled = false;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }
}
