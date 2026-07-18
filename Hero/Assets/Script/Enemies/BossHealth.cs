using UnityEngine;
using UnityEngine.Events;

public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 500f;

    [SerializeField] private float currentHealth;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    [Header("Optional")]
    public bool destroyOnDeath = true;

    [Header("Reward")]
    [Tooltip("Base kill XP. Equipped Kill XP % gear is applied by PlayerXP.")]
    [Min(0)] [SerializeField] private int xpReward = 100;

    [Header("Events")]
    public UnityEvent<float, float> onHealthChanged; // (current, max)
    public UnityEvent onDeath = new UnityEvent();

    private bool dead;
    private PlayerXP playerXP;
    private float baseMaxHealth;
    private EnemyDifficultyProfile difficultyProfile;
    private bool healthInitialized;

    private void Awake()
    {
        baseMaxHealth = Mathf.Max(1f, maxHealth);
        difficultyProfile = GetComponentInParent<EnemyDifficultyProfile>();
        if (difficultyProfile == null)
            difficultyProfile = gameObject.AddComponent<EnemyDifficultyProfile>();

        ApplyDifficultyHealth(false);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // If currentHealth was never set in Inspector, start full
        if (currentHealth <= 0f) currentHealth = maxHealth;
        healthInitialized = true;
        playerXP = FindAnyObjectByType<PlayerXP>();
    }

    private void Start()
    {
        // Fire once after all listeners had a chance to subscribe (UI often subscribes in OnEnable/Start)
        NotifyHealthChanged();
    }

    private void OnEnable()
    {
        DifficultyManager.DifficultyChanged += OnDifficultyChanged;
        // Also fire here so re-enabling works
        NotifyHealthChanged();
    }

    private void OnDisable()
    {
        DifficultyManager.DifficultyChanged -= OnDifficultyChanged;
    }

    public void TakeDamage(float amount)
    {
        if (dead) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        NotifyHealthChanged();

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (dead) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        NotifyHealthChanged();
    }

    public void SetHealth(float newCurrent, float newMax)
    {
        if (newMax <= 0f) newMax = 1f;

        baseMaxHealth = newMax;
        float sourcePercentage = Mathf.Clamp01(newCurrent / newMax);
        ApplyDifficultyHealth(false);
        currentHealth = Mathf.Clamp(sourcePercentage * maxHealth, 0f, maxHealth);

        if (currentHealth <= 0f && !dead)
            Die();
        else
            NotifyHealthChanged();
    }

    private void OnDifficultyChanged(GameDifficulty difficulty)
    {
        ApplyDifficultyHealth(healthInitialized);
        NotifyHealthChanged();
    }

    private void ApplyDifficultyHealth(bool preserveHealthPercentage)
    {
        float oldPercentage = maxHealth > 0f
            ? Mathf.Clamp01(currentHealth / maxHealth)
            : 1f;
        float multiplier = difficultyProfile != null
            ? difficultyProfile.HealthMultiplier
            : 1f;

        maxHealth = Mathf.Max(1f, baseMaxHealth * multiplier);

        if (preserveHealthPercentage)
            currentHealth = Mathf.Clamp(maxHealth * oldPercentage, 0f, maxHealth);
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
    }

    private void NotifyHealthChanged()
    {
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (dead) return;
        dead = true;

        currentHealth = 0f;
        NotifyHealthChanged();

        if (xpReward > 0)
        {
            if (playerXP == null) playerXP = FindAnyObjectByType<PlayerXP>();
            if (playerXP != null) playerXP.AddKillXP(xpReward);
        }

        onDeath?.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject);
    }
}
