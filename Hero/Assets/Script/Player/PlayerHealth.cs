// PlayerHealth.cs
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float health;
    [SerializeField] private PlayerEquipment equipment;

    [Header("Guard")]
    [Tooltip("Base Guard trigger chance. Decimal: 0.05 means 5%.")]
    [Range(0f, 1f)] [SerializeField] private float baseGuardChance = 0f;
    [Tooltip("Base damage reduction when Guard triggers. Decimal: 0.005 means 0.5%.")]
    [Range(0f, 1f)] [SerializeField] private float baseGuardReduction = 0.005f;

    public float BaseAndAbilityMaxHealth => maxHealth;
    public float EquipmentHealthBonus => equipment == null ? 0f : equipment.GetHealthBonus();
    public float MaxHealth => Mathf.Max(1f, BaseAndAbilityMaxHealth + EquipmentHealthBonus);
    public float EquipmentRegenBonus => equipment == null ? 0f : equipment.GetRegenerationBonus();
    public float RegenPerSecond => Mathf.Max(0f, baseRegen + regenLevel * regenPerLevel + EquipmentRegenBonus);
    public float Defense => equipment == null ? 0f : Mathf.Max(0f, equipment.GetDefenseBonus());
    public float EquipmentGuardChance => equipment == null ? 0f : Mathf.Max(0f, equipment.GetGuardChanceBonus());
    public float EquipmentGuardReduction => equipment == null ? 0f : Mathf.Max(0f, equipment.GetGuardReductionBonus());
    public float GuardChance => Mathf.Clamp01(baseGuardChance + EquipmentGuardChance);
    public float GuardReduction => Mathf.Clamp01(baseGuardReduction + EquipmentGuardReduction);

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
    private float levelZeroMaxHealth;

    void Start()
    {
        levelZeroMaxHealth = Mathf.Max(1f, maxHealth);
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();
        if (equipment != null) equipment.EquipmentChanged += OnEquipmentChanged;
        dashDoge = GetComponent<DashDoge>();
        PlayerProgressSave.RestoreHealthUpgrades(this);

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

    public void RestoreUpgradeProgress(
        int savedMaxHealthLevel,
        int savedRegenLevel,
        float savedMaxHealth)
    {
        maxHealthLevel = Mathf.Clamp(savedMaxHealthLevel, 0, maxHealthUpgradeMaxLevel);
        regenLevel = Mathf.Max(0, savedRegenLevel);
        maxHealth = Mathf.Max(1f, savedMaxHealth);
    }

    public void ResetAbilityUpgradeProgress()
    {
        maxHealthLevel = 0;
        regenLevel = 0;
        maxHealth = Mathf.Max(1f, levelZeroMaxHealth);
        health = Mathf.Min(health, MaxHealth);
        UpdateHealthUI(true);
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

        // Guard is a separate equipment-driven defensive layer after normal Defense.
        // One roll is made for each actual TakeDamage call.
        float guardChance = GuardChance;
        float guardReduction = GuardReduction;
        if (guardChance > 0f && guardReduction > 0f && Random.value < guardChance)
            damage *= 1f - guardReduction;

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

        PlayerXP playerXP = GetComponent<PlayerXP>();
        if (playerXP != null)
            playerXP.ApplyDeathPenaltyAndSave();

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

    private void OnValidate()
    {
        baseGuardChance = Mathf.Clamp01(baseGuardChance);
        baseGuardReduction = Mathf.Clamp01(baseGuardReduction);
    }

    private void OnDestroy()
    {
        if (equipment != null) equipment.EquipmentChanged -= OnEquipmentChanged;
    }
}
