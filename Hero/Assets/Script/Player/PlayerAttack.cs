using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;      // Attack speed (lower = faster)
    [SerializeField] private float hitboxStartDelay = 0.5f;
    [SerializeField] private float hitboxActiveTime = 0.2f;
    [SerializeField] private float hitboxDistance = 0.7f;

    public float BaseAttackCooldown => attackCooldown;
    public float EquipmentAttackSpeedBonus => equipment == null ? 0f : Mathf.Max(0f, equipment.GetAttackSpeedBonus());
    public float AttackCooldown => attackCooldown / (1f + EquipmentAttackSpeedBonus);
    public float AbilityLifeStealPercent => lifeStealPercent;
    public float EquipmentLifeStealPercent => equipment == null ? 0f : equipment.GetLifeStealBonus();
    public float LifeStealPercent => Mathf.Max(0f, AbilityLifeStealPercent + EquipmentLifeStealPercent);
    public float AbilityCritChance => critChance;
    public float EquipmentCritChance => equipment == null ? 0f : equipment.GetCriticalChanceBonus();
    public float CritChance => Mathf.Clamp01(AbilityCritChance + EquipmentCritChance);
    public float CritMultiplier => critMultiplier;      // e.g. 2.0

    // ---------- NEW: Cooldown/Ready info for UI ----------
    public bool CanAttack => canAttack;

    // Time.time when next attack becomes available
    public float AttackReadyTime { get; private set; }

    // Full cycle from pressing attack until you can attack again
    public float AttackCycleDuration => hitboxStartDelay + hitboxActiveTime + AttackCooldown;

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
    [SerializeField] private SwordSwingTrail2D swordSwingTrail;

    [Header("Health (for Life Steal)")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerEquipment equipment;

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
    private float levelZeroAttackCooldown;
    private Vector2 lastFacingDirection = Vector2.down;
    private Coroutine attackRoutine;
    private bool trailPlayedThisAttack;
    private PlayerDamageNumberWorld damageNumberWorld;
    private SafeZoneFeedbackUI safeZoneFeedback;

    [SerializeField] private DamageUpgrade damageUpgrade;
    public DamageUpgrade DamageUpgrade => damageUpgrade;

    private void Awake()
    {
        levelZeroAttackCooldown = Mathf.Max(0.01f, attackCooldown);
        if (attackHitbox != null) attackHitbox.enabled = false;
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (swordSwingTrail == null && attackHitbox != null)
        {
            swordSwingTrail = attackHitbox.GetComponent<SwordSwingTrail2D>();
            if (swordSwingTrail == null)
                swordSwingTrail = attackHitbox.gameObject.AddComponent<SwordSwingTrail2D>();
        }

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (equipment == null)
            equipment = GetComponent<PlayerEquipment>();

        if (damageUpgrade == null)
            damageUpgrade = GetComponent<DamageUpgrade>();

        damageNumberWorld = GetComponent<PlayerDamageNumberWorld>();
        if (damageNumberWorld == null)
            damageNumberWorld = gameObject.AddComponent<PlayerDamageNumberWorld>();

        safeZoneFeedback = GetComponent<SafeZoneFeedbackUI>();
        if (safeZoneFeedback == null)
            safeZoneFeedback = gameObject.AddComponent<SafeZoneFeedbackUI>();

        PlayerProgressSave.RestoreAttackUpgrades(this);

        // Start as ready
        AttackReadyTime = Time.time;
    }

    void Update()
    {
        if (MenuLock.IsGameplayInputBlocked)
            return;

        UpdateDirection();

        // Only start attack if allowed
        if (Input.GetKeyDown(KeyCode.Space) && canAttack && attackRoutine == null)
        {
            if (SafeZone2D.IsPlayerAttackBlocked(transform.position))
            {
                if (safeZoneFeedback != null)
                    safeZoneFeedback.ShowAttackBlocked();

                return;
            }

            attackRoutine = StartCoroutine(PerformAttack());
        }

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
        attackRoutine = null;
        trailPlayedThisAttack = false;

        // NEW: set when we will be ready again (includes delays + cooldown)
        AttackReadyTime = Time.time + AttackCycleDuration;

        if (animator != null)
        {
            animator.SetBool(animationBoolName, true);
            animator.SetTrigger(animationTriggerName);
        }

        yield return new WaitForSeconds(hitboxStartDelay);
        ActivateHitboxAndTrail();

        yield return new WaitForSeconds(hitboxActiveTime);
        if (attackHitbox != null) attackHitbox.enabled = false;

        if (animator != null)
            animator.SetBool(animationBoolName, false);

        if (swordSwingTrail != null)
            swordSwingTrail.StopTrail();

        yield return new WaitForSeconds(AttackCooldown);
        canAttack = true;
        attackRoutine = null;


    }
    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        canAttack = true;
        if (attackHitbox != null) attackHitbox.enabled = false;
        if (swordSwingTrail != null) swordSwingTrail.StopTrail();
        trailPlayedThisAttack = false;
    }

    public void EnableHitbox()
    {
        Debug.Log("Hitbox enabled");
        ActivateHitboxAndTrail();
    }

    public void DisableHitbox()
    {
        Debug.Log("Hitbox disabled");
        if (attackHitbox != null)
            attackHitbox.enabled = false;
    }

    private void ActivateHitboxAndTrail()
    {
        if (attackHitbox != null)
            attackHitbox.enabled = true;

        if (trailPlayedThisAttack || swordSwingTrail == null)
            return;

        // The trail belongs to the visible attack animation, never idle state.
        if (animator != null && !animator.GetBool(animationBoolName))
            return;

        trailPlayedThisAttack = true;
        swordSwingTrail.PlaySwing();
    }


    // ---------- Crit: compute damage for a hit ----------
    public int GetDamageForHit(int baseDamage)
    {
        return GetDamageForHit(baseDamage, out _);
    }

    public int GetDamageForHit(int baseDamage, out bool isCritical)
    {
        isCritical = false;
        if (baseDamage <= 0) return 0;

        isCritical = Random.value < CritChance;
        if (!isCritical) return baseDamage;

        int critDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
        return Mathf.Max(baseDamage, critDamage);
    }

    public void ShowDamageNumber(int damage, bool isCritical)
    {
        if (damage <= 0)
            return;

        if (damageNumberWorld == null)
            damageNumberWorld = GetComponent<PlayerDamageNumberWorld>();

        if (damageNumberWorld != null)
            damageNumberWorld.Show(damage, isCritical);
    }

    // ---------- Life Steal: heal on real hit ----------
    public void OnSuccessfulHit(float damageDealt)
    {
        if (playerHealth == null) return;
        if (LifeStealPercent <= 0f) return;
        if (damageDealt <= 0f) return;

        float healAmount = damageDealt * LifeStealPercent;
        if (healAmount <= 0f) return;

        playerHealth.Heal(healAmount);
    }

    public void UpgradeLifeSteal(float addPercent)
    {
        lifeStealLevel++;
        lifeStealPercent += addPercent;
    }

    public void RestoreUpgradeProgress(
        int savedAttackSpeedLevel,
        float savedAttackCooldown,
        int savedLifeStealLevel,
        float savedLifeStealPercent,
        int savedCritLevel,
        float savedCritChance)
    {
        attackSpeedLevel = Mathf.Max(0, savedAttackSpeedLevel);
        attackCooldown = Mathf.Max(0.01f, savedAttackCooldown);
        lifeStealLevel = Mathf.Max(0, savedLifeStealLevel);
        lifeStealPercent = Mathf.Max(0f, savedLifeStealPercent);
        critLevel = Mathf.Max(0, savedCritLevel);
        critChance = Mathf.Clamp01(savedCritChance);
    }

    public void ResetAbilityUpgradeProgress()
    {
        attackSpeedLevel = 0;
        attackCooldown = Mathf.Max(0.01f, levelZeroAttackCooldown);
        lifeStealLevel = 0;
        lifeStealPercent = 0f;
        critLevel = 0;
        critChance = 0f;
        AttackReadyTime = Time.time;
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
