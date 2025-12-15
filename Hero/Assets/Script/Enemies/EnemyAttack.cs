using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Indstillinger")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Kamp Stats")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackCooldown = 2f;

    private Transform player;
    private float cooldownTimer;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Mangler Player tag!");
        }

        if (firePoint == null) firePoint = transform;
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange && cooldownTimer <= 0)
        {
            Shoot();
            cooldownTimer = attackCooldown;
        }
        else
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    void Shoot()
    {
        // 1. Find retningen til spilleren
        Vector2 direction = (player.position - firePoint.position).normalized;

        if (animator != null)
        {
            // DEL 1: Er vi Oppe (Ryggen til) eller Nede (Ansigt frem)?
            if (direction.y > 0)
            {
                animator.SetBool("IsFacingUp", true); // Vi ser RYGGEN
            }
            else
            {
                animator.SetBool("IsFacingUp", false); // Vi ser ANSIGTET
            }

            // DEL 2: Er vi til Højre eller Venstre?
            if (direction.x > 0)
            {
                animator.SetBool("IsFacingRight", true);
            }
            else
            {
                animator.SetBool("IsFacingRight", false);
            }

            // 3. Aktivér angrebet
            animator.SetTrigger("Attack");
        }

        // 4. Roter selve fireballen
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        Instantiate(fireballPrefab, firePoint.position, rotation);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}