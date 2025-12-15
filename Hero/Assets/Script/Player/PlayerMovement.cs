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
        // Læs input
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // --- FLIP LOGIK (VENSTRE / HØJRE) ---
        if (movement.x > 0.01f)
        {
            sr.flipX = false; // kigger mod højre
        }
        else if (movement.x < -0.01f)
        {
            sr.flipX = true; // kigger mod venstre
        }

        if (movement.sqrMagnitude > 0)
        {
            movement = movement.normalized;
        }

        // --- ANIMATION LOGIK ---

        // 1. Fortæl om vi bevæger os (til at skifte fra Idle til Walk)
        animator.SetFloat("Speed", movement.sqrMagnitude);

        // 2. Bestem retning (Op eller Ned) med en BOOL
        if (movement.y > 0.01f) // Hvis vi går OP
        {
            animator.SetBool("IsFacingUp", true);
        }
        else if (movement.y < -0.01f) // Hvis vi går NED
        {
            animator.SetBool("IsFacingUp", false);
        }
        // Bemærk: Hvis vi går til siden (y=0), ændrer vi IKKE bool'en. 
        // Så husker den, om vi sidst kiggede op eller ned.
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }
}