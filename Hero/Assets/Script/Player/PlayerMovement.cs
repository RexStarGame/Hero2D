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
        rb.linearVelocity = movement * moveSpeed;
    }
}