using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public int level = 1;
    public int xp = 0;
    public int xpToNextLevel = 100;
    public int abilityPoints = 0;

    [Header("Equipment XP Bonus")]
    [Tooltip("Auto-finds the player's equipment if empty. Only equipped item modifiers affect kill XP.")]
    [SerializeField] private PlayerEquipment equipment;

    private float fractionalKillXp;

    public float EquipmentKillXpBonus
    {
        get
        {
            AutoFindEquipment();
            return equipment != null ? Mathf.Max(0f, equipment.GetExperienceGainBonus()) : 0f;
        }
    }

    private void Awake()
    {
        AutoFindEquipment();

        int inspectorLevel = Mathf.Max(1, level);
        int inspectorAbilityPoints = Mathf.Max(0, abilityPoints);
        PlayerProgressSave.RestorePlayer(this, inspectorLevel, inspectorAbilityPoints);
    }


    public void AddXP(int amount)
    {
        if (amount <= 0)
            return;

        xp += amount;

        ProcessLevelUps();
        SaveProgress();
    }

    /// <summary>
    /// Awards XP for a defeated enemy and applies only the currently equipped
    /// gear bonus. Fractions are retained so small percentage bonuses are not
    /// lost to integer rounding over multiple kills.
    /// </summary>
    public void AddKillXP(int baseAmount)
    {
        if (baseAmount <= 0)
            return;

        float total = baseAmount * (1f + EquipmentKillXpBonus) + fractionalKillXp;
        int awardedXp = Mathf.FloorToInt(total);
        fractionalKillXp = total - awardedXp;
        xp += awardedXp;

        ProcessLevelUps();
        SaveProgress();
    }

    /// <summary>
    /// Removes 10% of only the XP collected toward the next level. Completed
    /// levels are never touched. The loss rounds up so 1-9 XP still has a cost.
    /// </summary>
    public int ApplyDeathPenaltyAndSave()
    {
        int lostXp = Mathf.Min(xp, Mathf.CeilToInt(xp * 0.10f));
        xp -= lostXp;
        SaveProgress();

        Debug.Log($"Death penalty: lost {lostXp} current-level XP. " +
                  $"Level {level} remains secured with {xp}/{xpToNextLevel} XP.");
        return lostXp;
    }

    public void SaveProgress()
    {
        PlayerProgressSave.SavePlayer(this);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveProgress();
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    private void ProcessLevelUps()
    {

        while (xp >= xpToNextLevel)
        {
            xp -= xpToNextLevel;
            LevelUp();
        }
    }

    private void AutoFindEquipment()
    {
        if (equipment == null)
            equipment = FindAnyObjectByType<PlayerEquipment>();
    }

    void LevelUp()
    {
        level++;
        abilityPoints++;          // <-- give 1 point
        xpToNextLevel += 50;

        Debug.Log("Leveled up! Level " + level +
                  " | Ability Points: " + abilityPoints);
    }


}
