using UnityEngine;
using TMPro;

public class HealthTextUI : MonoBehaviour
{
    public PlayerHealth player;   // Reference to your PlayerHealth script
    public TMP_Text healthText;   // Reference to the TMP Text

    void Update()
    {
        if (player == null || healthText == null) return;

        // MaxHealth includes ability upgrades and all currently equipped gear.
        healthText.text = Mathf.RoundToInt(player.health) + " / " + Mathf.RoundToInt(player.MaxHealth);
    }
}
