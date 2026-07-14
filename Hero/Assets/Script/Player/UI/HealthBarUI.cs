using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public PlayerHealth player;
    public Slider slider;

    private void Start()
    {
        UpdateHealthBar();
    }

    void Update()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (player == null || slider == null) return;

        // Refresh this continuously because equipping or removing gear changes MaxHealth.
        slider.maxValue = player.MaxHealth;
        slider.value = Mathf.Clamp(player.health, 0f, player.MaxHealth);
    }
}
