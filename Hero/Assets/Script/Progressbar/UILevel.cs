using UnityEngine;
using TMPro;

public class LevelTextUI : MonoBehaviour
{
    public PlayerXP player;
    public TMP_Text levelText;

    void Update()
    {
        levelText.text = "Level " + player.level;
    }
}
