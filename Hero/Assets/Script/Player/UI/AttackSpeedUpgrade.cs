using UnityEngine;
using TMPro;

public class AttackSpeedUpgrade : MonoBehaviour
{
    [Header("References")]
    public PlayerXP playerXP;           // Reference to PlayerXP
    public PlayerAttack playerAttack;   // Reference to PlayerAttack
    public TMP_Text levelText;          // Optional: show upgrade level

    [Header("Upgrade Settings")]
    public int maxLevel = 10;
    public int cost = 1;                // 1 ability point per upgrade

    [Tooltip("How much the attack cooldown is reduced per upgrade level.")]
    public float cooldownReductionPerLevel = 0.05f;

    [Tooltip("Lowest attack cooldown allowed (prevents going to 0).")]
    public float minAttackCooldown = 0.10f;

    void Start()
    {
        UpdateText();
    }

    public void BuyAttackSpeedUpgrade()
    {
        if (playerAttack == null || playerXP == null)
        {
            Debug.LogWarning("Missing references (playerAttack or playerXP).");
            return;
        }

        if (playerAttack.attackSpeedLevel >= maxLevel)
        {
            Debug.Log("Attack speed upgrade is maxed!");
            return;
        }

        if (playerXP.abilityPoints < cost)
        {
            Debug.Log("Not enough ability points!");
            return;
        }

        playerXP.abilityPoints -= cost;
        playerAttack.UpgradeAttackSpeed(cooldownReductionPerLevel, minAttackCooldown);

        UpdateText();
    }

    void UpdateText()
    {
        if (levelText != null && playerAttack != null)
        {
            levelText.text = "Atk Speed " + playerAttack.attackSpeedLevel;
        }
    }
}
