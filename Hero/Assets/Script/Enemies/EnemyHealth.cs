using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Helbred Indstillinger")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    void Start()
    {
        // Sæt liv til max ved start
        currentHealth = maxHealth;
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
        // Her kan du senere indsætte lydeffekter, partikler eller loot drops
        Debug.Log(gameObject.name + " er død!");

        // VIGTIGT: Dette fjerner objektet fra spillet.
        // Når dette sker, vil din EnemySpawn automatisk opdage, at der mangler en fjende.
        Destroy(gameObject);
    }

    // TEST FUNKTION: (Kan slettes senere)
    // Hvis du klikker på fjenden med musen, tager den skade.
    private void OnMouseDown()
    {
        TakeDamage(1);
    }
}