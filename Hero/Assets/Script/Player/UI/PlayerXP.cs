using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public int level = 1;
    public int xp = 0;
    public int xpToNextLevel = 100;
    public int abilityPoints = 0;

    private void Awake()
    {
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
        xp += amount;

        while (xp >= xpToNextLevel)
        {
            xp -= xpToNextLevel;
            LevelUp();
        }
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
