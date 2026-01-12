using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public int level = 1;
    public int xp = 0;
    public int xpToNextLevel = 100;

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
        xpToNextLevel += 50;
        Debug.Log("Leveled up! Now level " + level);
    }

}
