using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("Helbred Indstillinger")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Belønning")]
    public int xpReward = 25;

    [Header("Events")]
    public UnityEvent onDeath = new UnityEvent();

    private PlayerXP player;
    private bool dead;
    private HitFeedback feedback;

    void Start()
    {
        // Sæt liv til max ved start
        currentHealth = maxHealth;

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

    void Die()
    {
        if (dead)
            return;

        dead = true;
        Debug.Log(gameObject.name + " er død!");

        // Giv XP til spilleren
        if (player != null)
        {
            player.AddXP(xpReward);
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