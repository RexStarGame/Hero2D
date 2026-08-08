using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorePanelUI : MonoBehaviour
{
    [Header("Store Inventory - add your items here")]
    [SerializeField] private List<ItemDefinition> stock = new List<ItemDefinition>();

    [Header("Player")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private PlayerXP playerXP;

    [Header("UI")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Transform gridRoot;
    [SerializeField] private StoreItemSlotUI slotTemplate;
    [SerializeField] private Image previewIcon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private ScrollRect detailsScrollRect;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button buyButton;
    [SerializeField] private StoreCategoryFilterUI categoryFilterUI;

    [Header("Store Category Layout")]
    [Tooltip("Bottom-left normalized anchor of the category bar inside StoreDynamicUI.")]
    [SerializeField] private Vector2 categoryBarAnchorMin = new Vector2(0.055f, 0.795f);
    [Tooltip("Top-right normalized anchor of the category bar inside StoreDynamicUI.")]
    [SerializeField] private Vector2 categoryBarAnchorMax = new Vector2(0.60f, 0.84f);
    [Min(0f)] [SerializeField] private float categoryButtonSpacing = 3f;
    [Min(1f)] [SerializeField] private float categoryButtonMinWidth = 58f;
    [Min(1f)] [SerializeField] private float categoryButtonMinHeight = 24f;

    [Header("Controls")]
    [SerializeField] private KeyCode openKey = KeyCode.B;
    [SerializeField] private bool pauseWhenOpen = true;

    private readonly List<StoreItemSlotUI> spawned = new List<StoreItemSlotUI>();
    private readonly StringBuilder details = new StringBuilder(256);
    private StoreItemSlotUI selectedSlot;
    private StoreCategory activeCategory = StoreCategory.All;
    private bool isOpen;

    public IReadOnlyList<ItemDefinition> Stock => stock;
    public Vector2 CategoryBarAnchorMin => categoryBarAnchorMin;
    public Vector2 CategoryBarAnchorMax => categoryBarAnchorMax;
    public float CategoryButtonSpacing => categoryButtonSpacing;
    public float CategoryButtonMinWidth => categoryButtonMinWidth;
    public float CategoryButtonMinHeight => categoryButtonMinHeight;

    private void Awake()
    {
        AutoFindPlayer();
        EnsureDetailsScrollArea();
        EnsureCategoryFilters();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuySelected);
            buyButton.onClick.AddListener(BuySelected);
        }

        SetVisible(false);
        Rebuild();
    }

    private void OnEnable()
    {
        if (wallet != null)
            wallet.GoldChanged += RefreshBalance;

        RefreshBalance();
    }

    private void OnDisable()
    {
        if (wallet != null)
            wallet.GoldChanged -= RefreshBalance;

        Close();
    }

    private void Update()
    {
        if (Input.GetKeyDown(openKey))
        {
            if (isOpen)
                Close();
            else
                Open();
        }
        else if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Open()
    {
        if (isOpen || !MenuLock.CanOpen(MenuOwner.Store))
            return;

        MenuLock.Set(MenuOwner.Store);
        isOpen = true;
        Rebuild();
        SetVisible(true);

        if (pauseWhenOpen)
            Time.timeScale = 0f;
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        ItemTooltipUI.Instance?.Hide();
        SetVisible(false);
        MenuLock.Clear(MenuOwner.Store);

        if (pauseWhenOpen)
            Time.timeScale = 1f;
    }

    public void Rebuild()
    {
        foreach (StoreItemSlotUI slot in spawned)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        spawned.Clear();
        selectedSlot = null;
        ClearSelection();

        if (gridRoot == null || slotTemplate == null)
            return;

        slotTemplate.gameObject.SetActive(false);

        foreach (ItemDefinition item in stock)
        {
            if (item == null || !MatchesActiveCategory(item))
                continue;

            StoreItemSlotUI slot = Instantiate(slotTemplate, gridRoot);
            slot.gameObject.SetActive(true);
            slot.Bind(this, item);
            spawned.Add(slot);
        }

        RefreshBalance();
    }

    public void SetCategory(StoreCategory category)
    {
        activeCategory = category;
        categoryFilterUI?.SetSelected(category);
        Rebuild();
    }

    public void Select(StoreItemSlotUI slot)
    {
        if (slot == null || slot.Item == null)
            return;

        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        selectedSlot = slot;
        selectedSlot.SetSelected(true);
        ShowSelection(slot.Item);
    }

    public void BuySelected()
    {
        ItemDefinition item = selectedSlot == null ? null : selectedSlot.Item;
        if (item == null || inventory == null || wallet == null)
            return;

        if (playerXP != null && playerXP.level < item.RequiredLevel)
        {
            ShowFeedback($"Requires level {item.RequiredLevel}", false);
            return;
        }

        if (inventory.Items.Count >= inventory.Capacity)
        {
            ShowFeedback("Inventory is full", false);
            return;
        }

        int price = Mathf.Max(0, item.GoldValue);
        if (!wallet.TrySpend(price))
        {
            ShowFeedback("Not enough gold", false);
            return;
        }

        if (!inventory.Add(item))
        {
            wallet.AddGold(price);
            ShowFeedback("Could not add item", false);
            return;
        }

        ShowFeedback($"Purchased {item.ItemName}", true);
        RefreshBalance();
    }

    public void SetStock(List<ItemDefinition> items)
    {
        stock = items ?? new List<ItemDefinition>();
        Rebuild();
    }

    private void ShowSelection(ItemDefinition item)
    {
        if (previewIcon != null)
        {
            previewIcon.sprite = item.Icon;
            previewIcon.enabled = item.Icon != null;
            previewIcon.preserveAspect = true;
        }

        if (titleText != null)
            titleText.text = item.ItemName;

        details.Clear();
        details.Append(item.Rarity).Append(" · Required level ").Append(item.RequiredLevel);

        if (!string.IsNullOrWhiteSpace(item.Description))
            details.Append("\n\n").Append(item.Description);

        if (item is EquippableItemDefinition gear)
        {
            ItemStatModifiers stats = gear.StatModifiers;
            details.Append("\n\n<color=#C7D2FE>STAT BONUSES</color>");
            AppendStat("Max Health", stats.MaxHealth);
            AppendDamageRange("Damage", stats.MinimumDamage, stats.MaximumDamage, true);
            AppendStat("Defense", stats.Defense);
            AppendStat("Regeneration", stats.Regeneration, "/s");
            AppendStat("Life Steal", stats.LifeSteal * 100f, "%");
            AppendStat("Critical Chance", stats.CriticalChance * 100f, "%");
            AppendStat("Attack Speed", stats.AttackSpeed * 100f, "%");
            AppendStat("Movement Speed", stats.MovementSpeed * 100f, "%");
        }

        if (item is WeaponDefinition weapon)
            AppendDamageRange("Base damage", weapon.MinimumBaseDamage, weapon.MaximumBaseDamage, false);

        if (detailsText != null)
        {
            detailsText.text = details.ToString();
            RefreshDetailsScroll();
        }

        if (priceText != null)
            priceText.text = $"Price: <color=#FFD166>{item.GoldValue} gold</color>";

        if (buyButton != null)
            buyButton.interactable = true;

        ShowFeedback(string.Empty, true);
    }

    private void AppendStat(string label, float value, string suffix = "")
    {
        if (Mathf.Approximately(value, 0f))
            return;

        string sign = value > 0f ? "+" : string.Empty;
        string color = value > 0f ? "#22C55E" : "#EF4444";
        details.Append("\n").Append(label).Append(": <color=").Append(color).Append(">")
            .Append(sign).Append(value.ToString("0.##")).Append(suffix).Append("</color>");
    }

    private void AppendDamageRange(string label, float minimum, float maximum, bool signed)
    {
        maximum = Mathf.Max(minimum, maximum);
        if (Mathf.Approximately(minimum, 0f) && Mathf.Approximately(maximum, 0f))
            return;

        string color = maximum > 0f ? "#22C55E" : "#EF4444";
        string minSign = signed && minimum > 0f ? "+" : string.Empty;
        string maxSign = signed && maximum > 0f ? "+" : string.Empty;
        details.Append("\n").Append(label).Append(": <color=").Append(color).Append(">")
            .Append(minSign).Append(minimum.ToString("0.##")).Append("–")
            .Append(maxSign).Append(maximum.ToString("0.##")).Append("</color>");
    }

    private void ClearSelection()
    {
        if (previewIcon != null)
        {
            previewIcon.sprite = null;
            previewIcon.enabled = false;
        }

        if (titleText != null)
            titleText.text = "Select an item";

        if (detailsText != null)
        {
            detailsText.text = string.Empty;
            RefreshDetailsScroll();
        }

        if (priceText != null)
            priceText.text = string.Empty;

        if (feedbackText != null)
            feedbackText.text = string.Empty;

        if (buyButton != null)
            buyButton.interactable = false;
    }

    private void RefreshBalance()
    {
        if (balanceText != null)
            balanceText.text = wallet == null
                ? "Gold: N/A"
                : $"Gold: <color=#FFD166>{wallet.Gold}</color>";
    }

    private void ShowFeedback(string message, bool success)
    {
        if (feedbackText == null)
            return;

        string color = success ? "#22C55E" : "#EF4444";
        feedbackText.text = string.IsNullOrEmpty(message)
            ? string.Empty
            : $"<color={color}>{message}</color>";
    }

    private void EnsureDetailsScrollArea()
    {
        if (detailsText == null)
            return;

        if (detailsScrollRect == null)
            detailsScrollRect = detailsText.GetComponentInParent<ScrollRect>();

        if (detailsScrollRect == null)
            detailsScrollRect = CreateDetailsScrollArea(detailsText);

        if (detailsScrollRect == null)
            return;

        detailsScrollRect.horizontal = false;
        detailsScrollRect.vertical = true;
        detailsScrollRect.movementType = ScrollRect.MovementType.Clamped;
        detailsScrollRect.inertia = true;
        detailsScrollRect.decelerationRate = 0.135f;
        detailsScrollRect.scrollSensitivity = 24f;
        detailsScrollRect.verticalScrollbarVisibility =
            ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        detailsText.enableWordWrapping = true;
        detailsText.overflowMode = TextOverflowModes.Overflow;
        detailsText.raycastTarget = false;
        detailsText.margin = Vector4.zero;
    }

    private void EnsureCategoryFilters()
    {
        if (categoryFilterUI == null)
            categoryFilterUI = GetComponentInChildren<StoreCategoryFilterUI>(true);

        if (categoryFilterUI == null && gridRoot is RectTransform gridRect)
            categoryFilterUI = StoreCategoryFilterUI.Create(this, gridRect);

        categoryFilterUI?.Initialize(this, activeCategory);
    }

    private bool MatchesActiveCategory(ItemDefinition item)
    {
        switch (activeCategory)
        {
            case StoreCategory.Swords:
                return item is WeaponDefinition weapon &&
                    weapon.WeaponType == WeaponType.Sword;
            case StoreCategory.Boots:
                return item is BootsDefinition;
            case StoreCategory.Helmets:
                return item is HelmetDefinition;
            case StoreCategory.Chests:
                return item is ChestArmorDefinition;
            case StoreCategory.Rings:
                return item is RingDefinition;
            case StoreCategory.Necklaces:
                return item is NecklaceDefinition;
            default:
                return true;
        }
    }

    private ScrollRect CreateDetailsScrollArea(TMP_Text text)
    {
        RectTransform textRect = text.rectTransform;
        RectTransform oldParent = textRect.parent as RectTransform;
        if (oldParent == null)
            return null;

        int siblingIndex = textRect.GetSiblingIndex();

        GameObject scrollObject = new GameObject(
            "SelectedItemDetailsScrollView",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.SetParent(oldParent, false);
        scrollRectTransform.SetSiblingIndex(siblingIndex);
        CopyRectTransform(textRect, scrollRectTransform);

        Image scrollRaycastArea = scrollObject.GetComponent<Image>();
        scrollRaycastArea.color = new Color(0f, 0f, 0f, 0f);
        scrollRaycastArea.raycastTarget = true;

        GameObject viewportObject = new GameObject(
            "Viewport", typeof(RectTransform), typeof(RectMask2D));
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.SetParent(scrollRectTransform, false);
        Stretch(viewport);

        GameObject contentObject = new GameObject(
            "Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(2, 2, 2, 2);
        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        textRect.SetParent(content, false);
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
        textRect.localScale = Vector3.one;

        Scrollbar scrollbar = CreateVerticalScrollbar(scrollRectTransform);
        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarSpacing = 3f;
        return scroll;
    }

    private static Scrollbar CreateVerticalScrollbar(RectTransform parent)
    {
        GameObject scrollbarObject = new GameObject(
            "Scrollbar Vertical",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.SetParent(parent, false);
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = Vector2.one;
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.anchoredPosition = Vector2.zero;
        scrollbarRect.sizeDelta = new Vector2(9f, 0f);

        Image background = scrollbarObject.GetComponent<Image>();
        background.color = new Color(0.04f, 0.05f, 0.065f, 0.82f);

        GameObject slidingAreaObject = new GameObject("Sliding Area", typeof(RectTransform));
        RectTransform slidingArea = slidingAreaObject.GetComponent<RectTransform>();
        slidingArea.SetParent(scrollbarRect, false);
        Stretch(slidingArea, 1f);

        GameObject handleObject = new GameObject(
            "Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.SetParent(slidingArea, false);
        Stretch(handleRect);

        Image handle = handleObject.GetComponent<Image>();
        handle.color = new Color(0.78f, 0.54f, 0.18f, 0.95f);
        handle.raycastTarget = true;

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.value = 1f;
        scrollbar.size = 1f;
        return scrollbar;
    }

    private void RefreshDetailsScroll()
    {
        if (detailsScrollRect == null || detailsText == null)
            return;

        detailsText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

        if (detailsScrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(detailsScrollRect.content);

        Canvas.ForceUpdateCanvases();
        detailsScrollRect.StopMovement();
        detailsScrollRect.verticalNormalizedPosition = 1f;
    }

    private static void CopyRectTransform(RectTransform source, RectTransform destination)
    {
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.pivot = source.pivot;
        destination.anchoredPosition = source.anchoredPosition;
        destination.sizeDelta = source.sizeDelta;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;
    }

    private static void Stretch(RectTransform rect, float padding = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
        rect.localScale = Vector3.one;
    }

    private void SetVisible(bool visible)
    {
        if (panelGroup == null)
            return;

        panelGroup.alpha = visible ? 1f : 0f;
        panelGroup.interactable = visible;
        panelGroup.blocksRaycasts = visible;
    }

    private void AutoFindPlayer()
    {
#if UNITY_2023_1_OR_NEWER
        if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
        if (wallet == null) wallet = FindAnyObjectByType<PlayerWallet>();
        if (playerXP == null) playerXP = FindAnyObjectByType<PlayerXP>();
#else
        if (inventory == null) inventory = FindObjectOfType<PlayerInventory>();
        if (wallet == null) wallet = FindObjectOfType<PlayerWallet>();
        if (playerXP == null) playerXP = FindObjectOfType<PlayerXP>();
#endif
    }

    private void OnValidate()
    {
        categoryBarAnchorMin.x = Mathf.Clamp01(categoryBarAnchorMin.x);
        categoryBarAnchorMin.y = Mathf.Clamp01(categoryBarAnchorMin.y);
        categoryBarAnchorMax.x = Mathf.Clamp01(categoryBarAnchorMax.x);
        categoryBarAnchorMax.y = Mathf.Clamp01(categoryBarAnchorMax.y);

        if (categoryBarAnchorMax.x < categoryBarAnchorMin.x)
            categoryBarAnchorMax.x = categoryBarAnchorMin.x;
        if (categoryBarAnchorMax.y < categoryBarAnchorMin.y)
            categoryBarAnchorMax.y = categoryBarAnchorMin.y;

        categoryButtonSpacing = Mathf.Max(0f, categoryButtonSpacing);
        categoryButtonMinWidth = Mathf.Max(1f, categoryButtonMinWidth);
        categoryButtonMinHeight = Mathf.Max(1f, categoryButtonMinHeight);

        if (Application.isPlaying && categoryFilterUI != null)
            categoryFilterUI.ApplyLayout();
    }
}
