using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public PlayerHealth player;
    public Slider slider;

    private void Start()
    {
        slider.maxValue = player.maxHealth;
    }
    void Update()
    {
        slider.value = player.health;
    }
}
