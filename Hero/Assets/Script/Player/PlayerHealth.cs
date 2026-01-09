using UnityEngine;
using UnityEngine.UI; // Needed for UI

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float health;

    [Header("UI")]
    public Slider healthSlider; // Drag your UI Slider here in the Inspector

    void Start()
    {
        health = maxHealth;

        // Initialize the slider
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;

        // Clamp health so it never goes below 0
        health = Mathf.Clamp(health, 0, maxHealth);

        // Update the slider
        if (healthSlider != null)
        {
            healthSlider.value = health;
        }

        Debug.Log("Player took damage. Current health: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player died!");
        // Handle player death here
    }
}
