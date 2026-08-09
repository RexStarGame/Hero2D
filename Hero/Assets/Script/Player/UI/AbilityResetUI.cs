using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AbilityResetUI : MonoBehaviour
{
    private const string Prefix = "Hero2D.AbilityReset.";
    private const string WeekKey = Prefix + "Week";
    private const string CountKey = Prefix + "Count";
    private const int WeeklyResetLimit = 5;

    private static readonly int[] ResetPrices = { 0, 500, 2500, 9000, 20000 };

    private PlayerXP playerXP;
    private PlayerHealth playerHealth;
    private PlayerAttack playerAttack;
    private DamageUpgrade damageUpgrade;
    private PlayerWallet wallet;
    private InventorySaveSystem inventorySaveSystem;

    private Button resetButton;
    private TMP_Text buttonText;
    private Image buttonImage;
    private bool subscribed;

    public static void Ensure(
        GameObject upgradeMenu,
        PlayerXP xp,
        PlayerHealth health,
        PlayerAttack attack,
        DamageUpgrade damage)
    {
        if (upgradeMenu == null)
            return;

        AbilityResetUI ui = upgradeMenu.GetComponent<AbilityResetUI>();
        if (ui == null)
            ui = upgradeMenu.AddComponent<AbilityResetUI>();

        ui.Configure(xp, health, attack, damage);
    }

    private void Configure(
        PlayerXP xp,
        PlayerHealth health,
        PlayerAttack attack,
        DamageUpgrade damage)
    {
        playerXP = xp;
        playerHealth = health;
        playerAttack = attack;
        damageUpgrade = damage;

        FindSupportingSystems();
        EnsureButton();
        Subscribe();
        Refresh();
    }

    private void OnEnable()
    {
        FindSupportingSystems();
        EnsureButton();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void FindSupportingSystems()
    {
#if UNITY_2023_1_OR_NEWER
        if (playerXP == null) playerXP = FindAnyObjectByType<PlayerXP>();
        if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerAttack == null) playerAttack = FindAnyObjectByType<PlayerAttack>();
        if (damageUpgrade == null) damageUpgrade = FindAnyObjectByType<DamageUpgrade>();
        if (wallet == null) wallet = FindAnyObjectByType<PlayerWallet>();
        if (inventorySaveSystem == null) inventorySaveSystem = FindAnyObjectByType<InventorySaveSystem>();
#else
        if (playerXP == null) playerXP = FindObjectOfType<PlayerXP>();
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerAttack == null) playerAttack = FindObjectOfType<PlayerAttack>();
        if (damageUpgrade == null) damageUpgrade = FindObjectOfType<DamageUpgrade>();
        if (wallet == null) wallet = FindObjectOfType<PlayerWallet>();
        if (inventorySaveSystem == null) inventorySaveSystem = FindObjectOfType<InventorySaveSystem>();
#endif
    }

    private void EnsureButton()
    {
        if (resetButton != null)
            return;

        Transform existing = transform.Find("AbilityResetButton");
        GameObject buttonObject;
        if (existing != null)
        {
            buttonObject = existing.gameObject;
        }
        else
        {
            buttonObject = new GameObject(
                "AbilityResetButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(transform, false);
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 20f);
        rect.sizeDelta = new Vector2(310f, 58f);
        rect.localScale = Vector3.one;

        buttonImage = buttonObject.GetComponent<Image>();
        resetButton = buttonObject.GetComponent<Button>();
        resetButton.targetGraphic = buttonImage;
        resetButton.onClick.RemoveListener(TryResetAbilities);
        resetButton.onClick.AddListener(TryResetAbilities);

        buttonText = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (buttonText == null)
            buttonText = CreateButtonText(buttonObject.transform);
    }

    private TMP_Text CreateButtonText(Transform parent)
    {
        GameObject textObject = new GameObject(
            "Text (TMP)",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 4f);
        rect.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        TMP_Text template = GetComponentInChildren<TMP_Text>(true);
        if (template != null)
        {
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
        }

        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 22f;
        text.raycastTarget = false;
        return text;
    }

    public void TryResetAbilities()
    {
        RefreshWeek();

        int resetCount = PlayerPrefs.GetInt(CountKey, 0);
        if (resetCount >= WeeklyResetLimit)
        {
            Refresh();
            return;
        }

        int spentPoints = GetSpentAbilityPoints();
        if (spentPoints <= 0 || wallet == null)
        {
            Refresh();
            return;
        }

        int price = ResetPrices[resetCount];
        if (!wallet.TrySpend(price))
        {
            Refresh();
            return;
        }

        int refunded = PlayerProgressSave.ResetAbilityUpgrades(
            playerXP, damageUpgrade, playerHealth, playerAttack);

        if (refunded < 0)
        {
            wallet.AddGold(price);
            Refresh();
            return;
        }

        PlayerPrefs.SetInt(CountKey, resetCount + 1);
        PlayerPrefs.Save();
        inventorySaveSystem?.Save();

        Debug.Log($"Ability reset completed. Refunded {refunded} points for {price} coins.");
        Refresh();
    }

    private int GetSpentAbilityPoints()
    {
        if (damageUpgrade == null || playerHealth == null || playerAttack == null)
            return 0;

        return
            Mathf.Max(0, damageUpgrade.DamageLevel) +
            Mathf.Max(0, playerHealth.maxHealthLevel) +
            Mathf.Max(0, playerHealth.regenLevel) +
            Mathf.Max(0, playerAttack.attackSpeedLevel) +
            Mathf.Max(0, playerAttack.lifeStealLevel) +
            Mathf.Max(0, playerAttack.critLevel);
    }

    private void Refresh()
    {
        RefreshWeek();

        if (resetButton == null || buttonText == null)
            return;

        int resetCount = PlayerPrefs.GetInt(CountKey, 0);
        int spentPoints = GetSpentAbilityPoints();

        if (resetCount >= WeeklyResetLimit)
        {
            resetButton.interactable = false;
            buttonText.text = "RESET LIMIT REACHED\nAVAILABLE NEXT WEEK";
            SetButtonColor(new Color(0.30f, 0.30f, 0.34f, 1f));
            return;
        }

        if (spentPoints <= 0)
        {
            resetButton.interactable = false;
            buttonText.text = "RESET ABILITIES\nNO POINTS SPENT";
            SetButtonColor(new Color(0.30f, 0.30f, 0.34f, 1f));
            return;
        }

        int price = ResetPrices[resetCount];
        bool canAfford = wallet != null && wallet.CanAfford(price);
        resetButton.interactable = canAfford;

        if (price == 0)
        {
            buttonText.text = "RESET ABILITIES • FREE";
            SetButtonColor(new Color(0.18f, 0.55f, 0.30f, 1f));
        }
        else if (canAfford)
        {
            buttonText.text = $"RESET ABILITIES • {price:N0} COINS";
            SetButtonColor(new Color(0.72f, 0.48f, 0.12f, 1f));
        }
        else
        {
            int balance = wallet == null ? 0 : wallet.Gold;
            buttonText.text = $"RESET • {price:N0} COINS\nNEED {Mathf.Max(0, price - balance):N0} MORE";
            SetButtonColor(new Color(0.42f, 0.20f, 0.18f, 1f));
        }
    }

    private void RefreshWeek()
    {
        int currentWeek = GetUtcWeekStartId();
        int savedWeek = PlayerPrefs.GetInt(WeekKey, 0);
        if (savedWeek == currentWeek)
            return;

        PlayerPrefs.SetInt(WeekKey, currentWeek);
        PlayerPrefs.SetInt(CountKey, 0);
        PlayerPrefs.Save();
    }

    private static int GetUtcWeekStartId()
    {
        DateTime today = DateTime.UtcNow.Date;
        int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        DateTime monday = today.AddDays(-daysSinceMonday);
        return monday.Year * 10000 + monday.Month * 100 + monday.Day;
    }

    private void SetButtonColor(Color color)
    {
        if (buttonImage != null)
            buttonImage.color = color;
    }

    private void Subscribe()
    {
        if (subscribed || wallet == null)
            return;

        wallet.GoldChanged += Refresh;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || wallet == null)
            return;

        wallet.GoldChanged -= Refresh;
        subscribed = false;
    }
}
