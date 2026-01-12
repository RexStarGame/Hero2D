using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public int level = 1;
    public int xp = 0;
    public int xpToNextLevel = 100;
    public int abilityPoints = 0;


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
