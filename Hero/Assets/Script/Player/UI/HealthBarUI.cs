using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public PlayerHealth player;
    public Slider slider;

    void Update()
    {
        slider.value = player.health / player.maxHealth;
    }
}
