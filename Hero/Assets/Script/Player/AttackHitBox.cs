using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Owner (for lifesteal / crit)")]
    [SerializeField] private PlayerAttack ownerAttack;

    [Header("Damage Source")]
    [Tooltip("Drag the player's DamageUpgrade here (recommended). If empty, we auto-find in parent.")]
    [SerializeField] private DamageUpgrade damageUpgrade;

    [Tooltip("Default damage if DamageUpgrade is missing (should match DamageUpgrade 'damage').")]
    [SerializeField] private int damage = 10; // <-- keep this so you always have a default

    private void Awake()
    {
        if (ownerAttack == null)
            ownerAttack = GetComponentInParent<PlayerAttack>();

        if (damageUpgrade == null)
            damageUpgrade = GetComponentInParent<DamageUpgrade>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Use the player's body, not the sword tip: attacks from inside are blocked.
        Vector2 attackerPosition = ownerAttack != null
            ? (Vector2)ownerAttack.transform.position
            : (Vector2)transform.root.position;

        if (SafeZone2D.IsPlayerAttackBlocked(attackerPosition))
            return;

        int baseDamage = (damageUpgrade != null) ? damageUpgrade.Damage : damage;

        // Crit
        int finalDamage = baseDamage;
        if (ownerAttack != null)
            finalDamage = ownerAttack.GetDamageForHit(baseDamage);

        bool didHit = false;

        BossHealth boss = collision.GetComponentInParent<BossHealth>();
        if (boss != null)
        {
            boss.TakeDamage(finalDamage);
            didHit = true;
        }
        else
        {
            EnemyHealth enemy = collision.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(finalDamage);
                didHit = true;
            }
        }

        // Lifesteal uses final damage (after crit)
        if (didHit && ownerAttack != null)
            ownerAttack.OnSuccessfulHit(finalDamage);
    }
}
