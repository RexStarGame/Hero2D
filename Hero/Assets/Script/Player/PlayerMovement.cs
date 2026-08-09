using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private PlayerEquipment equipment;
    public float EquipmentMovementSpeedBonus => equipment == null ? 0f : Mathf.Max(-0.9f, equipment.GetMovementSpeedBonus());
    public float EffectiveMoveSpeed => moveSpeed * (1f + EquipmentMovementSpeedBonus);

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    private SpriteRenderer sr;
    private float preserveExternalVelocityUntil;

    /// <summary>
    /// Prevents player input from immediately overwriting an externally applied
    /// Rigidbody velocity, for example after a boss throw or knockback.
    /// </summary>
    public void PreserveExternalVelocity(float duration)
    {
        preserveExternalVelocityUntil = Mathf.Max(
            preserveExternalVelocityUntil,
            Time.time + Mathf.Max(0f, duration));
        movement = Vector2.zero;
    }

    private void Awake()
    {
        MinimapTarget2D.Ensure(gameObject, MinimapTargetKind.Player);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }


    void Update()
    {
        if (MenuLock.IsGameplayInputBlocked)
        {
            movement = Vector2.zero;
            if (animator != null)
                animator.SetBool("IsMoving", false);
            return;
        }

        // Read input
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // --- FLIP LOGIC (LEFT / RIGHT) ---
        if (movement.x > 0.01f)
        {
            sr.flipX = false; // looking right
        }
        else if (movement.x < -0.01f)
        {
            sr.flipX = true; // looking left
        }

        if (movement.sqrMagnitude > 0)
        {
            movement = movement.normalized;
        }

        // --- WALK / IDLE ANIMATION ---
        bool isMoving = movement.sqrMagnitude > 0.01f;

        // Don't allow walk animation while attacking
        if (!animator.GetBool("IsAttackingBool"))
        {
            animator.SetBool("IsMoving", isMoving);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }


        // --- UP / DOWN DIRECTION ---
        if (movement.y > 0.01f)
        {
            animator.SetBool("IsFacingUp", true);
        }
        else if (movement.y < -0.01f)
        {
            animator.SetBool("IsFacingUp", false);
        }
    }


    void FixedUpdate()
    {
        if (Time.time < preserveExternalVelocityUntil)
            return;

        if (MenuLock.IsGameplayInputBlocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = movement * EffectiveMoveSpeed;
    }
}
