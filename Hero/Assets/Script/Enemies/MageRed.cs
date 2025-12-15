using System.Collections;
using UnityEngine;

public class MageRed : MonoBehaviour
{
    [Header("Indstillinger")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float waitTime = 2f;

    // Vi behøver ikke trække den ind manuelt længere, den finder den selv
    private EnemyManager myManager;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 currentTarget;
    private bool isMoving = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // --- HER ER MAGIEN: FIND MANAGEREN SELV ---

        // Metode A: Find via TAG (Husk at sætte tagget 'GameController' på din Manager i scenen!)
        GameObject managerObject = GameObject.FindGameObjectWithTag("Manger");

        if (managerObject != null)
        {
            myManager = managerObject.GetComponent<EnemyManager>();

            // Nu har vi fundet den, så start med at gå!
            FindNewPosition();
        }
        else
        {
            Debug.LogError("Fjenden kunne ikke finde 'GameController'! Har du husket at sætte Tagget?");
        }
    }

    void Update()
    {
        // Hvis vi ikke har en manager eller ikke skal bevæge os, så stop
        if (myManager == null || !isMoving) return;

        Vector2 direction = (currentTarget - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, currentTarget);

        // --- ANIMATION ---
        if (animator != null)
        {
            animator.SetFloat("Speed", 1f);
            if (direction.y > 0.01f) animator.SetBool("IsFacingUp", true);
            else if (direction.y < -0.01f) animator.SetBool("IsFacingUp", false);
        }

        if (distance < 0.2f)
        {
            StartCoroutine(WaitAndMove());
        }
    }

    void FixedUpdate()
    {
        if (isMoving && rb != null)
        {
            Vector2 direction = (currentTarget - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void FindNewPosition()
    {
        if (myManager != null)
        {
            currentTarget = myManager.GetRandomPointInZone();
            isMoving = true;
        }
    }

    IEnumerator WaitAndMove()
    {
        isMoving = false;
        if (animator != null) animator.SetFloat("Speed", 0f);

        yield return new WaitForSeconds(waitTime);

        FindNewPosition();
    }
}