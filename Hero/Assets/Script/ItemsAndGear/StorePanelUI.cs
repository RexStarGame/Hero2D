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
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button buyButton;

    [Header("Controls")]
    [SerializeField] private KeyCode openKey = KeyCode.B;
    [SerializeField] private bool pauseWhenOpen = true;

    private readonly List<StoreItemSlotUI> spawned = new List<StoreItemSlotUI>();
    private readonly StringBuilder details = new StringBuilder(256);
    private StoreItemSlotUI selectedSlot;
    private bool isOpen;

    public IReadOnlyList<ItemDefinition> Stock => stock;

    private void Awake()
    {
        AutoFindPlayer();

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
            if (item == null)
                continue;

            StoreItemSlotUI slot = Instantiate(slotTemplate, gridRoot);
            slot.gameObject.SetActive(true);
            slot.Bind(this, item);
            spawned.Add(slot);
        }

        RefreshBalance();
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
            AppendStat("Damage", stats.Damage);
            AppendStat("Defense", stats.Defense);
            AppendStat("Regeneration", stats.Regeneration, "/s");
            AppendStat("Life Steal", stats.LifeSteal * 100f, "%");
            AppendStat("Critical Chance", stats.CriticalChance * 100f, "%");
            AppendStat("Attack Speed", stats.AttackSpeed * 100f, "%");
            AppendStat("Movement Speed", stats.MovementSpeed * 100f, "%");
        }

        if (detailsText != null)
            detailsText.text = details.ToString();

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
            detailsText.text = string.Empty;

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
}
