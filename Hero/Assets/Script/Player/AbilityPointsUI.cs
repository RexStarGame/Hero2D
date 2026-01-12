using UnityEngine;
using TMPro;

public class AbilityPointsUI : MonoBehaviour
{
    public PlayerXP player;
    public TMP_Text pointsText;

    void Update()
    {
        pointsText.text = "Ability Points: " + player.abilityPoints;
    }
}
