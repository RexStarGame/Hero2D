using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Helbred Indstillinger")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    public int xpReward = 25;

    PlayerXP player;
    void Start()
    {
        // Sæt liv til max ved start
        currentHealth = maxHealth;
        player = FindObjectOfType<PlayerXP>();
    }

    // Denne funktion kalder du fra din spillers våben/projektil script
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log(gameObject.name + " tog skade! Liv tilbage: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        Debug.Log(gameObject.name + " er død!");
        if (player != null)
        {
            player.AddXP(xpReward);
        }
        Destroy(gameObject);
    }

    // TEST FUNKTION: (Kan slettes senere)
    // Hvis du klikker på fjenden med musen, tager den skade.
    private void OnMouseDown()
    {
        TakeDamage(1);
    }
}