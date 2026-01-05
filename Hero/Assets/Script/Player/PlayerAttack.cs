using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float hitboxStartDelay = 0.1f;
    [SerializeField] private float hitboxActiveTime = 0.2f;
    [SerializeField] private float hitboxDistance = 0.7f;

    [Header("References")]
    [SerializeField] private Collider2D attackHitbox;
    [SerializeField] private Animator animator;

    // Name of the Trigger in the Animator
    private string animationTriggerName = "AttackTrigger";

    private bool canAttack = true;
    private Vector2 lastFacingDirection = Vector2.down;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
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
        {
            lastFacingDirection = currentInput.normalized;
        }

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

        // 1. Trigger the attack animation
        if (animator != null)
            animator.SetTrigger(animationTriggerName);

        // 2. Wait before activating hitbox
        yield return new WaitForSeconds(hitboxStartDelay);

        // 3. Activate hitbox
        if (attackHitbox != null)
            attackHitbox.enabled = true;

        // 4. Keep hitbox active for a short duration
        yield return new WaitForSeconds(hitboxActiveTime);

        // 5. Deactivate hitbox
        if (attackHitbox != null)
            attackHitbox.enabled = false;

        // 6. Wait for cooldown
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }
}
