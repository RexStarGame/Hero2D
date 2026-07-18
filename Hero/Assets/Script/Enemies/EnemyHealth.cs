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

        // 1. Vis visuelt feedback (Blink rød)
        if (feedback != null)
        {
            feedback.PlayHitFeedback();
        }

        // 2. Tjek om fjenden skal dø
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void OnDifficultyChanged(GameDifficulty difficulty)
    {
        ApplyDifficultyHealth(healthInitialized);
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

    void Die()
    {
        if (dead)
            return;

        dead = true;
        Debug.Log(gameObject.name + " er død!");

        // Giv XP til spilleren
        if (player != null)
        {
            player.AddKillXP(xpReward);
        }

        // Giv alle death listeners besked én gang (coins, quests osv.)
        onDeath?.Invoke();

        // Fjern fjenden fra spillet
        Destroy(gameObject);
    }

    // TEST FUNKTION: 
    // Gør det muligt at teste skade og blink ved at klikke på fjenden i spillet
    private void OnMouseDown()
    {
        TakeDamage(25);
    }
}
