using UnityEngine;

/// <summary>
/// Persists only the highest level secured by defeating a milestone boss.
/// Run XP and chosen ability upgrades are deliberately not saved here.
/// </summary>
public static class BossLevelCheckpoint
{
    private const string PlayerPrefsKey = "Hero2D.BossLevelCheckpoint";

    public static int Level => Mathf.Max(1, PlayerPrefs.GetInt(PlayerPrefsKey, 1));

    /// <summary>
    /// Raises the checkpoint, but never lowers an existing one.
    /// Returns true when a new checkpoint was saved.
    /// </summary>
    public static bool TryUnlock(int checkpointLevel)
    {
        checkpointLevel = Mathf.Max(1, checkpointLevel);
        if (checkpointLevel <= Level)
            return false;

        PlayerPrefs.SetInt(PlayerPrefsKey, checkpointLevel);
        PlayerPrefs.Save();

        Debug.Log($"Boss checkpoint unlocked: Level {checkpointLevel}");
        return true;
    }

    /// <summary>
    /// Intended for development/testing or an explicit New Game action.
    /// </summary>
    public static void Reset()
    {
        PlayerPrefs.DeleteKey(PlayerPrefsKey);
        PlayerPrefs.Save();
    }
}
