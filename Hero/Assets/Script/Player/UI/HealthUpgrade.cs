using UnityEngine;
using TMPro;

public class HealthUpgrade : MonoBehaviour
{
    public PlayerXP playerXP;           // Reference to PlayerXP
    public PlayerHealth playerHealth;   // Reference to PlayerHealth
    public TMP_Text levelText;          // Optional: show upgrade level

    public int maxLevel = 10;
    public int cost = 1;                // 1 ability point per upgrade

    void Start()
    {
        UpdateText();
    }

    public void BuyHealthUpgrade()
    {
        if (playerHealth.maxHealthLevel >= maxLevel)
        {
            Debug.Log("Health upgrade is maxed!");
            return;
        }

        if (playerXP.abilityPoints < cost)
        {
            Debug.Log("Not enough ability points!");
            return;
        }

        playerXP.abilityPoints -= cost;
        playerHealth.UpgradeMaxHealth();

        UpdateText();
    }

    void UpdateText()
    {
        if (levelText != null)
        {
            levelText.text = "Health " + playerHealth.maxHealthLevel;
        }
    }
}
