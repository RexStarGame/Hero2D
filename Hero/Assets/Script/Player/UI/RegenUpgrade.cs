using UnityEngine;
using TMPro;

public class RegenUpgrade : MonoBehaviour
{
    public PlayerXP playerXP;
    public PlayerHealth playerHealth;

    public TMP_Text levelText; // NEW
    //public TMP_Text Level;
    public int maxLevel = 10;
    public int cost = 1;

    void Start()
    {
        UpdateLevelText();
    }

    public void BuyRegen()
    {
        if (playerHealth.regenLevel >= maxLevel)
        {
            Debug.Log("Regen is maxed out!");
            return;
        }

        if (playerXP.abilityPoints < cost)
        {
            Debug.Log("Not enough ability points!");
            return;
        }

        playerXP.abilityPoints -= cost;
        playerHealth.regenLevel++;

        Debug.Log("Bought Regen Level " + playerHealth.regenLevel);

        UpdateLevelText(); // NEW
    }

    void UpdateLevelText()
    {
        levelText.text = "+";
        //Level.text = playerHealth.regenLevel.ToString();
    }
}
