using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;      // Attack speed (lavere = hurtigere)
    [SerializeField] private float hitboxStartDelay = 0.1f;
    [SerializeField] private float hitboxActiveTime = 0.2f;
    [SerializeField] private float hitboxDistance = 0.7f;

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
    [SerializeField] private float lifeStealPercent = 0f;

    private string animationTriggerName = "AttackTrigger";
    private string animationBoolName = "IsAttackingBool";

    private bool canAttack = true;
    private Vector2 lastFacingDirection = Vector2.down;

    void Awake()
    {
        if (attackHitbox != null) attackHitbox.enabled = false;
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        UpdateDirection();

        if (Input.GetKeyDown(KeyCode.Space) && canAttack)
        {
            StartCoroutine(PerformAttack());
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

        // 1) Animator
        if (animator != null)
        {
            animator.SetBool(animationBoolName, true);
            animator.SetTrigger(animationTriggerName);
        }

        yield return new WaitForSeconds(hitboxStartDelay);
        if (attackHitbox != null) attackHitbox.enabled = true;

        yield return new WaitForSeconds(hitboxActiveTime);
        if (attackHitbox != null) attackHitbox.enabled = false;

        // 2) Animator færdig
        if (animator != null)
            animator.SetBool(animationBoolName, false);

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    // ---------- Life Steal mechanics ----------
    // Kaldes fra AttackHitbox når vi reelt rammer noget
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
        lifeStealPercent += addPercent; // 0.0002 = 0.02%
        // Debug.Log($"LifeSteal level {lifeStealLevel}, LifeSteal%={lifeStealPercent * 100f}%");
    }

    // ---------- Attack Speed upgrade support ----------
    public void UpgradeAttackSpeed(float reductionPerLevel, float minCooldown)
    {
        attackSpeedLevel++;
        attackCooldown = Mathf.Max(minCooldown, attackCooldown - reductionPerLevel);
        // Debug.Log($"AttackSpeed level {attackSpeedLevel}, cooldown={attackCooldown}");
    }
}
