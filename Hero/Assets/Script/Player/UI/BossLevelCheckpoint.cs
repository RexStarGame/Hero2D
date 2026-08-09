using UnityEngine;

/// <summary>
/// Persists the highest level secured by a milestone boss and triggers the
/// broader player progression autosave after every boss defeat.
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
        bool unlockedNewCheckpoint = checkpointLevel > Level;

        if (unlockedNewCheckpoint)
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, checkpointLevel);
            PlayerPrefs.Save();
            Debug.Log($"Boss checkpoint unlocked: Level {checkpointLevel}");
        }

        // A boss defeat is always an autosave trigger, even when replaying an
        // already secured milestone.
        PlayerXP playerXP = Object.FindAnyObjectByType<PlayerXP>();
        if (playerXP != null)
            playerXP.SaveProgress();

        return unlockedNewCheckpoint;
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
