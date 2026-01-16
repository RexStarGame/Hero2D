using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 3;

    [Header("Owner (for lifesteal / on-hit effects)")]
    [SerializeField] private PlayerAttack ownerAttack;

    private void Awake()
    {
        // Auto-find the PlayerAttack on the parent (player)
        if (ownerAttack == null)
            ownerAttack = GetComponentInParent<PlayerAttack>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        bool didHit = false;

        // Boss
        BossHealth boss = collision.GetComponentInParent<BossHealth>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            didHit = true;
        }
        else
        {
            // Normal enemy
            EnemyHealth enemy = collision.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                didHit = true;
            }
        }

        // Lifesteal / on-hit only if we actually damaged something
        if (didHit && ownerAttack != null)
        {
            ownerAttack.OnSuccessfulHit(damage);
        }
    }
}
