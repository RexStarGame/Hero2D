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
        RestoreBossCheckpoint();
    }

    private void RestoreBossCheckpoint()
    {
        int startingLevel = Mathf.Max(1, level);
        int startingAbilityPoints = Mathf.Max(0, abilityPoints);
        int checkpointLevel = Mathf.Max(startingLevel, BossLevelCheckpoint.Level);

        // Start exactly at the secured checkpoint. Run XP and previously chosen
        // ability upgrades are deliberately not restored.
        level = checkpointLevel;
        xp = 0;
        xpToNextLevel = 100 + ((checkpointLevel - 1) * 50);

        // Restore the points naturally earned up to this level, allowing the
        // player to create a fresh ability build each run.
        abilityPoints = startingAbilityPoints + (checkpointLevel - startingLevel);
    }


    public void AddXP(int amount)
    {
        if (amount <= 0)
            return;

        xp += amount;

        ProcessLevelUps();
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
