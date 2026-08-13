using System.Collections;
using UnityEngine;

public class MageRed : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float waitTime = 2f;

    [Header("Smart Chase")]
    [Min(0.1f)] [SerializeField] private float detectionRange = 6f;
    [Min(0.1f)] [SerializeField] private float giveUpRange = 10f;
    [Min(0f)] [SerializeField] private float chaseStopDistance = 0.9f;
    [SerializeField] private float chaseSpeedMultiplier = 1.15f;

    [Header("Patrol Validation")]
    [Min(0.05f)] [SerializeField] private float routeValidationInterval = 0.2f;
    [Min(0.1f)] [SerializeField] private float stuckTimeout = 1f;
    [Min(0.001f)] [SerializeField] private float minimumProgressDistance = 0.05f;

    [Header("Scene Debug")]
    [SerializeField] private bool showPatrolTarget = true;

    private EnemyManager myManager;
    private EnemyAggro2D aggro;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 currentTarget;
    private bool isMoving;
    private bool wasChasing;
    private Coroutine waitRoutine;
    private float nextRouteValidationTime;
    private Vector2 lastProgressPosition;
    private float lastProgressTime;
    private Vector2 lastRejectedTarget;
    private float showRejectedTargetUntil;

    public Vector2 CurrentPatrolTarget => currentTarget;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        aggro = GetComponent<EnemyAggro2D>();
        if (aggro == null) aggro = gameObject.AddComponent<EnemyAggro2D>();
        aggro.ConfigureRanges(detectionRange, giveUpRange);

        GameObject managerObject = GameObject.FindGameObjectWithTag("Manger");
        if (managerObject != null)
        {
            myManager = managerObject.GetComponent<EnemyManager>();
            FindNewPosition();
        }
        else
        {
            Debug.LogError("Enemy could not find the object tagged 'Manger'.", this);
        }
    }

    private void Update()
    {
        Transform playerTarget = aggro != null ? aggro.CurrentTarget : null;
        if (playerTarget != null)
        {
            wasChasing = true;
            UpdateAnimation(((Vector2)playerTarget.position - (Vector2)transform.position).normalized, true);
            return;
        }

        if (wasChasing)
        {
            wasChasing = false;
            FindNewPosition();
        }

        if (myManager == null || !isMoving)
        {
            UpdateAnimation(Vector2.zero, false);
            return;
        }

        if (Time.time >= nextRouteValidationTime)
        {
            nextRouteValidationTime = Time.time + routeValidationInterval;
            if (!myManager.IsPatrolRouteValid(transform.position, currentTarget))
            {
                CancelCurrentPatrolAndFindAnother();
                return;
            }

            CheckForBlockedPatrol();
        }

        Vector2 direction = (currentTarget - (Vector2)transform.position).normalized;
        UpdateAnimation(direction, true);

        if (Vector2.Distance(transform.position, currentTarget) < 0.2f && waitRoutine == null)
            waitRoutine = StartCoroutine(WaitAndMove());
    }

    private void FixedUpdate()
    {
        if (rb == null || aggro == null || !aggro.HasAuthority()) return;

        Transform playerTarget = aggro.CurrentTarget;
        if (playerTarget != null)
        {
            Vector2 offset = (Vector2)playerTarget.position - rb.position;
            if (offset.sqrMagnitude <= chaseStopDistance * chaseStopDistance)
                rb.linearVelocity = Vector2.zero;
            else
                rb.linearVelocity = offset.normalized * moveSpeed * chaseSpeedMultiplier;
            return;
        }

        rb.linearVelocity = isMoving
            ? (currentTarget - rb.position).normalized * moveSpeed
            : Vector2.zero;
    }

    private void FindNewPosition()
    {
        if (myManager == null) return;
        currentTarget = myManager.GetRandomPointInZone(transform.position);
        nextRouteValidationTime = Time.time + routeValidationInterval;
        lastProgressPosition = transform.position;
        lastProgressTime = Time.time;
        isMoving = true;
    }

    private void CancelCurrentPatrolAndFindAnother()
    {
        lastRejectedTarget = currentTarget;
        showRejectedTargetUntil = Time.time + 1f;

        if (rb != null && aggro != null && aggro.HasAuthority())
            rb.linearVelocity = Vector2.zero;

        isMoving = false;
        FindNewPosition();
    }

    private void CheckForBlockedPatrol()
    {
        float progressSqr = ((Vector2)transform.position - lastProgressPosition).sqrMagnitude;
        if (progressSqr >= minimumProgressDistance * minimumProgressDistance)
        {
            lastProgressPosition = transform.position;
            lastProgressTime = Time.time;
            return;
        }

        bool hasNotReachedTarget = Vector2.Distance(transform.position, currentTarget) >= 0.2f;
        if (hasNotReachedTarget && Time.time - lastProgressTime >= stuckTimeout)
            CancelCurrentPatrolAndFindAnother();
    }

    private void OnValidate()
    {
        detectionRange = Mathf.Max(0.1f, detectionRange);
        giveUpRange = Mathf.Max(detectionRange, giveUpRange);
        chaseStopDistance = Mathf.Max(0f, chaseStopDistance);
        routeValidationInterval = Mathf.Max(0.05f, routeValidationInterval);
        stuckTimeout = Mathf.Max(0.1f, stuckTimeout);
        minimumProgressDistance = Mathf.Max(0.001f, minimumProgressDistance);
    }

    private IEnumerator WaitAndMove()
    {
        isMoving = false;
        UpdateAnimation(Vector2.zero, false);
        yield return new WaitForSeconds(waitTime);
        FindNewPosition();
        waitRoutine = null;
    }

    private void UpdateAnimation(Vector2 direction, bool moving)
    {
        if (animator == null) return;
        animator.SetFloat("Speed", moving ? 1f : 0f);
        if (direction.y > 0.01f) animator.SetBool("IsFacingUp", true);
        else if (direction.y < -0.01f) animator.SetBool("IsFacingUp", false);
    }

    private void OnDisable()
    {
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        if (rb != null && aggro != null && aggro.HasAuthority())
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        DrawChaseRanges(detectionRange, giveUpRange);
        DrawPatrolTarget();
    }

    private void DrawPatrolTarget()
    {
        if (!showPatrolTarget || !Application.isPlaying || myManager == null) return;

        bool valid = myManager.IsPatrolRouteValid(transform.position, currentTarget);
        Color routeColor = valid
            ? new Color(0.15f, 0.9f, 1f, 0.95f)
            : new Color(1f, 0.1f, 0.1f, 1f);

        Gizmos.color = routeColor;
        Gizmos.DrawLine(transform.position, currentTarget);
        Gizmos.DrawWireSphere(currentTarget, 0.18f);
        Gizmos.DrawLine(currentTarget + Vector2.left * 0.25f, currentTarget + Vector2.right * 0.25f);
        Gizmos.DrawLine(currentTarget + Vector2.down * 0.25f, currentTarget + Vector2.up * 0.25f);

        if (Time.time < showRejectedTargetUntil)
        {
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 1f);
            Gizmos.DrawLine(transform.position, lastRejectedTarget);
            Gizmos.DrawWireSphere(lastRejectedTarget, 0.24f);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.color = routeColor;
        string state = valid ? "PATROL TARGET - VALID" : "PATROL TARGET - CANCELLED";
        UnityEditor.Handles.Label((Vector3)currentTarget + Vector3.up * 0.3f, state);
        if (Time.time < showRejectedTargetUntil)
        {
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label((Vector3)lastRejectedTarget + Vector3.up * 0.3f, "REJECTED - NEW TARGET SELECTED");
        }
#endif
    }

    private void DrawChaseRanges(float detect, float giveUp)
    {
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, detect);

        Gizmos.color = new Color(1f, 0.2f, 0.15f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, giveUp);

#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 0.75f, 0.1f, 1f);
        UnityEditor.Handles.Label(transform.position + Vector3.right * detect, "CHASE START");
        UnityEditor.Handles.color = new Color(1f, 0.2f, 0.15f, 1f);
        UnityEditor.Handles.Label(transform.position + Vector3.right * giveUp, "CHASE STOPS");
#endif
    }
}
