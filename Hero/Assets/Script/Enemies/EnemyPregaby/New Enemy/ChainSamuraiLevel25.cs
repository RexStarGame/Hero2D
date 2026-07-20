using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level-25 dual-attack enemy controller.
///
/// Attack 1 is a short, aimed chain slash.
/// Attack 2 is a slower, clearly telegraphed circular chain sweep.
/// Damage is passed through EnemyDifficultyProfile and PlayerHealth, so
/// difficulty, armour/defence, SafeZones and future authority rules stay shared.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyAggro2D))]
public sealed class ChainSamuraiLevel25 : MonoBehaviour
{
    [Header("Movement and awareness")]
    [Min(0f)] [SerializeField] private float moveSpeed = 3.25f;
    [Min(0.1f)] [SerializeField] private float detectionRange = 8f;
    [Min(0.1f)] [SerializeField] private float giveUpRange = 13f;
    [Min(0f)] [SerializeField] private float combatStopDistance = 1.35f;
    [Min(0f)] [SerializeField] private float patrolWaitTime = 1.5f;
    [Min(0.05f)] [SerializeField] private float routeValidationInterval = 0.2f;

    [Header("Attack 1 - aimed chain slash")]
    [Min(0.1f)] [SerializeField] private float slashRange = 2.25f;
    [Range(1f, 180f)] [SerializeField] private float slashHalfAngle = 55f;
    [Min(0f)] [SerializeField] private float slashDamage = 18f;
    [Min(0f)] [SerializeField] private float slashWindup = 0.28f;
    [Min(0f)] [SerializeField] private float slashRecovery = 0.28f;
    [Min(0.05f)] [SerializeField] private float slashCooldown = 1.4f;

    [Header("Attack 2 - circular chain sweep")]
    [Min(0.1f)] [SerializeField] private float sweepRange = 3.8f;
    [Min(0f)] [SerializeField] private float sweepDamage = 28f;
    [Min(0f)] [SerializeField] private float sweepWindup = 0.85f;
    [Min(0f)] [SerializeField] private float sweepRecovery = 0.55f;
    [Min(0.05f)] [SerializeField] private float sweepCooldown = 5.5f;
    [Range(0f, 1f)] [SerializeField] private float sweepChance = 0.35f;

    [Header("Targets")]
    [Tooltip("Only colliders on these layers can be considered attack targets.")]
    [SerializeField] private LayerMask playerLayers = ~0;

    [Header("Telegraph")]
    [SerializeField] private Color slashWarningColor = new Color(1f, 0.72f, 0.15f, 0.9f);
    [SerializeField] private Color sweepWarningColor = new Color(1f, 0.16f, 0.08f, 0.9f);
    [Min(0.01f)] [SerializeField] private float warningLineWidth = 0.07f;
    [Range(8, 96)] [SerializeField] private int warningSegments = 48;
    [SerializeField] private LineRenderer warningLine;

    [Header("Animator parameter names")]
    [Tooltip("The supplied Orc art faces right and uses horizontal flipping instead of four directional clips.")]
    [SerializeField] private bool useHorizontalSpriteFlipping = true;
    [SerializeField] private bool sourceSpriteFacesRight = true;
    [SerializeField] private SpriteRenderer facingSprite;
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string moveYParameter = "MoveY";
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string slashTrigger = "Attack1";
    [SerializeField] private string sweepTrigger = "Attack2";
    [SerializeField] private string hurtTrigger = "Hurt";
    [SerializeField] private string deathTrigger = "Die";

    [Header("Hit reaction")]
    [Tooltip("The supplied Hurt clip is 4 frames at 0.1 seconds per frame.")]
    [Min(0f)] [SerializeField] private float hurtLockDuration = 0.4f;

    [Header("Scene debug")]
    [SerializeField] private bool drawRanges = true;

    private readonly HashSet<PlayerHealth> damagedPlayers = new HashSet<PlayerHealth>();
    private Rigidbody2D body;
    private EnemyHealth health;
    private Animator animator;
    private EnemyAggro2D aggro;
    private EnemyDifficultyProfile difficultyProfile;
    private EnemyManager enemyManager;
    private Material runtimeWarningMaterial;
    private Coroutine attackRoutine;
    private Coroutine hurtRoutine;
    private Coroutine patrolWaitRoutine;
    private Vector2 patrolTarget;
    private Vector2 lastFacing = Vector2.down;
    private float nextSlashTime;
    private float nextSweepTime;
    private float nextRouteValidationTime;
    private bool patrolActive;
    private bool dead;

    public bool IsAttacking => attackRoutine != null;
    public bool IsHurt => hurtRoutine != null;
    public float SlashRange => slashRange;
    public float SweepRange => sweepRange;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        animator = GetComponent<Animator>();
        aggro = GetComponent<EnemyAggro2D>();
        difficultyProfile = GetComponent<EnemyDifficultyProfile>();
        if (facingSprite == null)
            facingSprite = GetComponentInChildren<SpriteRenderer>(true);

        if (difficultyProfile == null)
            difficultyProfile = gameObject.AddComponent<EnemyDifficultyProfile>();

        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        aggro.ConfigureRanges(detectionRange, giveUpRange);
        BuildWarningLineIfNeeded();
        HideWarning();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<EnemyHealth>();

        if (health != null)
        {
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
        }
    }

    private void Start()
    {
        enemyManager = FindAny<EnemyManager>();
        ChooseNewPatrolPoint();
    }

    private void Update()
    {
        if (dead || aggro == null || !aggro.HasAuthority())
            return;

        if (IsHurt)
        {
            UpdateMovementAnimation(lastFacing, false);
            return;
        }

        Transform target = aggro.CurrentTarget;
        if (target != null && SafeZone2D.IsPlayerProtected(target.position))
        {
            aggro.ClearTarget();
            target = null;
        }

        if (target != null)
        {
            patrolActive = false;
            UpdateCombat(target);
        }
        else
        {
            UpdatePatrol();
        }
    }

    private void FixedUpdate()
    {
        if (dead || body == null || aggro == null || !aggro.HasAuthority())
            return;

        if (IsAttacking || IsHurt)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Transform target = aggro.CurrentTarget;
        if (target != null && !SafeZone2D.IsPlayerProtected(target.position))
        {
            Vector2 offset = (Vector2)target.position - body.position;
            body.linearVelocity = offset.sqrMagnitude > combatStopDistance * combatStopDistance
                ? offset.normalized * moveSpeed
                : Vector2.zero;
            return;
        }

        body.linearVelocity = patrolActive
            ? (patrolTarget - body.position).normalized * moveSpeed
            : Vector2.zero;
    }

    private void UpdateCombat(Transform target)
    {
        Vector2 offset = (Vector2)target.position - (Vector2)transform.position;
        float distance = offset.magnitude;
        if (offset.sqrMagnitude > 0.0001f)
        {
            lastFacing = offset.normalized;
            UpdateMovementAnimation(lastFacing, !IsAttacking && distance > combatStopDistance);
        }

        if (IsAttacking)
            return;

        bool sweepReady = Time.time >= nextSweepTime && distance <= sweepRange;
        bool slashReady = Time.time >= nextSlashTime && distance <= slashRange;

        // The sweep is guaranteed when the target is outside slash reach.
        // At close range it is deliberately less common and therefore readable.
        if (sweepReady && (!slashReady || Random.value < sweepChance))
        {
            attackRoutine = StartCoroutine(PerformSweep());
        }
        else if (slashReady)
        {
            attackRoutine = StartCoroutine(PerformSlash(lastFacing));
        }
    }

    private void UpdatePatrol()
    {
        if (IsAttacking || enemyManager == null)
        {
            UpdateMovementAnimation(lastFacing, false);
            return;
        }

        if (!patrolActive)
        {
            if (patrolWaitRoutine == null)
                patrolWaitRoutine = StartCoroutine(WaitThenChoosePatrolPoint());

            UpdateMovementAnimation(lastFacing, false);
            return;
        }

        if (Time.time >= nextRouteValidationTime)
        {
            nextRouteValidationTime = Time.time + routeValidationInterval;
            if (!enemyManager.IsPatrolRouteValid(transform.position, patrolTarget))
            {
                ChooseNewPatrolPoint();
                return;
            }
        }

        Vector2 offset = patrolTarget - (Vector2)transform.position;
        if (offset.sqrMagnitude <= 0.04f)
        {
            patrolActive = false;
            return;
        }

        lastFacing = offset.normalized;
        UpdateMovementAnimation(lastFacing, true);
    }

    private IEnumerator PerformSlash(Vector2 lockedDirection)
    {
        body.linearVelocity = Vector2.zero;
        lastFacing = lockedDirection.sqrMagnitude > 0.0001f
            ? lockedDirection.normalized
            : lastFacing;
        UpdateMovementAnimation(lastFacing, false);
        SetAnimatorTrigger(slashTrigger);
        ShowSlashWarning(lastFacing);

        yield return new WaitForSeconds(slashWindup);
        HideWarning();

        if (aggro.CurrentTarget != null &&
            !SafeZone2D.IsPlayerProtected(aggro.CurrentTarget.position))
        {
            DamagePlayersInSlash(lastFacing);
        }

        nextSlashTime = Time.time + slashCooldown;
        yield return new WaitForSeconds(slashRecovery);
        attackRoutine = null;
    }

    private IEnumerator PerformSweep()
    {
        body.linearVelocity = Vector2.zero;
        UpdateMovementAnimation(lastFacing, false);
        SetAnimatorTrigger(sweepTrigger);
        ShowSweepWarning();

        yield return new WaitForSeconds(sweepWindup);
        HideWarning();

        Transform target = aggro.CurrentTarget;
        if (target != null && !SafeZone2D.IsPlayerProtected(target.position))
            DamagePlayersInCircle(sweepRange, sweepDamage);

        nextSweepTime = Time.time + sweepCooldown;
        // Also leave a short gap before a follow-up slash.
        nextSlashTime = Mathf.Max(nextSlashTime, Time.time + sweepRecovery);
        yield return new WaitForSeconds(sweepRecovery);
        attackRoutine = null;
    }

    private void DamagePlayersInSlash(Vector2 direction)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slashRange, playerLayers);
        damagedPlayers.Clear();

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerHealth player = hits[i] != null
                ? hits[i].GetComponentInParent<PlayerHealth>()
                : null;
            if (!CanDamage(player) || !damagedPlayers.Add(player))
                continue;

            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude <= 0.0001f ||
                Vector2.Angle(direction, toPlayer) <= slashHalfAngle)
            {
                DealDamage(player, slashDamage);
            }
        }
    }

    private void DamagePlayersInCircle(float radius, float baseDamage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, playerLayers);
        damagedPlayers.Clear();

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerHealth player = hits[i] != null
                ? hits[i].GetComponentInParent<PlayerHealth>()
                : null;
            if (CanDamage(player) && damagedPlayers.Add(player))
                DealDamage(player, baseDamage);
        }
    }

    private static bool CanDamage(PlayerHealth player)
    {
        return player != null && player.isActiveAndEnabled &&
               !SafeZone2D.IsPlayerProtected(player.transform.position);
    }

    private void DealDamage(PlayerHealth player, float baseDamage)
    {
        float scaledDamage = difficultyProfile != null
            ? difficultyProfile.ScaleDamage(baseDamage)
            : baseDamage;
        player.TakeDamage(scaledDamage);
    }

    private void ChooseNewPatrolPoint()
    {
        if (enemyManager == null)
        {
            patrolActive = false;
            return;
        }

        patrolTarget = enemyManager.GetRandomPointInZone(transform.position);
        nextRouteValidationTime = Time.time + routeValidationInterval;
        patrolActive = Vector2.Distance(transform.position, patrolTarget) > 0.2f;
    }

    private IEnumerator WaitThenChoosePatrolPoint()
    {
        yield return new WaitForSeconds(patrolWaitTime);
        ChooseNewPatrolPoint();
        patrolWaitRoutine = null;
    }

    private void BuildWarningLineIfNeeded()
    {
        if (warningLine == null)
        {
            GameObject warningObject = new GameObject("ChainAttackWarning");
            warningObject.transform.SetParent(transform, false);
            warningLine = warningObject.AddComponent<LineRenderer>();
        }

        warningLine.useWorldSpace = false;
        warningLine.loop = false;
        warningLine.startWidth = warningLineWidth;
        warningLine.endWidth = warningLineWidth;
        warningLine.numCapVertices = 2;
        warningLine.numCornerVertices = 2;
        warningLine.sortingOrder = 50;

        if (warningLine.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

            if (shader != null)
            {
                runtimeWarningMaterial = new Material(shader)
                {
                    name = "Chain Warning (Runtime)"
                };
                warningLine.sharedMaterial = runtimeWarningMaterial;
            }
        }
    }

    private void ShowSlashWarning(Vector2 direction)
    {
        if (warningLine == null)
            return;

        int segments = Mathf.Max(8, warningSegments / 2);
        warningLine.loop = false;
        warningLine.positionCount = segments + 2;
        warningLine.startColor = slashWarningColor;
        warningLine.endColor = slashWarningColor;
        warningLine.SetPosition(0, Vector3.zero);

        float centre = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = (centre - slashHalfAngle + t * slashHalfAngle * 2f) * Mathf.Deg2Rad;
            warningLine.SetPosition(i + 1, new Vector3(
                Mathf.Cos(angle) * slashRange,
                Mathf.Sin(angle) * slashRange,
                0f));
        }

        warningLine.enabled = true;
    }

    private void ShowSweepWarning()
    {
        if (warningLine == null)
            return;

        int segments = Mathf.Max(8, warningSegments);
        warningLine.loop = true;
        warningLine.positionCount = segments;
        warningLine.startColor = sweepWarningColor;
        warningLine.endColor = sweepWarningColor;

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            warningLine.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * sweepRange,
                Mathf.Sin(angle) * sweepRange,
                0f));
        }

        warningLine.enabled = true;
    }

    private void HideWarning()
    {
        if (warningLine != null)
            warningLine.enabled = false;
    }

    private void UpdateMovementAnimation(Vector2 direction, bool moving)
    {
        if (animator == null)
            return;

        if (direction.sqrMagnitude > 0.0001f)
        {
            if (useHorizontalSpriteFlipping)
            {
                if (facingSprite != null && Mathf.Abs(direction.x) > 0.05f)
                {
                    bool lookingRight = direction.x > 0f;
                    facingSprite.flipX = sourceSpriteFacesRight ? !lookingRight : lookingRight;
                }
            }
            else
            {
                SetAnimatorFloat(moveXParameter, direction.x);
                SetAnimatorFloat(moveYParameter, direction.y);
            }
        }

        SetAnimatorFloat(speedParameter, moving ? 1f : 0f);
    }

    private void SetAnimatorFloat(string parameterName, float value)
    {
        if (!string.IsNullOrWhiteSpace(parameterName) && HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            animator.SetFloat(parameterName, value);
    }

    private void SetAnimatorTrigger(string parameterName)
    {
        if (!string.IsNullOrWhiteSpace(parameterName) && HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(parameterName);
    }

    private void ResetAnimatorTrigger(string parameterName)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(parameterName) &&
            HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(parameterName);
        }
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == type && parameters[i].name == parameterName)
                return true;
        }

        return false;
    }

    private void HandleDamaged()
    {
        if (dead)
            return;

        // A visible hit reaction must also interrupt the gameplay attack;
        // otherwise the animation says "hurt" while invisible damage still lands.
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        HideWarning();
        if (body != null)
            body.linearVelocity = Vector2.zero;

        ResetAnimatorTrigger(slashTrigger);
        ResetAnimatorTrigger(sweepTrigger);
        SetAnimatorFloat(speedParameter, 0f);

        if (hurtRoutine != null)
            StopCoroutine(hurtRoutine);

        hurtRoutine = StartCoroutine(PlayHurtReaction());
    }

    private IEnumerator PlayHurtReaction()
    {
        SetAnimatorTrigger(hurtTrigger);
        yield return new WaitForSeconds(hurtLockDuration);
        hurtRoutine = null;
    }

    private void HandleDied()
    {
        if (dead)
            return;

        dead = true;
        HideWarning();

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
            hurtRoutine = null;
        }

        if (patrolWaitRoutine != null)
        {
            StopCoroutine(patrolWaitRoutine);
            patrolWaitRoutine = null;
        }

        if (body != null)
            body.linearVelocity = Vector2.zero;

        if (aggro != null)
            aggro.ClearTarget();

        Collider2D bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider != null)
            bodyCollider.enabled = false;

        SetAnimatorFloat(speedParameter, 0f);
        ResetAnimatorTrigger(hurtTrigger);
        SetAnimatorTrigger(deathTrigger);
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
        }

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
            hurtRoutine = null;
        }

        if (patrolWaitRoutine != null)
        {
            StopCoroutine(patrolWaitRoutine);
            patrolWaitRoutine = null;
        }

        HideWarning();
        if (body != null && (aggro == null || aggro.HasAuthority()))
            body.linearVelocity = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (runtimeWarningMaterial != null)
            Destroy(runtimeWarningMaterial);
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        detectionRange = Mathf.Max(0.1f, detectionRange);
        giveUpRange = Mathf.Max(detectionRange, giveUpRange);
        combatStopDistance = Mathf.Max(0f, combatStopDistance);
        slashRange = Mathf.Max(0.1f, slashRange);
        sweepRange = Mathf.Max(slashRange, sweepRange);
        slashDamage = Mathf.Max(0f, slashDamage);
        sweepDamage = Mathf.Max(0f, sweepDamage);
        warningLineWidth = Mathf.Max(0.01f, warningLineWidth);
        warningSegments = Mathf.Clamp(warningSegments, 8, 96);

        if (warningLine != null)
        {
            warningLine.startWidth = warningLineWidth;
            warningLine.endWidth = warningLineWidth;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawRanges)
            return;

        Gizmos.color = new Color(1f, 0.75f, 0.12f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, slashRange);
        Gizmos.color = new Color(1f, 0.12f, 0.08f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, sweepRange);
        Gizmos.color = new Color(0.25f, 0.8f, 1f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(0.6f, 0.35f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, giveUpRange);
    }

    private static T FindAny<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindAnyObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
