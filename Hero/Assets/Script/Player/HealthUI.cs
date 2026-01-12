using UnityEngine;
using TMPro;

public class HealthTextUI : MonoBehaviour
{
    public PlayerHealth player;   // Reference to your PlayerHealth script
    public TMP_Text healthText;   // Reference to the TMP Text

    void Update()
    {
        // Display current health rounded to integer
        healthText.text = Mathf.RoundToInt(player.health) + " / " + Mathf.RoundToInt(player.maxHealth);
    }
}
