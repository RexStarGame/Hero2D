using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [Header("UI Slider")]
    [SerializeField] private Slider expSlider;

    [Header("Player XP Settings")]
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int xpToLevelUp = 100;

    [Header("Level")]
    [SerializeField] private int playerLevel = 1;

    void Start()
    {
        if (expSlider != null)
        {
            expSlider.maxValue = xpToLevelUp;
            expSlider.value = currentXP;
        }
    }

    // Call this to add XP
    public void AddXP(int amount)
    {
        currentXP += amount;
        if (currentXP >= xpToLevelUp)
        {
            LevelUp();
        }
        UpdateUI();
    }

    void LevelUp()
    {
        playerLevel++;
        currentXP -= xpToLevelUp;
        xpToLevelUp = Mathf.RoundToInt(xpToLevelUp * 1.2f); // Increase XP needed per level
        Debug.Log("Level Up! Current Level: " + playerLevel);
    }

    void UpdateUI()
    {
        if (expSlider != null)
        {
            expSlider.value = currentXP;
        }
    }
}
