using UnityEngine;

public static class DifficultyDebugTelemetry
{
    public static string LastDamageSource { get; private set; } = "No hit recorded yet";
    public static float LastBaseDamage { get; private set; }
    public static float LastDifficultyDamage { get; private set; }
    public static float LastDamageAfterDefense { get; private set; }
    public static bool LastDamageReachedPlayerHealth { get; private set; }
    public static float LastDamageTime { get; private set; } = -1f;

    public static void RecordEnemyDamage(
        Object source, float baseDamage, float difficultyDamage)
    {
        LastDamageSource = source != null ? source.name : "Unknown enemy attack";
        LastBaseDamage = Mathf.Max(0f, baseDamage);
        LastDifficultyDamage = Mathf.Max(0f, difficultyDamage);
        LastDamageAfterDefense = 0f;
        LastDamageReachedPlayerHealth = false;
        LastDamageTime = Time.unscaledTime;
    }

    public static void RecordDamageAfterDefense(
        float incomingDifficultyDamage, float damageAfterDefense)
    {
        LastDifficultyDamage = Mathf.Max(0f, incomingDifficultyDamage);
        LastDamageAfterDefense = Mathf.Max(0f, damageAfterDefense);
        LastDamageReachedPlayerHealth = true;
        LastDamageTime = Time.unscaledTime;
    }
}
