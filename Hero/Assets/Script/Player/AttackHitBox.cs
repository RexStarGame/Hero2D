using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Owner (for lifesteal / on-hit effects)")]
    [SerializeField] private PlayerAttack ownerAttack;

    private DamageUpgrade damageUpgrade;

    private void Awake()
    {
        if (ownerAttack == null)
            ownerAttack = GetComponentInParent<PlayerAttack>();

        // THIS is the important change
        damageUpgrade = GetComponentInParent<DamageUpgrade>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (damageUpgrade == null) return;

        int damage = damageUpgrade.CurrentDamage;
        bool didHit = false;

        BossHealth boss = collision.GetComponentInParent<BossHealth>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            didHit = true;
        }
        else
        {
            EnemyHealth enemy = collision.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                didHit = true;
            }
        }

        if (didHit && ownerAttack != null)
        {
            ownerAttack.OnSuccessfulHit(damage);
        }
    }
}
