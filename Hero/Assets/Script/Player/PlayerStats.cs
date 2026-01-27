using UnityEngine;
using TMPro;
using System.Text;

public class PlayerStats : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statsText;

    [Header("References (auto-find if null)")]
    [SerializeField] private PlayerXP playerXP;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private DamageUpgrade damageUpgrade;

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
    [SerializeField] private bool pauseWhenOpen = true;  // Pause spil når menu er åben
    private bool upgradeMenuOpen = false;
    private float timer;
    private readonly StringBuilder sb = new StringBuilder(768);

    private void Awake()
    {
        AutoFind();
        ForceUpdate();
        if (upgradeMenu != null)
            upgradeMenu.SetActive(false);
    }

    private void OnEnable()
    {
        AutoFind();
        ForceUpdate();
    }

    private void Update()
    {
        // VIGTIGT: unscaledDeltaTime fortsætter selv når Time.timeScale = 0 (pause)
        if (Input.GetKeyDown(KeyCode.U))
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
           Debug.LogWarning("[PlayerStats] upgradeMenu reference mangler.");
           return;
        }

        upgradeMenuOpen = !upgradeMenuOpen;
        upgradeMenu.SetActive(upgradeMenuOpen);

        if (pauseWhenOpen)
        {
            //Time.timeScale = upgradeMenuOpen ? 0f : 1f;
            upgradeMenuOpen = true;
            upgradeMenu.SetActive(true);
            Time.timeScale = 0f; // pause indtil Resume-knap

        }


    }
    private void AutoFind()
    {
        if (playerXP == null) playerXP = FindAny<PlayerXP>();
        if (playerHealth == null) playerHealth = FindAny<PlayerHealth>();
        if (playerAttack == null) playerAttack = FindAny<PlayerAttack>();

        if (damageUpgrade == null && playerAttack != null)
            damageUpgrade = playerAttack.DamageUpgrade;

        if (damageUpgrade == null)
            damageUpgrade = FindAny<DamageUpgrade>();
        if (upgradeMenu == null)
        {
            var go = GameObject.Find("UpgradeMenu");
             if (go != null) upgradeMenu = go;
        }
    }
    public void ResumeGame()
    {
      if (upgradeMenu == null) return;

       upgradeMenuOpen = false;
       upgradeMenu.SetActive(false);
       Time.timeScale = 1f;
    }
    private void OnDisable()
    {
        // failsafe så du ikke bliver “låst” i pause hvis objektet disables
        if (upgradeMenuOpen)
            Time.timeScale = 1f;
    }
private void ForceUpdate()
    {
        if (statsText == null) return;

        sb.Clear();

        // Title
        sb.AppendLine(Head("PLAYER STATS"));
        sb.AppendLine(Soft("──────────────"));

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

        sb.AppendLine(Soft(" "));

        // Health + Regen
        sb.AppendLine(Head("SURVIVABILITY"));

        if (playerHealth != null)
        {
            float hpPct = (playerHealth.maxHealth > 0f) ? (playerHealth.health / playerHealth.maxHealth) : 0f;
            string hpCol = hpPct >= 0.60f ? goodColor : (hpPct >= 0.25f ? warnColor : badColor);

            sb.AppendLine(Row("HP",
                $"{Color($"{playerHealth.health:0}", hpCol)}/{Color($"{playerHealth.maxHealth:0}", valueColor)}  {Soft($"(MaxHP Lv {playerHealth.maxHealthLevel})")}"));

            float regenPerSec = (playerHealth.baseRegen + playerHealth.regenLevel * playerHealth.regenPerLevel);
            string regenCol = regenPerSec > 0f ? blueColor : mutedColor;

            sb.AppendLine(Row("Regen",
                $"{Soft($"Lv {playerHealth.regenLevel}")}  {Color($"{regenPerSec:0.00}/s", regenCol)}"));
        }
        else
        {
            sb.AppendLine(Row("HP", Soft("N/A")));
            sb.AppendLine(Row("Regen", Soft("N/A")));
        }

        sb.AppendLine(Soft(" "));

        // Damage
        sb.AppendLine(Head("OFFENSE"));

        if (damageUpgrade != null)
        {
            sb.AppendLine(Row("Damage",
                $"{Color($"{damageUpgrade.Damage}", valueColor)}  {Soft($"(Lv {damageUpgrade.DamageLevel})")}"));
        }
        else
        {
            sb.AppendLine(Row("Damage", Soft("N/A")));
        }

        // Attack stats
        if (playerAttack != null)
        {
            float cd = playerAttack.AttackCooldown;
            float aps = cd > 0.0001f ? (1f / cd) : 0f;

            sb.AppendLine(Row("Attack Speed",
                $"{Soft($"Lv {playerAttack.attackSpeedLevel}")}  {Color($"{cd:0.00}s", valueColor)} {Soft($"(~{aps:0.00}/s)")}"));

            float lsPct = playerAttack.LifeStealPercent * 100f;
            string lsCol = lsPct > 0f ? goodColor : mutedColor;
            sb.AppendLine(Row("Life Steal",
                $"{Soft($"Lv {playerAttack.lifeStealLevel}")}  {Color($"{lsPct:0.000}%", lsCol)} {Soft("per hit")}"));

            float critPct = playerAttack.CritChance * 100f;
            string critCol = critPct >= 10f ? goodColor : (critPct > 0f ? warnColor : mutedColor);
            sb.AppendLine(Row("Crit",
                $"{Soft($"Lv {playerAttack.critLevel}")}  {Color($"{critPct:0.00}%", critCol)} {Soft("chance")}  {Color($"x{playerAttack.CritMultiplier:0.00}", valueColor)}"));
        }
        else
        {
            sb.AppendLine(Row("Attack", Soft("N/A")));
        }

        statsText.text = sb.ToString();
    }

    // ---------- Formatting helpers ----------
    private string Head(string t) => $"<b><color={headerColor}>{t}</color></b>";
    private string Soft(string t) => $"<color={mutedColor}>{t}</color>";
    private string Label(string t) => $"<color={labelColor}>{t}</color>";
    private string Color(string t, string hex) => $"<color={hex}>{t}</color>";

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
