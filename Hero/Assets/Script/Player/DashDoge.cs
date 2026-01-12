using System.Collections;
using UnityEngine;

public class DashDoge : MonoBehaviour
{
    [Header("Dash Indstillinger")]
    [SerializeField] private float dashPower = 30f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.8f;

    private Rigidbody2D rb;
    private TrailRenderer trail;
    private SpriteRenderer sprite;
    private Animator animator;

    // NYT: Vi skal bruge en reference til dit movement script
    private PlayerMovement playerMovement;

    private bool canDash = true;
    private bool isDashing = false;
    private int originalLayer;

    // Vi gemmer den sidste retning, vi gik i
    private Vector2 lastMoveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        trail = GetComponent<TrailRenderer>();
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // NYT: Find movement scriptet på samme spiller
        playerMovement = GetComponent<PlayerMovement>();

        originalLayer = gameObject.layer;

        // Sæt en standard retning
        lastMoveDirection = Vector2.right;

        if (trail != null) trail.emitting = false;
    }

    void Update()
    {
        // 1. Opdater "Hukommelsen"
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 inputDir = new Vector2(moveX, moveY);

        // Hvis vi bevæger os, så gem retningen!
        if (inputDir.magnitude > 0)
        {
            lastMoveDirection = inputDir.normalized;
        }

        if (isDashing) return;

        // 2. Dash Trigger
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(PerformRollDash(lastMoveDirection));
        }
    }

    private IEnumerator PerformRollDash(Vector2 direction)
    {
        canDash = false;
        isDashing = true;

        // Sikkerhedsnet
        if (direction == Vector2.zero) direction = Vector2.right;

        // --- VIGTIGT FIX ---
        // Sluk for PlayerMovement så den ikke bremser os!
        if (playerMovement != null) playerMovement.enabled = false;

        // 1. Visuel start
        if (sprite != null) sprite.color = new Color(1f, 1f, 1f, 0.6f);
        if (trail != null) trail.emitting = true;
        if (animator != null) animator.SetTrigger("Roll");

        // 2. Skift lag
        gameObject.layer = 2; // "Ignore Raycast"

        // 3. FYSISK KRAFT
        rb.linearVelocity = Vector2.zero; // Nulstil nuværende fart
        rb.AddForce(direction * dashPower, ForceMode2D.Impulse); // Eksplosion fremad!

        yield return new WaitForSeconds(dashDuration);

        // 4. Afslutning
        rb.linearVelocity = rb.linearVelocity * 0.1f; // Brems ned

        if (sprite != null) sprite.color = Color.white;
        if (trail != null) trail.emitting = false;
        gameObject.layer = originalLayer;

        // --- VIGTIGT FIX ---
        // Tænd for styringen igen, så du kan gå normalt
        if (playerMovement != null) playerMovement.enabled = true;

        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public bool IsInvulnerable()
    {
        return isDashing;
    }
}