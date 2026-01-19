using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;      // Attack speed (lower = faster)
    [SerializeField] private float hitboxStartDelay = 0.1f;
    [SerializeField] private float hitboxActiveTime = 0.2f;
    [SerializeField] private float hitboxDistance = 0.7f;

    public float AttackCooldown => attackCooldown;      // attack speed value (seconds)
    public float LifeStealPercent => lifeStealPercent;  // 0..1
    public float CritChance => critChance;              // 0..1
    public float CritMultiplier => critMultiplier;      // e.g. 2.0

    // ---------- NEW: Cooldown/Ready info for UI ----------
    public bool CanAttack => canAttack;

    // Time.time when next attack becomes available
    public float AttackReadyTime { get; private set; }

    // Full cycle from pressing attack until you can attack again
    public float AttackCycleDuration => hitboxStartDelay + hitboxActiveTime + attackCooldown;

    public float CooldownRemaining => Mathf.Max(0f, AttackReadyTime - Time.time);

    // 0..1 progress where 1 = ready
    public float Cooldown01
    {
        get
        {
            float d = Mathf.Max(0.0001f, AttackCycleDuration);
            return Mathf.Clamp01(1f - (CooldownRemaining / d));
        }
    }
    // -----------------------------------------------

    [Header("References")]
    [SerializeField] private Collider2D attackHitbox;
    [SerializeField] private Animator animator;

    [Header("Health (for Life Steal)")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Attack Upgrades")]
    public int attackSpeedLevel = 0;

    [Header("Life Steal")]
    public int lifeStealLevel = 0;
    [Tooltip("0 = 0%. 0.0002 = 0.02%")]
    [SerializeField] private float lifeStealPercent = 0f; // starts 0%

    [Header("Critical Damage")]
    public int critLevel = 0;
    [Tooltip("0 = 0%. 0.01 = 1%")]
    [SerializeField] private float critChance = 0f;      // starts 0%
    [SerializeField] private float critMultiplier = 2f;  // double damage

    private string animationTriggerName = "AttackTrigger";
    private string animationBoolName = "IsAttackingBool";

    private bool canAttack = true;
    private Vector2 lastFacingDirection = Vector2.down;

    [SerializeField] private DamageUpgrade damageUpgrade;
    public DamageUpgrade DamageUpgrade => damageUpgrade;

    private void Awake()
    {
        if (attackHitbox != null) attackHitbox.enabled = false;
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (damageUpgrade == null)
            damageUpgrade = GetComponent<DamageUpgrade>();

        // Start as ready
        AttackReadyTime = Time.time;
    }

    void Update()
    {
        UpdateDirection();

        if (Input.GetKeyDown(KeyCode.Space) && canAttack)
            StartCoroutine(PerformAttack());
    }

    private void UpdateDirection()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 currentInput = new Vector2(moveX, moveY);

        if (currentInput.sqrMagnitude > 0.01f)
            lastFacingDirection = currentInput.normalized;

        if (attackHitbox != null)
        {
            attackHitbox.transform.localPosition = lastFacingDirection * hitboxDistance;
            float angle = Mathf.Atan2(lastFacingDirection.y, lastFacingDirection.x) * Mathf.Rad2Deg;
            attackHitbox.transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    IEnumerator PerformAttack()
    {
        canAttack = false;

        // NEW: set when we will be ready again (includes delays + cooldown)
        AttackReadyTime = Time.time + AttackCycleDuration;

        if (animator != null)
        {
            animator.SetBool(animationBoolName, true);
            animator.SetTrigger(animationTriggerName);
        }

        yield return new WaitForSeconds(hitboxStartDelay);
        if (attackHitbox != null) attackHitbox.enabled = true;

        yield return new WaitForSeconds(hitboxActiveTime);
        if (attackHitbox != null) attackHitbox.enabled = false;

        if (animator != null)
            animator.SetBool(animationBoolName, false);

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    // ---------- Crit: compute damage for a hit ----------
    public int GetDamageForHit(int baseDamage)
    {
        if (baseDamage <= 0) return 0;

        bool isCrit = Random.value < Mathf.Clamp01(critChance);
        if (!isCrit) return baseDamage;

        int critDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
        return Mathf.Max(baseDamage, critDamage);
    }

    // ---------- Life Steal: heal on real hit ----------
    public void OnSuccessfulHit(float damageDealt)
    {
        if (playerHealth == null) return;
        if (lifeStealPercent <= 0f) return;
        if (damageDealt <= 0f) return;

        float healAmount = damageDealt * lifeStealPercent;
        if (healAmount <= 0f) return;

        playerHealth.Heal(healAmount);
    }

    public void UpgradeLifeSteal(float addPercent)
    {
        lifeStealLevel++;
        lifeStealPercent += addPercent;
    }

    // ---------- Attack speed upgrade ----------
    public void UpgradeAttackSpeed(float reductionPerLevel, float minCooldown)
    {
        attackSpeedLevel++;
        attackCooldown = Mathf.Max(minCooldown, attackCooldown - reductionPerLevel);

        // Keep ready time consistent if you change cooldown mid-game
        if (!canAttack)
            AttackReadyTime = Mathf.Max(AttackReadyTime, Time.time + CooldownRemaining);
    }

    // ---------- Crit upgrade ----------
    public void UpgradeCritChance(float addChance)
    {
        critLevel++;
        critChance = Mathf.Clamp01(critChance + addChance);
    }
}
