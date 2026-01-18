// CriticalDamage.cs  (this is your CriticalDamageUpgrade script)
// Same pattern as HealthUpgrade / LifeStealUpgrade.
using UnityEngine;
using TMPro;

public class CriticalDamage : MonoBehaviour
{
    [Header("References")]
    public PlayerXP playerXP;
    public PlayerAttack playerAttack;
    public TMP_Text levelText;

    [Header("Upgrade Settings")]
    public int maxLevel = 10;
    public int cost = 1;

    [Tooltip("Crit chance added per level. Example: 0.01 = +1% per level.")]
    public float critChancePerLevel = 0.01f;

    void Start()
    {
        UpdateText();
    }

    public void BuyCriticalDamageUpgrade()
    {
        if (playerXP == null || playerAttack == null)
        {
            Debug.LogWarning("Missing references for CriticalDamage upgrade.");
            return;
        }

        if (playerAttack.critLevel >= maxLevel)
        {
            Debug.Log("Critical Damage upgrade is maxed!");
            return;
        }

        if (playerXP.abilityPoints < cost)
        {
            Debug.Log("Not enough ability points!");
            return;
        }

        playerXP.abilityPoints -= cost;
        playerAttack.UpgradeCritChance(critChancePerLevel);

        UpdateText();
    }

    void UpdateText()
    {
        if (levelText != null && playerAttack != null)
            levelText.text = "Crit " + playerAttack.critLevel;
    }
}
