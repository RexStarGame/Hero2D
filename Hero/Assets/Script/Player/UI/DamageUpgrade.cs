using UnityEngine;
using TMPro;

public class DamageUpgrade : MonoBehaviour
{
    [Header("Damage Stats")]
    public int baseDamage = 10;
    public int damageLevel = 0;
    public int maxDamageLevel = 10;
    public int damagePerLevel = 5;

    [Header("Upgrade Cost")]
    public int cost = 1;
    public PlayerXP playerXP;

    [Header("UI")]
    public TMP_Text levelText;

    public int CurrentDamage
    {
        get
        {
            return baseDamage + (damageLevel * damagePerLevel);
        }
    }

    void Start()
    {
        UpdateLevelText();
    }

    // Called by UI Button
    public void BuyDamage()
    {
        if (damageLevel >= maxDamageLevel)
        {
            Debug.Log("Damage is maxed out!");
            return;
        }

        if (playerXP.abilityPoints < cost)
        {
            Debug.Log("Not enough ability points!");
            return;
        }

        playerXP.abilityPoints -= cost;
        damageLevel++;

        Debug.Log("Bought Damage Level " + damageLevel);

        UpdateLevelText();
    }

    void UpdateLevelText()
    {
        if (levelText != null)
            levelText.text = "Damage " + damageLevel;
    }
}
