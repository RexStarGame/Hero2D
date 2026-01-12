using UnityEngine;
using UnityEngine.UI; // Needed for UI

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float health;
    public GameOverMenu gameOverManager;
    [Header("UI")]
    public Slider healthSlider; // Drag your UI Slider here in the Inspector

    void Start()
    {
        // Hvis referencen er tom, så led efter den i hele scenen
        if (gameOverManager == null)
        {
            gameOverManager = GameObject.FindAnyObjectByType<GameOverMenu>();

            if (gameOverManager == null)
            {
                Debug.LogError("Kunne overhovedet ikke finde et GameOverMenu script i scenen!");
            }
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

        // 1. Kald funktionen i GameOverMenu scriptet
        if (gameOverManager != null)
        {
            gameOverManager.TriggerGameOver();
        }
        else
        {
            Debug.LogError("GameOverManager mangler på spilleren!");
        }

        // 2. Du kan også deaktivere spillerens styring her
        // GetComponent<PlayerMovement>().enabled = false;

        // 3. (Valgfrit) Skjul spilleren eller spil en død-animation
        // gameObject.SetActive(false); 
    }
}
