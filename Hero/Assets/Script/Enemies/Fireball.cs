using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Indstillinger")]
    [Tooltip("Hvor hurtigt flyver fireballen?")]
    [SerializeField] private float speed = 7f;

    [Tooltip("Hvor mange sekunder går der før den forsvinder af sig selv?")]
    [SerializeField] private float lifeTime = 4f;

    [SerializeField] private float Damage = 10f;

    private PlayerHealth playerHealth;

    void Start()
    {
        // Denne linje fortæller Unity: "Slet mig om 'lifeTime' sekunder"
        // Det sker helt automatisk i baggrunden.
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Flyv fremad i den retning vi kigger (Vector2.right er "fremad" i 2D rotation)
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    // Denne funktion kører, når fireballens Collider rammer en anden Collider
    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Tjek om det vi ramte er en fjende
        // Hvis vi rammer en fjende (os selv eller en ven), så gør ingenting!
        if (other.CompareTag("Enemy"))
        {
            return; // Stop funktionen her, ikke gør mere.
        }

        // 2. Tjek om vi ramte patrulje-området (EnemyManagerens zone)
        // Hvis din EnemyManager har et tag (f.eks. "GameController" eller "Zone"), kan du ignorere den
        if (other.CompareTag("GameController"))
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            playerHealth.TakeDamage(Damage);
            return;
        }

        // 3. Hvis vi når herned, har vi ramt spilleren eller en væg
        // Her kan du senere tilføje: other.GetComponent<PlayerHealth>().TakeDamage(1);

        Destroy(gameObject);
    }
}