using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Helbred Indstillinger")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    private ProgressBar progressBar;
    void Start()
    {
        // Sæt liv til max ved start
        currentHealth = maxHealth;
        progressBar = FindObjectOfType<ProgressBar>(); // Finds your ProgressBar in the scene
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
        if (progressBar != null)
        {
            progressBar.AddXP(10); // Add 10 XP per kill
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