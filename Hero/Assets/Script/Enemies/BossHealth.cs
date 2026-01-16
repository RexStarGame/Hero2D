using UnityEngine;
using UnityEngine.Events;

public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 500f;

    [SerializeField] private float currentHealth;
    public float CurrentHealth => currentHealth;

    [Header("Optional")]
    public bool destroyOnDeath = true;

    [Header("Events")]
    public UnityEvent<float, float> onHealthChanged; // (current, max)
    public UnityEvent onDeath;

    private bool dead;

    private void Awake()
    {
        // Ensure valid values
        if (maxHealth <= 0f) maxHealth = 1f;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // If currentHealth was never set in Inspector, start full
        if (currentHealth <= 0f) currentHealth = maxHealth;
    }

    private void Start()
    {
        // Fire once after all listeners had a chance to subscribe (UI often subscribes in OnEnable/Start)
        NotifyHealthChanged();
    }

    private void OnEnable()
    {
        // Also fire here so re-enabling works
        NotifyHealthChanged();
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

        maxHealth = newMax;
        currentHealth = Mathf.Clamp(newCurrent, 0f, maxHealth);

        if (currentHealth <= 0f && !dead)
            Die();
        else
            NotifyHealthChanged();
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

        onDeath?.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject);
    }
}
