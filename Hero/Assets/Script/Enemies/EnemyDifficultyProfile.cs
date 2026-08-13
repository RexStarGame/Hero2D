using System;
using UnityEngine;

public interface IDifficultyScaledEnemyDamage
{
    void SetDifficultyDamageMultiplier(float multiplier);
}

[DisallowMultipleComponent]
public class EnemyDifficultyProfile : MonoBehaviour
{
    private const int CurrentProfileVersion = 2;

    [Serializable]
    public struct DifficultyModifiers
    {
        [Tooltip("Extra maximum health in percent. 10 means +10%.")]
        [Min(-99f)] public float bonusHealthPercent;

        [Tooltip("Extra attack damage in percent. 5 means +5%.")]
        [Min(-99f)] public float bonusDamagePercent;

        public DifficultyModifiers(float healthPercent, float damagePercent)
        {
            bonusHealthPercent = healthPercent;
            bonusDamagePercent = damagePercent;
        }
    }

    [Header("Per-enemy / per-boss difficulty percentages")]
    [SerializeField] private DifficultyModifiers easy =
        new DifficultyModifiers(0f, 0f);
    [SerializeField] private DifficultyModifiers normal =
        new DifficultyModifiers(10f, 5f);
    [SerializeField] private DifficultyModifiers hard =
        new DifficultyModifiers(30f, 20f);

    [Tooltip("Keeps Extreme exactly halfway between this prefab's Hard and Nightmare values.")]
    [SerializeField] private bool calculateExtremeAsMidpoint = true;

    [SerializeField, HideInInspector] private int profileVersion =
        CurrentProfileVersion;

    [Tooltip("Used only when Calculate Extreme As Midpoint is disabled.")]
    [SerializeField] private DifficultyModifiers extreme =
        new DifficultyModifiers(45f, 30f);

    [SerializeField] private DifficultyModifiers nightmare =
        new DifficultyModifiers(60f, 40f);

    public float HealthMultiplier =>
        PercentToMultiplier(GetCurrentModifiers().bonusHealthPercent);

    public float DamageMultiplier =>
        PercentToMultiplier(GetCurrentModifiers().bonusDamagePercent);

    public float CurrentHealthBonusPercent =>
        GetCurrentModifiers().bonusHealthPercent;

    public float CurrentDamageBonusPercent =>
        GetCurrentModifiers().bonusDamagePercent;

    public float ScaleDamage(float baseDamage)
    {
        return Mathf.Max(0f, baseDamage) * DamageMultiplier;
    }

    public void ApplyToSpawnedDamage(GameObject spawnedObject)
    {
        if (spawnedObject == null)
            return;

        MonoBehaviour[] behaviours =
            spawnedObject.GetComponentsInChildren<MonoBehaviour>(true);
        float multiplier = DamageMultiplier;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IDifficultyScaledEnemyDamage scalableDamage)
                scalableDamage.SetDifficultyDamageMultiplier(multiplier);
        }
    }

    public static float GetDefaultDamageMultiplier()
    {
        switch (DifficultyManager.CurrentDifficulty)
        {
            case GameDifficulty.Normal: return 1.05f;
            case GameDifficulty.Hard: return 1.20f;
            case GameDifficulty.Extreme: return 1.30f;
            case GameDifficulty.Nightmare: return 1.40f;
            default: return 1f;
        }
    }

    private DifficultyModifiers GetCurrentModifiers()
    {
        switch (DifficultyManager.CurrentDifficulty)
        {
            case GameDifficulty.Normal: return normal;
            case GameDifficulty.Hard: return hard;
            case GameDifficulty.Extreme:
                return profileVersion < CurrentProfileVersion ||
                       calculateExtremeAsMidpoint
                    ? Midpoint(hard, nightmare)
                    : extreme;
            case GameDifficulty.Nightmare: return nightmare;
            default: return easy;
        }
    }

    private static DifficultyModifiers Midpoint(
        DifficultyModifiers lower, DifficultyModifiers upper)
    {
        return new DifficultyModifiers(
            (lower.bonusHealthPercent + upper.bonusHealthPercent) * 0.5f,
            (lower.bonusDamagePercent + upper.bonusDamagePercent) * 0.5f);
    }

    private static float PercentToMultiplier(float bonusPercent)
    {
        return Mathf.Max(0.01f, 1f + bonusPercent / 100f);
    }

    private void OnValidate()
    {
        if (profileVersion >= CurrentProfileVersion)
            return;

        // Existing four-mode prefabs have no serialized Extreme fields yet.
        // Migrate them safely to an exact Hard/Nightmare midpoint.
        calculateExtremeAsMidpoint = true;
        profileVersion = CurrentProfileVersion;
    }
}
