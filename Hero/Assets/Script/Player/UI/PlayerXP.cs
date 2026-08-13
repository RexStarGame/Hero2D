using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    [Serializable]
    private class XpLevelRange
    {
        [Min(1)] public int fromLevel = 1;
        [Min(1)] public int toLevel = 10;
        [Min(0)] public int xpIncreasePerLevel = 100;

        public bool Contains(int playerLevel)
        {
            return playerLevel >= fromLevel && playerLevel <= toLevel;
        }
    }

    public int level = 1;
    public int xp = 0;
    public int xpToNextLevel = 100;
    public int abilityPoints = 0;

    [Header("Level XP Requirements")]
    [Tooltip("XP required at level 1 before any per-level increases are applied.")]
    [Min(1)]
    [SerializeField] private int startingXpRequirement = 100;

    [Tooltip("The increase applied when a new level in this range is reached. " +
             "For example, reaching level 11 uses the range containing level 11.")]
    [SerializeField] private List<XpLevelRange> xpLevelRanges = new List<XpLevelRange>
    {
        new XpLevelRange { fromLevel = 1, toLevel = 10, xpIncreasePerLevel = 100 },
        new XpLevelRange { fromLevel = 11, toLevel = 20, xpIncreasePerLevel = 145 },
        new XpLevelRange { fromLevel = 21, toLevel = 30, xpIncreasePerLevel = 200 },
        new XpLevelRange { fromLevel = 31, toLevel = 40, xpIncreasePerLevel = 278 }
    };

    [Tooltip("Used for levels that are not covered by a range, including levels above the final range.")]
    [Min(0)]
    [SerializeField] private int fallbackXpIncreasePerLevel = 278;

    [Header("Death Penalty")]
    [Tooltip("Percentage of current-level XP lost on death. Completed levels are never lost.")]
    [Range(0f, 100f)]
    [SerializeField] private float deathXpLossPercent = 10f;

    [Header("Equipment XP Bonus")]
    [Tooltip("Auto-finds the player's equipment if empty. Only equipped item modifiers affect kill XP.")]
    [SerializeField] private PlayerEquipment equipment;

    private float fractionalKillXp;

    public event Action ProgressChanged;

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
        NotifyProgressChanged();
    }

    public void AddXP(int amount)
    {
        if (amount <= 0)
            return;

        xp += amount;

        ProcessLevelUps();
        SaveProgress();
        NotifyProgressChanged();
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
        NotifyProgressChanged();
    }

    /// <summary>
    /// Removes the configured percentage of only the XP collected toward the
    /// next level. Fractional XP loss rounds down and completed levels are never
    /// touched.
    /// </summary>
    public int ApplyDeathPenaltyAndSave()
    {
        float lossPercent = Mathf.Clamp(deathXpLossPercent, 0f, 100f);
        int lostXp = Mathf.FloorToInt(xp * (lossPercent / 100f));
        xp -= lostXp;
        SaveProgress();
        NotifyProgressChanged();

        Debug.Log($"Death penalty ({lossPercent:0.##}%): lost {lostXp} current-level XP. " +
                  $"Level {level} remains secured with {xp}/{xpToNextLevel} XP.");
        return lostXp;
    }

    public void SaveProgress()
    {
        PlayerProgressSave.SavePlayer(this);
    }

    public void RefreshProgressUI()
    {
        NotifyProgressChanged();
    }

    public int GetXpRequiredForLevel(int targetLevel)
    {
        int safeTargetLevel = Mathf.Max(1, targetLevel);
        long requiredXp = Mathf.Max(1, startingXpRequirement);

        for (int reachedLevel = 2; reachedLevel <= safeTargetLevel; reachedLevel++)
        {
            requiredXp += GetXpIncreaseForReachedLevel(reachedLevel);

            if (requiredXp >= int.MaxValue)
                return int.MaxValue;
        }

        return (int)requiredXp;
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

    private void NotifyProgressChanged()
    {
        ProgressChanged?.Invoke();
    }

    private void LevelUp()
    {
        level++;
        abilityPoints++;
        xpToNextLevel = GetXpRequiredForLevel(level);

        Debug.Log("Leveled up! Level " + level +
                  " | Ability Points: " + abilityPoints);
    }

    private int GetXpIncreaseForReachedLevel(int reachedLevel)
    {
        if (xpLevelRanges != null)
        {
            for (int i = 0; i < xpLevelRanges.Count; i++)
            {
                XpLevelRange range = xpLevelRanges[i];
                if (range != null && range.Contains(reachedLevel))
                    return Mathf.Max(0, range.xpIncreasePerLevel);
            }
        }

        return Mathf.Max(0, fallbackXpIncreasePerLevel);
    }

    private void OnValidate()
    {
        startingXpRequirement = Mathf.Max(1, startingXpRequirement);
        fallbackXpIncreasePerLevel = Mathf.Max(0, fallbackXpIncreasePerLevel);

        if (xpLevelRanges == null)
            return;

        for (int i = 0; i < xpLevelRanges.Count; i++)
        {
            XpLevelRange range = xpLevelRanges[i];
            if (range == null)
                continue;

            range.fromLevel = Mathf.Max(1, range.fromLevel);
            range.toLevel = Mathf.Max(range.fromLevel, range.toLevel);
            range.xpIncreasePerLevel = Mathf.Max(0, range.xpIncreasePerLevel);
        }
    }
}
