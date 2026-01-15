// PlayerHealth.cs
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float health;

    [Header("UI")]
    public Slider healthSlider;

    [Header("Game Over")]
    public GameOverMenu gameOverManager;

    [Header("Regen")]
    public float baseRegen = 0f;
    public float regenPerLevel = 0.5f;
    public int regenLevel = 0;

    [Header("Max Health Upgrade")]
    public int maxHealthLevel = 0;
    public int maxHealthUpgradeAmount = 50;
    public int maxHealthUpgradeMaxLevel = 10;

    private DashDoge dashDoge;

    void Start()
    {
        dashDoge = GetComponent<DashDoge>();

        // Start fuld HP
        health = maxHealth;

        // Find GameOverMenu hvis ikke sat i Inspector
        if (gameOverManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            gameOverManager = FindAnyObjectByType<GameOverMenu>();
#else
            gameOverManager = FindObjectOfType<GameOverMenu>();
#endif
            if (gameOverManager == null)
                Debug.LogError("Kunne ikke finde GameOverMenu i scenen!");
        }

        UpdateHealthUI(true);
    }

    void Update()
    {
        Regenerate();
    }

    void Regenerate()
    {
        if (health >= maxHealth) return;

        float regenAmount = (baseRegen + regenLevel * regenPerLevel) * Time.deltaTime;
        float oldHealth = health;

        health = Mathf.Min(health + regenAmount, maxHealth);

        if (!Mathf.Approximately(oldHealth, health))
            UpdateHealthUI(false);
    }

    public void UpgradeMaxHealth()
    {
        if (maxHealthLevel >= maxHealthUpgradeMaxLevel)
        {
            Debug.Log("Max Health is already maxed!");
            return;
        }

        maxHealthLevel++;
        maxHealth += maxHealthUpgradeAmount;

        // Heal fuldt ved upgrade
        health = maxHealth;

        UpdateHealthUI(true);
        Debug.Log("Max Health upgraded! New max: " + maxHealth);
    }

    public void TakeDamage(float damage)
    {
        // Invulnerability under dash
        if (dashDoge != null && dashDoge.IsInvulnerable())
            return;

        health -= damage;
        health = Mathf.Clamp(health, 0f, maxHealth);

        UpdateHealthUI(false);

        if (health <= 0f)
            Die();
    }

    void Die()
    {
        Debug.Log("Player died!");

        if (gameOverManager != null)
            gameOverManager.TriggerGameOver();
        else
            Debug.LogError("GameOverManager mangler på spilleren!");
    }

    private void UpdateHealthUI(bool updateMaxValue)
    {
        if (healthSlider == null) return;

        if (updateMaxValue)
            healthSlider.maxValue = maxHealth;

        healthSlider.value = health;
    }
}
