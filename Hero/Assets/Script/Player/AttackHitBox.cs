using UnityEngine;
using UnityEngine.Serialization;

public class AttackHitbox : MonoBehaviour
{
    [Header("Owner (for lifesteal / crit)")]
    [SerializeField] private PlayerAttack ownerAttack;

    [Header("Damage Source")]
    [Tooltip("Drag the player's DamageUpgrade here (recommended). If empty, we auto-find in parent.")]
    [SerializeField] private DamageUpgrade damageUpgrade;

    [Tooltip("Minimum fallback damage if DamageUpgrade is missing.")]
    [FormerlySerializedAs("damage")]
    [Min(0)] [SerializeField] private int minimumDamage = 10;
    [Tooltip("Maximum fallback damage if DamageUpgrade is missing (inclusive).")]
    [Min(0)] [SerializeField] private int maximumDamage = 10;

    private void Awake()
    {
        if (ownerAttack == null)
            ownerAttack = GetComponentInParent<PlayerAttack>();

        if (damageUpgrade == null)
            damageUpgrade = GetComponentInParent<DamageUpgrade>();
    }

    private void OnValidate()
    {
        maximumDamage = Mathf.Max(minimumDamage, maximumDamage);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Use the player's body, not the sword tip: attacks from inside are blocked.
        Vector2 attackerPosition = ownerAttack != null
            ? (Vector2)ownerAttack.transform.position
            : (Vector2)transform.root.position;

        if (SafeZone2D.IsPlayerAttackBlocked(attackerPosition))
            return;

        BossHealth boss = collision.GetComponentInParent<BossHealth>();
        EnemyHealth enemy = boss == null ? collision.GetComponentInParent<EnemyHealth>() : null;
        if (boss == null && enemy == null)
            return;

        int baseDamage = damageUpgrade != null
            ? damageUpgrade.RollDamage()
            : Random.Range(minimumDamage, Mathf.Max(minimumDamage, maximumDamage) + 1);

        // Crit
        int finalDamage = baseDamage;
        bool isCritical = false;
        if (ownerAttack != null)
            finalDamage = ownerAttack.GetDamageForHit(baseDamage, out isCritical);

        bool didHit = false;

        if (boss != null)
        {
            boss.TakeDamage(finalDamage);
            didHit = true;
        }
        else
        {
            if (enemy != null)
            {
                enemy.TakeDamage(finalDamage);
                didHit = true;
            }
        }

        // Lifesteal uses final damage (after crit)
        if (didHit && ownerAttack != null)
        {
            ownerAttack.OnSuccessfulHit(finalDamage);
            ownerAttack.ShowDamageNumber(finalDamage, isCritical);
        }
    }
}
