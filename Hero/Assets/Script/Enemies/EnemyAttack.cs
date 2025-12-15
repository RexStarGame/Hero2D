using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Indstillinger")]
    [SerializeField] private GameObject fireballPrefab; // Hvad skal den skyde med?
    [SerializeField] private Transform firePoint;       // Hvor skydes den fra? (F.eks. stavens spids)

    [Header("Kamp Stats")]
    [SerializeField] private float attackRange = 5f;    // Hvor tæt skal spilleren være?
    [SerializeField] private float attackCooldown = 2f; // Hvor mange sekunder mellem hvert skud?

    private Transform player;       // Hvor er spilleren?
    private float cooldownTimer;    // Tæller ned til næste skud

    void Start()
    {
        // Vi finder spilleren automatisk via Tagget "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            // Hvis vi glemte tagget, giver vi en advarsel i konsollen, men spillet crasher ikke
            Debug.LogWarning("EnemyAttack kunne ikke finde en spiller med tagget 'Player'!");
        }

        // Hvis vi har glemt at sætte et firePoint, bruger vi bare fjendens egen position
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    void Update()
    {
        // Hvis spilleren er død (eller ikke fundet), gør ingenting
        if (player == null) return;

        // 1. Mål afstanden til spilleren
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // 2. Er vi tæt nok? OG er timeren klar?
        if (distanceToPlayer <= attackRange && cooldownTimer <= 0)
        {
            Shoot();
            // Nulstil timeren
            cooldownTimer = attackCooldown;
        }
        else
        {
            // Tæl timeren ned
            cooldownTimer -= Time.deltaTime;
        }
    }

    void Shoot()
    {
        // Udregn retningen mod spilleren (Spillerens position minus start-positionen)
        Vector2 direction = (player.position - firePoint.position).normalized;

        // Udregn vinklen, så fireballen roterer rigtigt mod spilleren
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        // Skab fireballen
        Instantiate(fireballPrefab, firePoint.position, rotation);
    }

    // En lille hjælper, så du kan se rækkevidden (den røde cirkel) i Editoren
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}