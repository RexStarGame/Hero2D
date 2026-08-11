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

    [Header("Ranged Combat Movement")]
    [Min(0.1f)] [SerializeField] private float preferredCombatDistance = 4.25f;
    [Min(0.05f)] [SerializeField] private float combatDistanceTolerance = 0.45f;
    [Range(0.1f, 1f)] [SerializeField] private float retreatSpeedMultiplier = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float strafeSpeedMultiplier = 0.35f;
    [Min(0.1f)] [SerializeField] private float movementAcceleration = 12f;
    [Min(0.05f)] [SerializeField] private float combatDecisionInterval = 0.12f;
    [Min(0.1f)] [SerializeField] private float strafeDirectionInterval = 1.25f;
    [Min(0f)] [SerializeField] private float closingSpeedLookAhead = 0.3f;

    [Header("Ranged Attack Timing")]
    [Tooltip("Randomizes only the pause after each shot. The existing windup/telegraph always plays before firing.")]
    [Min(0.1f)] [SerializeField] private float minimumAttackCooldownMultiplier = 0.65f;
    [Min(0.1f)] [SerializeField] private float maximumAttackCooldownMultiplier = 1.25f;

    [Header("Ranged Movement Safety")]
    [Min(0.05f)] [SerializeField] private float movementProbeDistance = 0.45f;
    [Min(0f)] [SerializeField] private float obstacleProbeRadius = 0.18f;
    [Tooltip("Optional override. Leave empty to use the physics layers that collide with this enemy.")]
    [SerializeField] private LayerMask movementBlockingLayers;

    [Header("Patrol Validation")]
    [Min(0.05f)] [SerializeField] private float routeValidationInterval = 0.2f;
    [Min(0.1f)] [SerializeField] private float stuckTimeout = 1f;
    [Min(0.001f)] [SerializeField] private float minimumProgressDistance = 0.05f;
    [Min(1)] [SerializeField] private int regionalTargetSearchAttempts = 32;

    [Header("Scene Debug")]
    [SerializeField] private bool showPatrolTarget = true;

    private EnemyManager myManager;
    private EnemyAggro2D aggro;
    private Rigidbody2D rb;
    private Animator animator;
    private SpawnedEnemyRegionLink regionLink;
    private Vector2 currentTarget;
    private bool isMoving;
    private bool wasChasing;
    private Coroutine waitRoutine;
    private float nextRouteValidationTime;
    private Vector2 lastProgressPosition;
    private float lastProgressTime;
    private Vector2 lastRejectedTarget;
    private float showRejectedTargetUntil;
    private bool returningToRegion;
    private Vector2 desiredCombatVelocity;
    private float previousTargetDistance;
    private float lastCombatDecisionTime;
    private float nextCombatDecisionTime;
    private float nextStrafeDirectionTime;
    private int strafeDirection = 1;

    private readonly Collider2D[] movementProbeHits = new Collider2D[8];

    public Vector2 CurrentPatrolTarget => currentTarget;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        regionLink = GetComponent<SpawnedEnemyRegionLink>();
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        aggro = GetComponent<EnemyAggro2D>();
        if (aggro == null) aggro = gameObject.AddComponent<EnemyAggro2D>();
        aggro.ConfigureRanges(detectionRange, giveUpRange);

        EnemyAttack enemyAttack = GetComponent<EnemyAttack>();
        if (enemyAttack != null)
        {
            enemyAttack.ConfigureCooldownVariation(
                minimumAttackCooldownMultiplier,
                maximumAttackCooldownMultiplier);
        }

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
            Vector2 combatDirection = rb != null && rb.linearVelocity.sqrMagnitude > 0.0025f
                ? rb.linearVelocity.normalized
                : ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
            UpdateAnimation(combatDirection, rb != null && rb.linearVelocity.sqrMagnitude > 0.0025f);
            return;
        }

        if (wasChasing)
        {
            wasChasing = false;
            FindNewPosition();
        }

        if (IsOutsideAssignedRegion())
        {
            if (!returningToRegion)
                BeginReturningToRegion();
        }
        else if (returningToRegion)
        {
            returningToRegion = false;
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
            UpdateCombatMovement(playerTarget);
            return;
        }

        desiredCombatVelocity = Vector2.zero;
        previousTargetDistance = 0f;

        rb.linearVelocity = isMoving
            ? (currentTarget - rb.position).normalized * moveSpeed
            : Vector2.zero;
    }

    private void FindNewPosition()
    {
        if (myManager == null) return;

        Collider2D spawnArea = regionLink != null ? regionLink.SpawnArea : null;
        if (spawnArea != null)
        {
            // A regional enemy must never fall back to the global patrol area.
            // If no safe regional route is available this tick, staying still
            // lets the normal stuck retry request another regional point.
            currentTarget = TryGetRegionalPatrolTarget(out Vector2 regionalTarget)
                ? regionalTarget
                : (Vector2)transform.position;
        }
        else
        {
            currentTarget = myManager.GetRandomPointInZone(transform.position);
        }
        nextRouteValidationTime = Time.time + routeValidationInterval;
        lastProgressPosition = transform.position;
        lastProgressTime = Time.time;
        isMoving = true;
    }

    private bool IsOutsideAssignedRegion()
    {
        Collider2D spawnArea = regionLink != null ? regionLink.SpawnArea : null;
        return spawnArea != null && !spawnArea.OverlapPoint(transform.position);
    }

    private void UpdateCombatMovement(Transform playerTarget)
    {
        Vector2 toPlayer = (Vector2)playerTarget.position - rb.position;
        float distance = toPlayer.magnitude;
        if (distance <= 0.001f)
        {
            desiredCombatVelocity = Vector2.zero;
            rb.linearVelocity = Vector2.MoveTowards(
                rb.linearVelocity, Vector2.zero, movementAcceleration * Time.fixedDeltaTime);
            return;
        }

        if (Time.time >= nextCombatDecisionTime)
        {
            float elapsed = lastCombatDecisionTime > 0f
                ? Mathf.Max(Time.fixedDeltaTime, Time.time - lastCombatDecisionTime)
                : combatDecisionInterval;
            float closingSpeed = previousTargetDistance > 0f
                ? Mathf.Max(0f, (previousTargetDistance - distance) / elapsed)
                : 0f;

            desiredCombatVelocity = ChooseCombatVelocity(
                playerTarget.position,
                toPlayer / distance,
                distance,
                closingSpeed);

            previousTargetDistance = distance;
            lastCombatDecisionTime = Time.time;
            nextCombatDecisionTime = Time.time + combatDecisionInterval;
        }

        rb.linearVelocity = Vector2.MoveTowards(
            rb.linearVelocity,
            desiredCombatVelocity,
            movementAcceleration * Time.fixedDeltaTime);
    }

    private Vector2 ChooseCombatVelocity(
        Vector2 playerPosition,
        Vector2 directionToPlayer,
        float distance,
        float closingSpeed)
    {
        float minimumDistance = preferredCombatDistance - combatDistanceTolerance;
        float maximumDistance = preferredCombatDistance + combatDistanceTolerance;
        float predictedDistance = distance - closingSpeed * closingSpeedLookAhead;

        if (predictedDistance < minimumDistance || distance <= chaseStopDistance)
        {
            Vector2 awayFromPlayer = -directionToPlayer;
            float retreatSpeed = moveSpeed * retreatSpeedMultiplier;
            return FindSafeCombatVelocity(
                playerPosition,
                awayFromPlayer,
                retreatSpeed,
                true);
        }

        if (distance > maximumDistance)
        {
            return FindSafeCombatVelocity(
                playerPosition,
                directionToPlayer,
                moveSpeed * chaseSpeedMultiplier,
                false);
        }

        if (strafeSpeedMultiplier <= 0f)
            return Vector2.zero;

        if (Time.time >= nextStrafeDirectionTime)
        {
            strafeDirection = Random.value < 0.5f ? -1 : 1;
            nextStrafeDirectionTime = Time.time + strafeDirectionInterval;
        }

        Vector2 perpendicular = new Vector2(-directionToPlayer.y, directionToPlayer.x) * strafeDirection;
        Vector2 strafeVelocity = FindSafeCombatVelocity(
            playerPosition,
            perpendicular,
            moveSpeed * strafeSpeedMultiplier,
            false);

        if (strafeVelocity.sqrMagnitude > 0f)
            return strafeVelocity;

        strafeDirection *= -1;
        nextStrafeDirectionTime = Time.time + strafeDirectionInterval;
        return FindSafeCombatVelocity(
            playerPosition,
            -perpendicular,
            moveSpeed * strafeSpeedMultiplier,
            false);
    }

    private Vector2 FindSafeCombatVelocity(
        Vector2 playerPosition,
        Vector2 preferredDirection,
        float speed,
        bool prioritizeDistance)
    {
        preferredDirection.Normalize();
        Vector2 perpendicular = new Vector2(-preferredDirection.y, preferredDirection.x);

        Vector2 bestDirection = Vector2.zero;
        float bestScore = float.NegativeInfinity;
        float currentDistance = Vector2.Distance(rb.position, playerPosition);

        EvaluateCombatDirection(
            preferredDirection,
            preferredDirection,
            playerPosition,
            currentDistance,
            prioritizeDistance,
            ref bestDirection,
            ref bestScore);
        EvaluateCombatDirection(
            (preferredDirection + perpendicular * 0.65f).normalized,
            preferredDirection,
            playerPosition,
            currentDistance,
            prioritizeDistance,
            ref bestDirection,
            ref bestScore);
        EvaluateCombatDirection(
            (preferredDirection - perpendicular * 0.65f).normalized,
            preferredDirection,
            playerPosition,
            currentDistance,
            prioritizeDistance,
            ref bestDirection,
            ref bestScore);
        EvaluateCombatDirection(
            perpendicular,
            preferredDirection,
            playerPosition,
            currentDistance,
            prioritizeDistance,
            ref bestDirection,
            ref bestScore);
        EvaluateCombatDirection(
            -perpendicular,
            preferredDirection,
            playerPosition,
            currentDistance,
            prioritizeDistance,
            ref bestDirection,
            ref bestScore);

        return bestDirection * speed;
    }

    private void EvaluateCombatDirection(
        Vector2 candidate,
        Vector2 preferredDirection,
        Vector2 playerPosition,
        float currentDistance,
        bool prioritizeDistance,
        ref Vector2 bestDirection,
        ref float bestScore)
    {
        Vector2 destination = rb.position + candidate * movementProbeDistance;
        if (!IsCombatStepSafe(destination)) return;

        float resultingDistance = Vector2.Distance(destination, playerPosition);
        float directionScore = Vector2.Dot(candidate, preferredDirection);
        float distanceScore = prioritizeDistance ? resultingDistance - currentDistance : 0f;
        float score = directionScore + distanceScore * 2f;
        if (score <= bestScore) return;

        bestScore = score;
        bestDirection = candidate;
    }

    private bool IsCombatStepSafe(Vector2 destination)
    {
        Collider2D spawnArea = regionLink != null ? regionLink.SpawnArea : null;
        if (spawnArea != null && !spawnArea.OverlapPoint(destination))
            return false;

        if (myManager != null && !myManager.IsPatrolRouteValid(rb.position, destination))
            return false;

        int blockingMask = movementBlockingLayers.value != 0
            ? movementBlockingLayers.value
            : Physics2D.GetLayerCollisionMask(gameObject.layer);

        if (blockingMask == 0) return true;

        int hitCount = Physics2D.OverlapCircleNonAlloc(
            destination,
            obstacleProbeRadius,
            movementProbeHits,
            blockingMask);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = movementProbeHits[i];
            if (hit == null || hit.isTrigger || hit.transform.IsChildOf(transform)) continue;
            return false;
        }

        return true;
    }

    private void BeginReturningToRegion()
    {
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        returningToRegion = true;
        FindNewPosition();
    }

    private bool TryGetRegionalPatrolTarget(out Vector2 target)
    {
        target = transform.position;
        Collider2D spawnArea = regionLink != null ? regionLink.SpawnArea : null;
        if (spawnArea == null || myManager == null) return false;

        Bounds bounds = spawnArea.bounds;
        int attempts = Mathf.Max(1, regionalTargetSearchAttempts);
        for (int i = 0; i < attempts; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y));

            if (!spawnArea.OverlapPoint(candidate)) continue;
            if (!myManager.IsPatrolRouteValid(transform.position, candidate)) continue;

            target = candidate;
            return true;
        }

        return false;
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
        preferredCombatDistance = Mathf.Max(0.1f, preferredCombatDistance);
        combatDistanceTolerance = Mathf.Clamp(combatDistanceTolerance, 0.05f, preferredCombatDistance);
        retreatSpeedMultiplier = Mathf.Clamp(retreatSpeedMultiplier, 0.1f, 1f);
        strafeSpeedMultiplier = Mathf.Clamp01(strafeSpeedMultiplier);
        movementAcceleration = Mathf.Max(0.1f, movementAcceleration);
        combatDecisionInterval = Mathf.Max(0.05f, combatDecisionInterval);
        strafeDirectionInterval = Mathf.Max(0.1f, strafeDirectionInterval);
        closingSpeedLookAhead = Mathf.Max(0f, closingSpeedLookAhead);
        minimumAttackCooldownMultiplier = Mathf.Max(0.1f, minimumAttackCooldownMultiplier);
        maximumAttackCooldownMultiplier = Mathf.Max(
            minimumAttackCooldownMultiplier,
            maximumAttackCooldownMultiplier);
        movementProbeDistance = Mathf.Max(0.05f, movementProbeDistance);
        obstacleProbeRadius = Mathf.Max(0f, obstacleProbeRadius);
        routeValidationInterval = Mathf.Max(0.05f, routeValidationInterval);
        stuckTimeout = Mathf.Max(0.1f, stuckTimeout);
        minimumProgressDistance = Mathf.Max(0.001f, minimumProgressDistance);
        regionalTargetSearchAttempts = Mathf.Max(1, regionalTargetSearchAttempts);
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

        desiredCombatVelocity = Vector2.zero;
        previousTargetDistance = 0f;
        lastCombatDecisionTime = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        DrawChaseRanges(detectionRange, giveUpRange);
        DrawCombatRanges();
        DrawPatrolTarget();
    }

    private void DrawCombatRanges()
    {
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, preferredCombatDistance - combatDistanceTolerance);
        Gizmos.DrawWireSphere(transform.position, preferredCombatDistance + combatDistanceTolerance);
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
