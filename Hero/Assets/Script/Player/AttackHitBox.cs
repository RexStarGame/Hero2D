using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 3;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Boss
        BossHealth boss = collision.GetComponentInParent<BossHealth>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            return;
        }

        // Normal enemy
        EnemyHealth enemy = collision.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            return;
        }
    }
}
