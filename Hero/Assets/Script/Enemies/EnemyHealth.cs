using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("Helbred Indstillinger")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    private int difficultyMaxHealth;

    [Header("Belønning")]
    public int xpReward = 25;

    [Header("Events")]
    public UnityEvent onDeath = new UnityEvent();

    [Header("Death presentation")]
    [Tooltip("Keeps the enemy object alive briefly so a death animation can finish. Existing enemies keep the default value of 0.")]
    [Min(0f)] [SerializeField] private float deathDestroyDelay;

    public event System.Action Damaged;
    public event System.Action Died;
    public event System.Action<int, int> HealthChanged;

    private PlayerXP player;
    private bool dead;
    private HitFeedback feedback;
    private EnemyDifficultyProfile difficultyProfile;
    private bool healthInitialized;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => difficultyMaxHealth;
    public int BaseMaxHealth => maxHealth;

    private void Awake()
    {
        difficultyProfile = GetComponentInParent<EnemyDifficultyProfile>();
        if (difficultyProfile == null)
            difficultyProfile = gameObject.AddComponent<EnemyDifficultyProfile>();

        // Every normal enemy gets the same lightweight, event-driven world bar.
        // Keeping this here also covers future and runtime-spawned enemy prefabs.
        if (GetComponent<BossHealth>() == null &&
            GetComponentInChildren<EnemyHealthBarWorld>(true) == null)
            gameObject.AddComponent<EnemyHealthBarWorld>();
    }

    private void OnEnable()
    {
        DifficultyManager.DifficultyChanged += OnDifficultyChanged;
    }

    private void OnDisable()
    {
        DifficultyManager.DifficultyChanged -= OnDifficultyChanged;
    }

    void Start()
    {
        // Sæt liv til det valgte difficulty-max ved start.
        ApplyDifficultyHealth(false);
        currentHealth = difficultyMaxHealth;
        healthInitialized = true;
        NotifyHealthChanged();

        // Find referencer
        player = Object.FindAnyObjectByType<PlayerXP>();
        feedback = GetComponent<HitFeedback>();
    }

    // Denne funktion kaldes når spilleren rammer fjenden
    public void TakeDamage(int damage)
    {
        if (dead || damage <= 0)
            return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " tog " + damage + " skade! Liv tilbage: " + currentHealth);
        NotifyHealthChanged();

        // 1. Vis visuelt feedback (Blink rød)
        if (feedback != null)
        {
            feedback.PlayHitFeedback();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            Damaged?.Invoke();
        }
    }

    private void OnDifficultyChanged(GameDifficulty difficulty)
    {
        ApplyDifficultyHealth(healthInitialized);
        NotifyHealthChanged();
    }

    private void ApplyDifficultyHealth(bool preserveHealthPercentage)
    {
        float oldPercentage = difficultyMaxHealth > 0
            ? Mathf.Clamp01((float)currentHealth / difficultyMaxHealth)
            : 1f;

        float multiplier = difficultyProfile != null
            ? difficultyProfile.HealthMultiplier
            : 1f;
        difficultyMaxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * multiplier));

        if (preserveHealthPercentage)
        {
            currentHealth = Mathf.Clamp(
                Mathf.RoundToInt(difficultyMaxHealth * oldPercentage),
                0,
                difficultyMaxHealth);
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(Mathf.Max(0, currentHealth), Mathf.Max(1, difficultyMaxHealth));
    }

    void Die()
    {
        if (dead)
            return;

        dead = true;
        Debug.Log(gameObject.name + " er død!");

        Died?.Invoke();

        // Giv XP til spilleren
        if (player != null)
        {
            player.AddKillXP(xpReward);
        }

        // Giv alle death listeners besked én gang (coins, quests osv.)
        onDeath?.Invoke();

        // Fjern fjenden fra spillet
        Destroy(gameObject, deathDestroyDelay);
    }

    // TEST FUNKTION: 
    // Gør det muligt at teste skade og blink ved at klikke på fjenden i spillet
    private void OnMouseDown()
    {
        TakeDamage(25);
    }
}
