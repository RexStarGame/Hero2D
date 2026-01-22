// LifeStealUpgrade.cs
using UnityEngine;
using TMPro;

public class LifeStealUpgrade : MonoBehaviour
{
    [Header("References")]
    public PlayerXP playerXP;
    public PlayerHealth playerHealth;
    public PlayerAttack playerAttack;
    public TMP_Text levelText;

    [Header("Upgrade Settings")]
    public int maxLevel = 10;
    public int cost = 1;

    [Tooltip("Life steal added per level. 0.0002 = 0.02%")]
    public float lifeStealPerLevel = 0.0002f; // 0.02%

    void Start()
    {
        UpdateText();
    }

    public void BuyLifeStealUpgrade()
    {
        if (playerXP == null || playerHealth == null || playerAttack == null)
        {
            Debug.LogWarning("Missing references for LifeStealUpgrade.");
            return;
        }

        if (playerAttack.lifeStealLevel >= maxLevel)
        {
            Debug.Log("Life steal upgrade is maxed!");
            return;
        }

        if (playerXP.abilityPoints < cost)
        {
            Debug.Log("Not enough ability points!");
            return;
        }

        playerXP.abilityPoints -= cost;
        playerAttack.UpgradeLifeSteal(lifeStealPerLevel);

        UpdateText();
    }

    void UpdateText()
    {
        if (levelText != null && playerAttack != null)
            levelText.text = "+";
    }
}
