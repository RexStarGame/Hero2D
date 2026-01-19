using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }


    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // --- FLIP LOGIK ---
        if (movement.x > 0.01f)
        {
            sr.flipX = false;
        }
        else if (movement.x < -0.01f)
        {
            sr.flipX = true;
        }

        if (movement.sqrMagnitude > 0)
        {
            movement = movement.normalized;
        }

        // --- ANIMATION LOGIK ---

        // Walking / Idle
        bool isMoving = movement.sqrMagnitude > 0.01f;
        animator.SetBool("IsMoving", isMoving);

        // Facing up/down
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
        rb.linearVelocity = movement * moveSpeed;
    }
}