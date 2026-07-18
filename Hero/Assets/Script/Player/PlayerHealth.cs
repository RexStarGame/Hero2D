// PlayerHealth.cs
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float health;
    [SerializeField] private PlayerEquipment equipment;

    public float BaseAndAbilityMaxHealth => maxHealth;
    public float EquipmentHealthBonus => equipment == null ? 0f : equipment.GetHealthBonus();
    public float MaxHealth => Mathf.Max(1f, BaseAndAbilityMaxHealth + EquipmentHealthBonus);
    public float EquipmentRegenBonus => equipment == null ? 0f : equipment.GetRegenerationBonus();
    public float RegenPerSecond => Mathf.Max(0f, baseRegen + regenLevel * regenPerLevel + EquipmentRegenBonus);
    public float Defense => equipment == null ? 0f : Mathf.Max(0f, equipment.GetDefenseBonus());

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
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();
        if (equipment != null) equipment.EquipmentChanged += OnEquipmentChanged;
        dashDoge = GetComponent<DashDoge>();

        // Start fuld HP
        health = MaxHealth;

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
        if (health >= MaxHealth) return;

        float regenAmount = RegenPerSecond * Time.deltaTime;
        float oldHealth = health;

        health = Mathf.Min(health + regenAmount, MaxHealth);

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
        health = MaxHealth;

        UpdateHealthUI(true);
        Debug.Log("Max Health upgraded! New max: " + maxHealth);
    }

    public void TakeDamage(float damage)
    {
        // Central check covers projectiles, boss attacks, AoE and future damage sources.
        if (SafeZone2D.IsPlayerProtected(transform.position))
            return;

        // Invulnerability under dash
        if (dashDoge != null && dashDoge.IsInvulnerable())
            return;

        if (damage <= 0f) return;

        float incomingDifficultyDamage = damage;
        float reduction = Defense / (Defense + 100f);
        damage *= 1f - reduction;
        DifficultyDebugTelemetry.RecordDamageAfterDefense(
            incomingDifficultyDamage, damage);
        health = Mathf.Clamp(health - damage, 0f, MaxHealth);

        UpdateHealthUI(false);

        if (health <= 0f)
            Die();
    }

    // ✅ ADD THIS: used by Life Steal / healing items / etc.
    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        if (health <= 0f) return; // don't heal if dead

        float oldHealth = health;
        health = Mathf.Min(MaxHealth, health + amount);

        if (!Mathf.Approximately(oldHealth, health))
            UpdateHealthUI(false);
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
            healthSlider.maxValue = MaxHealth;

        healthSlider.value = health;
    }

    private void OnEquipmentChanged()
    {
        health = Mathf.Min(health, MaxHealth);
        UpdateHealthUI(true);
    }

    private void OnDestroy()
    {
        if (equipment != null) equipment.EquipmentChanged -= OnEquipmentChanged;
    }
}
