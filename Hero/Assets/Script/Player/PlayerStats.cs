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

    private float timer;
    private readonly StringBuilder sb = new StringBuilder(512);

    private void Awake()
    {
        AutoFind();
        ForceUpdate();
    }

    private void OnEnable()
    {
        AutoFind();
        ForceUpdate();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= refreshInterval)
        {
            timer = 0f;
            ForceUpdate();
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
    }

    private void ForceUpdate()
    {
        if (statsText == null) return;

        sb.Clear();

        // Ability Points
        sb.AppendLine(playerXP != null
            ? $"Ability Points: {playerXP.abilityPoints}"
            : "Ability Points: N/A");

        sb.AppendLine("");

        // Health + Regen
        if (playerHealth != null)
        {
            float regenPerSec = (playerHealth.baseRegen + playerHealth.regenLevel * playerHealth.regenPerLevel);
            sb.AppendLine($"HP: {playerHealth.health:0}/{playerHealth.maxHealth:0}  (MaxHP Lv {playerHealth.maxHealthLevel})");
            sb.AppendLine($"Regen: Lv {playerHealth.regenLevel}  ({regenPerSec:0.00}/s)");
        }
        else
        {
            sb.AppendLine("HP: N/A");
            sb.AppendLine("Regen: N/A");
        }

        sb.AppendLine("");

        // Damage
        if (damageUpgrade != null)
            sb.AppendLine($"Damage: {damageUpgrade.Damage}  (Lv {damageUpgrade.DamageLevel})");
        else
            sb.AppendLine("Damage: N/A");

        sb.AppendLine("");

        // Attack stats
        if (playerAttack != null)
        {
            // Attack speed
            float cd = playerAttack.AttackCooldown; // seconds between attacks
            float aps = cd > 0.0001f ? (1f / cd) : 0f;

            sb.AppendLine($"Attack Speed: Lv {playerAttack.attackSpeedLevel}");
            sb.AppendLine($"Cooldown: {cd:0.00}s  (~{aps:0.00} atk/s)");

            // Life steal
            float lsPct = playerAttack.LifeStealPercent * 100f;
            sb.AppendLine($"Life Steal: Lv {playerAttack.lifeStealLevel}  ({lsPct:0.000}% per hit)");

            // Crit
            float critPct = playerAttack.CritChance * 100f;
            sb.AppendLine($"Crit: Lv {playerAttack.critLevel}  ({critPct:0.00}% chance, x{playerAttack.CritMultiplier:0.00})");
        }
        else
        {
            sb.AppendLine("Attack: N/A");
        }

        statsText.text = sb.ToString();
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
