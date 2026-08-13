using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum StoreCategory
{
    All,
    Swords,
    Boots,
    Helmets,
    Chests,
    Rings,
    Necklaces
}

[ExecuteAlways]
[DisallowMultipleComponent]
public class StoreCategoryFilterUI : MonoBehaviour
{
    private sealed class CategoryButton
    {
        public StoreCategory Category;
        public Image Background;
    }

    private static readonly StoreCategory[] Categories =
    {
        StoreCategory.All,
        StoreCategory.Swords,
        StoreCategory.Boots,
        StoreCategory.Helmets,
        StoreCategory.Chests,
        StoreCategory.Rings,
        StoreCategory.Necklaces
    };

    private static readonly Color NormalColor = new Color(0.035f, 0.045f, 0.06f, 0.88f);
    private static readonly Color SelectedColor = new Color(0.34f, 0.22f, 0.07f, 0.96f);
    private static readonly Color HighlightedTint = new Color(1f, 0.90f, 0.68f, 1f);
    private static readonly Color PressedTint = new Color(1f, 0.72f, 0.36f, 1f);

    private readonly List<CategoryButton> categoryButtons = new List<CategoryButton>();
    private StorePanelUI store;
    private bool built;

    private void OnEnable()
    {
        if (Application.isPlaying || HasAllCategoryButtons())
            return;

        store = GetComponentInParent<StorePanelUI>();
        if (store == null)
            return;

        BuildButtons();
        ApplyLayout();
        SetSelected(StoreCategory.All);
    }

    public static StoreCategoryFilterUI Create(StorePanelUI owner, RectTransform gridRoot)
    {
        RectTransform parent = gridRoot.parent as RectTransform;
        if (parent == null)
            return null;

        Transform existing = parent.Find("StoreCategoryBar");
        if (existing != null)
        {
            StoreCategoryFilterUI existingFilter =
                existing.GetComponent<StoreCategoryFilterUI>();
            return existingFilter != null
                ? existingFilter
                : existing.gameObject.AddComponent<StoreCategoryFilterUI>();
        }

        GameObject barObject = new GameObject(
            "StoreCategoryBar", typeof(RectTransform), typeof(HorizontalLayoutGroup),
            typeof(StoreCategoryFilterUI));
        RectTransform bar = barObject.GetComponent<RectTransform>();
        bar.SetParent(parent, false);
        bar.SetSiblingIndex(gridRoot.GetSiblingIndex() + 1);
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.offsetMin = Vector2.zero;
        bar.offsetMax = Vector2.zero;
        bar.localScale = Vector3.one;
        barObject.layer = parent.gameObject.layer;

        HorizontalLayoutGroup layout = barObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        StoreCategoryFilterUI filter = barObject.GetComponent<StoreCategoryFilterUI>();
        filter.Initialize(owner, StoreCategory.All);
        return filter;
    }

    public void Initialize(StorePanelUI owner, StoreCategory selected)
    {
        store = owner;

        if (!built)
            BuildButtons();

        ApplyLayout();
        SetSelected(selected);
    }

    public void ApplyLayout()
    {
        if (store == null)
            return;

        RectTransform bar = transform as RectTransform;
        if (bar != null)
        {
            bar.anchorMin = store.CategoryBarAnchorMin;
            bar.anchorMax = store.CategoryBarAnchorMax;
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = Vector2.zero;
        }

        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
            layout.spacing = store.CategoryButtonSpacing;

        foreach (LayoutElement element in GetComponentsInChildren<LayoutElement>(true))
        {
            element.minWidth = store.CategoryButtonMinWidth;
            element.minHeight = store.CategoryButtonMinHeight;
        }
    }

    public void SetSelected(StoreCategory selected)
    {
        foreach (CategoryButton button in categoryButtons)
        {
            if (button.Background != null)
                button.Background.color = button.Category == selected
                    ? SelectedColor
                    : NormalColor;
        }
    }

    private void BuildButtons()
    {
        built = true;
        categoryButtons.Clear();

        foreach (StoreCategory category in Categories)
            CreateButton(category);
    }

    private void CreateButton(StoreCategory category)
    {
        Transform existing = transform.Find(category.ToString());
        GameObject buttonObject = existing != null
            ? existing.gameObject
            : new GameObject(
                category.ToString(), typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (existing == null)
        {
            buttonRect.SetParent(transform, false);
            buttonRect.localScale = Vector3.one;
            buttonObject.layer = gameObject.layer;
        }

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.minWidth = store == null ? 58f : store.CategoryButtonMinWidth;
        layoutElement.minHeight = store == null ? 24f : store.CategoryButtonMinHeight;
        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = 1f;

        Image background = buttonObject.GetComponent<Image>();
        background.color = NormalColor;
        background.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = HighlightedTint;
        colors.pressedColor = PressedTint;
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.gray;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        StoreCategory captured = category;
        button.onClick.AddListener(() => store?.SetCategory(captured));

        if (buttonRect.Find("Icon") == null)
            CreateIcon(buttonRect, category);
        if (buttonRect.Find("Label") == null)
            CreateLabel(buttonRect, GetLabel(category));

        categoryButtons.Add(new CategoryButton
        {
            Category = category,
            Background = background
        });
    }

    private bool HasAllCategoryButtons()
    {
        foreach (StoreCategory category in Categories)
        {
            Transform button = transform.Find(category.ToString());
            if (button == null || button.Find("Icon") == null || button.Find("Label") == null)
                return false;
        }

        return true;
    }

    private static void CreateIcon(RectTransform parent, StoreCategory category)
    {
        GameObject iconObject = new GameObject(
            "Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(parent, false);
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(0.42f, 1f);
        iconRect.offsetMin = new Vector2(2f, 2f);
        iconRect.offsetMax = new Vector2(-1f, -2f);
        iconRect.localScale = Vector3.one;
        iconObject.layer = parent.gameObject.layer;

        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = Resources.Load<Sprite>(
            "StoreUI/CategoryIcons/" + category.ToString().ToLowerInvariant());
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = icon.sprite != null;
    }

    private static void CreateLabel(RectTransform parent, string value)
    {
        GameObject labelObject = new GameObject(
            "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(parent, false);
        labelRect.anchorMin = new Vector2(0.40f, 0f);
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = new Vector2(-2f, 0f);
        labelRect.localScale = Vector3.one;
        labelObject.layer = parent.gameObject.layer;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = 9f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.82f, 0.40f);
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
    }

    private static string GetLabel(StoreCategory category)
    {
        switch (category)
        {
            case StoreCategory.Helmets: return "HELMET";
            case StoreCategory.Chests: return "CHEST";
            case StoreCategory.Necklaces: return "NECKLACE";
            default: return category.ToString().ToUpperInvariant();
        }
    }
}
