using System.Collections;
using UnityEngine;

public class MageRed : MonoBehaviour
{
    [Header("Indstillinger")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float waitTime = 2f; // Hvor længe venter den, før den går videre?
    [SerializeField] private EnemyManager myManager; // HUSK at trække din EnemyManager herind i Inspectoren!

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 currentTarget; // Hvor er vi på vej hen lige nu?
    private bool isMoving = false; // Holder styr på om vi går eller venter

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Find det første punkt med det samme
        FindNewPosition();
    }

    void Update()
    {
        // Hvis vi venter (ikke bevæger os), skal vi ikke gøre mere i Update
        if (!isMoving) return;

        // Udregn retningen fra os selv hen til målet
        Vector2 direction = (currentTarget - (Vector2)transform.position).normalized;

        // Hvor langt er der tilbage til målet?
        float distance = Vector2.Distance(transform.position, currentTarget);

        // --- ANIMATION LOGIK (Samme stil som spilleren) ---
        animator.SetFloat("Speed", 1f); // Vi bevæger os, så speed er høj

        // Bestem om vi kigger OP eller NED (bruger Bool som du ønskede)
        if (direction.y > 0.01f)
        {
            animator.SetBool("IsFacingUp", true);
        }
        else if (direction.y < -0.01f)
        {
            animator.SetBool("IsFacingUp", false);
        }

        // Tjek om vi er nået frem (hvis vi er meget tæt på målet)
        if (distance < 0.2f)
        {
            StartCoroutine(WaitAndMove());
        }
    }

    void FixedUpdate()
    {
        if (isMoving)
        {
            // Bevæg mod målet
            Vector2 direction = (currentTarget - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
            // Husk: Brug rb.velocity hvis du ikke kører Unity 6 endnu
        }
        else
        {
            // Stop helt op hvis vi ikke skal bevæge os
            rb.linearVelocity = Vector2.zero;
        }
    }

    // Hjælpefunktion der finder et nyt punkt via Manageren
    void FindNewPosition()
    {
        currentTarget = myManager.GetRandomPointInZone();
        isMoving = true;
    }

    // En "Coroutine" der får fjenden til at vente lidt
    IEnumerator WaitAndMove()
    {
        isMoving = false; // Stop bevægelse
        animator.SetFloat("Speed", 0f); // Sæt animation til Idle

        // Vent i 'waitTime' sekunder
        yield return new WaitForSeconds(waitTime);

        // Find nyt punkt
        FindNewPosition();
    }
}