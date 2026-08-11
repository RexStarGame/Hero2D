using UnityEngine;
using TMPro;
using System.Text;

public class PlayerStats : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text defenseText;
    [Tooltip("Separate, low-noise text field for XP gained from equipped gear.")]
    [SerializeField] private TMP_Text experienceBonusText;
    [Tooltip("Separate text field for extra gold gained from kills through equipped gear.")]
    [SerializeField] private TMP_Text goldBonusText;

    [Header("Kill XP Text Layout")]
    [Tooltip("Position of the generated Kill XP row relative to DefenseText when no ExperienceBonusText is assigned.")]
    [SerializeField] private Vector2 experienceBonusOffsetFromDefense = new Vector2(0f, -34f);

    [Header("Kill Gold Text Layout")]
    [Tooltip("Position of the generated Kill Gold row relative to ExperienceBonusText when no GoldBonusText is assigned.")]
    [SerializeField] private Vector2 goldBonusOffsetFromExperience = new Vector2(0f, -34f);

    [Header("References (auto-find if null)")]
    [SerializeField] private PlayerXP playerXP;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private DamageUpgrade damageUpgrade;
    [SerializeField] private PlayerEquipment equipment;

    [Header("Refresh")]
    [SerializeField] private float refreshInterval = 0.15f;

    [Header("Theme Colors (hex)")]
    [SerializeField] private string headerColor = "#FFD166";
    [SerializeField] private string labelColor = "#C7D2FE";
    [SerializeField] private string valueColor = "#FFFFFF";
    [SerializeField] private string mutedColor = "#9CA3AF";
    [SerializeField] private string goodColor = "#22C55E";
    [SerializeField] private string warnColor = "#F59E0B";
    [SerializeField] private string badColor = "#EF4444";
    [SerializeField] private string blueColor = "#60A5FA";
    [Header("Upgrade Menu")]
    [SerializeField] private GameObject upgradeMenu;     // Drag dit UpgradeMenu panel her
    private bool upgradeMenuOpen = false;
    private float timer;
    private readonly StringBuilder sb = new StringBuilder(768);

    private void Awake()
    {
        AutoFind();
        EnsureExperienceBonusText();
        EnsureGoldBonusText();
        ForceUpdate();
        if (upgradeMenu != null)
        {
            upgradeMenu.SetActive(false);
            AbilityResetUI.Ensure(
                upgradeMenu, playerXP, playerHealth, playerAttack, damageUpgrade);
        }
    }

    private void OnEnable()
    {
        AutoFind();
        EnsureExperienceBonusText();
        EnsureGoldBonusText();
        ForceUpdate();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
            ToggleUpgradeMenu();

        if (upgradeMenuOpen && Input.GetKeyDown(KeyCode.Escape))
            ToggleUpgradeMenu();

        timer += Time.unscaledDeltaTime;
        if (timer >= refreshInterval)
        {
            timer = 0f;
            ForceUpdate();
        }
    }
    private void ToggleUpgradeMenu()
    {
        if (upgradeMenu == null)
        {
            UnityEngine.Debug.LogWarning("[PlayerStats] upgradeMenu reference mangler.");
            return;
        }

        bool wantsToOpen = !upgradeMenuOpen;

        // If trying to OPEN, block if Pause menu owns the lock
        if (wantsToOpen && !MenuLock.CanOpen(MenuOwner.Upgrade))
            return;

        upgradeMenuOpen = wantsToOpen;
        upgradeMenu.SetActive(upgradeMenuOpen);

        if (upgradeMenuOpen)
            MenuLock.Set(MenuOwner.Upgrade);
        else
            MenuLock.Clear(MenuOwner.Upgrade);

    }
    private void AutoFind()
    {
        if (playerXP == null) playerXP = FindAny<PlayerXP>();
        if (playerHealth == null) playerHealth = FindAny<PlayerHealth>();
        if (playerAttack == null) playerAttack = FindAny<PlayerAttack>();
        if (equipment == null) equipment = FindAny<PlayerEquipment>();

        if (damageUpgrade == null && playerAttack != null)
            damageUpgrade = playerAttack.DamageUpgrade;

        if (damageUpgrade == null)
            damageUpgrade = FindAny<DamageUpgrade>();
        if (upgradeMenu == null)
        {
            var go = GameObject.Find("UpgradeMenu");
             if (go != null) upgradeMenu = go;
        }

        if (experienceBonusText == null)
        {
            GameObject go = GameObject.Find("ExperienceBonusText");
            if (go != null) experienceBonusText = go.GetComponent<TMP_Text>();
        }

        if (goldBonusText == null)
        {
            GameObject go = GameObject.Find("GoldBonusText");
            if (go != null) goldBonusText = go.GetComponent<TMP_Text>();
        }
    }

    private void EnsureExperienceBonusText()
    {
        if (experienceBonusText != null || defenseText == null)
            return;

        Transform parent = defenseText.transform.parent;
        if (parent == null)
            return;

        GameObject textObject = Instantiate(defenseText.gameObject, parent);
        textObject.name = "ExperienceBonusText";
        textObject.transform.SetSiblingIndex(
            Mathf.Min(defenseText.transform.GetSiblingIndex() + 1, parent.childCount - 1));

        experienceBonusText = textObject.GetComponent<TMP_Text>();
        if (experienceBonusText == null)
        {
            Destroy(textObject);
            return;
        }

        experienceBonusText.raycastTarget = false;

        if (defenseText.transform is RectTransform defenseRect &&
            experienceBonusText.transform is RectTransform experienceRect)
        {
            experienceRect.anchoredPosition =
                defenseRect.anchoredPosition + experienceBonusOffsetFromDefense;
        }
    }

    private void EnsureGoldBonusText()
    {
        if (goldBonusText != null)
            return;

        TMP_Text template = experienceBonusText != null ? experienceBonusText : defenseText;
        if (template == null)
            return;

        Transform parent = template.transform.parent;
        if (parent == null)
            return;

        GameObject textObject = Instantiate(template.gameObject, parent);
        textObject.name = "GoldBonusText";
        textObject.transform.SetSiblingIndex(
            Mathf.Min(template.transform.GetSiblingIndex() + 1, parent.childCount - 1));

        goldBonusText = textObject.GetComponent<TMP_Text>();
        if (goldBonusText == null)
        {
            Destroy(textObject);
            return;
        }

        goldBonusText.raycastTarget = false;

        if (template.transform is RectTransform templateRect &&
            goldBonusText.transform is RectTransform goldRect)
        {
            goldRect.anchoredPosition =
                templateRect.anchoredPosition + goldBonusOffsetFromExperience;
        }
    }

    public void ResumeGame()
    {
        if (upgradeMenu == null) return;

        upgradeMenuOpen = false;
        upgradeMenu.SetActive(false);
        MenuLock.Clear(MenuOwner.Upgrade);
    }
    private void OnDisable()
    {
        if (upgradeMenuOpen)
        {
            MenuLock.Clear(MenuOwner.Upgrade);
        }
    }
    private void ForceUpdate()
    {
        UpdateDefenseText();
        UpdateExperienceBonusText();
        UpdateGoldBonusText();

        if (statsText == null) return;

        sb.Clear();

        // Title
        sb.AppendLine(Head("PLAYER STATS"));
        sb.AppendLine(Soft("──────────────"));
        sb.AppendLine();

        // Ability Points
        if (playerXP != null)
        {
            string apCol = playerXP.abilityPoints >= 5 ? goodColor : (playerXP.abilityPoints >= 1 ? warnColor : mutedColor);
            sb.AppendLine(Row("Ability Points", Color(playerXP.abilityPoints.ToString(), apCol)));
        }
        else
        {
            sb.AppendLine(Row("Ability Points", Soft("N/A")));
        }

        sb.AppendLine("<size=45%> </size>");

        // Health + Regen
        sb.AppendLine(Head("SURVIVABILITY"));
        sb.AppendLine("<size=35%> </size>");

        if (playerHealth != null)
        {
            float hpPct = playerHealth.MaxHealth > 0f ? playerHealth.health / playerHealth.MaxHealth : 0f;
            string hpCol = hpPct >= 0.60f ? goodColor : (hpPct >= 0.25f ? warnColor : badColor);

            sb.AppendLine(Row("HP",
                $"{Color($"{playerHealth.health:0}", hpCol)}/{Color($"{playerHealth.MaxHealth:0}", valueColor)}{Bonus(playerHealth.EquipmentHealthBonus, "0")}  {Soft($"(MaxHP Lv {playerHealth.maxHealthLevel})")}"));

            sb.AppendLine("<size=35%> </size>");

            float regenPerSec = playerHealth.baseRegen + playerHealth.regenLevel * playerHealth.regenPerLevel;
            string regenCol = regenPerSec > 0f ? blueColor : mutedColor;

            sb.AppendLine(Row("Regen",
                $"{Soft($"Lv {playerHealth.regenLevel}")}  {Color($"{regenPerSec:0.00}/s", regenCol)}{Bonus(playerHealth.EquipmentRegenBonus, "0.00", "/s")}"));
        }
        else
        {
            sb.AppendLine(Row("HP", Soft("N/A")));
            sb.AppendLine("<size=35%> </size>");
            sb.AppendLine(Row("Regen", Soft("N/A")));
        }

        sb.AppendLine("<size=55%> </size>");

        // Damage
        sb.AppendLine(Head("OFFENSE"));
        sb.AppendLine("<size=35%> </size>");

        if (damageUpgrade != null)
        {
            sb.AppendLine(Row("Damage",
                $"{Color($"{damageUpgrade.MinimumDamage}–{damageUpgrade.MaximumDamage}", valueColor)}  {Soft($"(Lv {damageUpgrade.DamageLevel})")}"));
        }
        else
        {
            sb.AppendLine(Row("Damage", Soft("N/A")));
        }

        sb.AppendLine("<size=35%> </size>");

        // Attack stats
        if (playerAttack != null)
        {
            float cd = playerAttack.AttackCooldown;
            float aps = cd > 0.0001f ? (1f / cd) : 0f;

            sb.AppendLine(Row("Attack Speed",
                $"{Soft($"Lv {playerAttack.attackSpeedLevel}")}  {Color($"{cd:0.00}s", valueColor)}{Bonus(playerAttack.EquipmentAttackSpeedBonus * 100f, "0.##", "% speed")} {Soft($"(~{aps:0.00}/s)")}"));

            sb.AppendLine("<size=35%> </size>");

            float lsPct = playerAttack.LifeStealPercent * 100f;
            string lsCol = lsPct > 0f ? goodColor : mutedColor;

            sb.AppendLine(Row("Life Steal",
                $"{Soft($"Lv {playerAttack.lifeStealLevel}")}  {Color($"{playerAttack.AbilityLifeStealPercent * 100f:0.###}%", lsCol)}{Bonus(playerAttack.EquipmentLifeStealPercent * 100f, "0.###", "%")} {Soft("per hit")}"));

            sb.AppendLine("<size=35%> </size>");

            float critPct = playerAttack.CritChance * 100f;
            string critCol = critPct >= 10f ? goodColor : (critPct > 0f ? warnColor : mutedColor);

            sb.AppendLine(Row("Crit",
                $"{Soft($"Lv {playerAttack.critLevel}")}  {Color($"{playerAttack.AbilityCritChance * 100f:0.##}%", critCol)}{Bonus(playerAttack.EquipmentCritChance * 100f, "0.##", "%")} {Soft("chance")}  {Color($"x{playerAttack.CritMultiplier:0.00}", valueColor)}"));
        }
        else
        {
            sb.AppendLine(Row("Attack", Soft("N/A")));
        }

        statsText.text = sb.ToString();
    }

    private void UpdateDefenseText()
    {
        if (defenseText == null)
            return;

        if (playerHealth == null)
        {
            defenseText.text = Row("Defense", Soft("N/A"));
            return;
        }

        float defense = playerHealth.Defense;
        defenseText.text = defense > 0f
            ? Row("Defense", BonusOnly(defense, "0"))
            : Row("Defense", Soft("0"));
    }

    private void UpdateExperienceBonusText()
    {
        if (experienceBonusText == null)
            return;

        float bonusPercent = equipment != null
            ? Mathf.Max(0f, equipment.GetExperienceGainBonus()) * 100f
            : 0f;

        experienceBonusText.text = bonusPercent > 0f
            ? Row("Kill XP Bonus", $"{Color($"+{bonusPercent:0.##}%", goodColor)} {Soft("per kill")}")
            : Row("Kill XP Bonus", $"{Soft("0% per kill")}");
    }

    private void UpdateGoldBonusText()
    {
        if (goldBonusText == null)
            return;

        float bonusPercent = equipment != null
            ? Mathf.Max(0f, equipment.GetGoldGainBonus()) * 100f
            : 0f;

        goldBonusText.text = bonusPercent > 0f
            ? Row("Kill Gold Bonus", $"{Color($"+{bonusPercent:0.##}%", goodColor)} {Soft("per kill")}")
            : Row("Kill Gold Bonus", $"{Soft("0% per kill")}");
    }

    // ---------- Formatting helpers ----------
    private string Head(string t) => $"<b><color={headerColor}>{t}</color></b>";
    private string Soft(string t) => $"<color={mutedColor}>{t}</color>";
    private string Label(string t) => $"<color={labelColor}>{t}</color>";
    private string Color(string t, string hex) => $"<color={hex}>{t}</color>";
    private string Bonus(float value, string format, string suffix = "") => value > 0f ? $"  {Color($"+{value.ToString(format)}{suffix}", goodColor)}" : string.Empty;
    private string BonusOnly(float value, string format) => Color($"+{value.ToString(format)}", goodColor);

    private string Row(string label, string value)
    {
        return $"{Label(label)}{Soft(": ")}{value}";
    }

    private static T FindAny<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindAnyObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
