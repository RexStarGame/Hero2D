// DashDoge.cs
using System.Collections;
using UnityEngine;

public class DashDoge : MonoBehaviour
{
    [Header("Dash Indstillinger")]
    [SerializeField] private float dashPower = 30f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.8f;

    [Header("Optional: Ignor�r Enemy-collisions under dash")]
    [Tooltip("S�t dine fjender p� en layer der hedder fx 'Enemy', ellers virker collision-ignore ikke.")]
    [SerializeField] private string enemyLayerName = "Enemy";
    [Tooltip("Hvilken layer spilleren skal v�re p� under dash. (2 = Ignore Raycast).")]
    [SerializeField] private int dashingLayer = 2;

    private Rigidbody2D rb;
    private TrailRenderer trail;
    private SpriteRenderer sprite;
    private Animator animator;
    private PlayerMovement playerMovement;

    private bool canDash = true;
    private bool isDashing = false;

    private int originalLayer;
    private Vector2 lastMoveDirection = Vector2.right;

    private int enemyLayer = -1;
    private bool collisionIgnored = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        trail = GetComponent<TrailRenderer>();
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();

        originalLayer = gameObject.layer;

        enemyLayer = LayerMask.NameToLayer(enemyLayerName); // -1 hvis layer ikke findes

        if (trail != null) trail.emitting = false;
    }

    void Update()
    {
        if (MenuLock.IsGameplayInputBlocked)
            return;

        // Gem sidste bevægelsesretning (så dash virker selv når du slipper taster)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 inputDir = new Vector2(moveX, moveY);

        if (inputDir.sqrMagnitude > 0.0001f)
            lastMoveDirection = inputDir.normalized;

        if (isDashing) return;

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
            StartCoroutine(PerformRollDash(lastMoveDirection));
    }

    private IEnumerator PerformRollDash(Vector2 direction)
    {
        canDash = false;
        isDashing = true;

        if (animator != null)
        {
            animator.SetBool("IsDashing", true);
            animator.SetTrigger("Roll");
        }


        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        // Sluk movement s� det ikke bremser dash
        if (playerMovement != null) playerMovement.enabled = false;

        // Visuel start
        if (sprite != null) sprite.color = new Color(1f, 1f, 1f, 0.6f);
        if (trail != null) trail.emitting = true;
        if (animator != null) animator.SetTrigger("Roll");

        // Skift layer under dash (valgfrit)
        gameObject.layer = dashingLayer;

        // Optional: Ignor�r collisions med Enemy-layer under dash
        if (enemyLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(dashingLayer, enemyLayer, true);
            collisionIgnored = true;
        }

        // Dash kraft
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * dashPower, ForceMode2D.Impulse);

        yield return new WaitForSeconds(dashDuration);

        // Slut: brems lidt ned
        rb.linearVelocity *= 0.1f;

        isDashing = false;

        if (animator != null)
        {
            animator.SetBool("IsDashing", false);
        }

        // Gendan visuals
        if (sprite != null) sprite.color = Color.white;
        if (trail != null) trail.emitting = false;

        // Gendan collisions
        if (collisionIgnored)
        {
            Physics2D.IgnoreLayerCollision(dashingLayer, enemyLayer, false);
            collisionIgnored = false;
        }

        // Gendan layer
        gameObject.layer = originalLayer;

        // T�nd movement igen
        if (playerMovement != null) playerMovement.enabled = true;

        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // PlayerHealth bruger denne
    public bool IsInvulnerable()
    {
        return isDashing;
    }
}
