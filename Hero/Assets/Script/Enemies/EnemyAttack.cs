using System.Collections;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Indstillinger")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Kamp Stats")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackCooldown = 2f;

    private bool useVariableCooldown;
    private float minimumCooldownMultiplier = 1f;
    private float maximumCooldownMultiplier = 1f;

    [Header("Telegraph (Warning before shot)")]
    [SerializeField] private float windupTime = 0.45f;                 // tid før skuddet
    [SerializeField] private GameObject telegraphPrefab;               // fx ! icon eller glow sprite (valgfri)
    [SerializeField] private Vector3 telegraphOffset = new Vector3(0, 0.6f, 0);
    [SerializeField] private AudioClip windupSfx;                      // valgfri pip/charge lyd
    [SerializeField] private bool freezeDuringWindup = true;           // hvis du har movement script, kan du bruge dette som signal

    [Header("Optional Visual Blink")]
    [SerializeField] private SpriteRenderer enemySprite;               // assign enemy sprite renderer (valgfri)
    [SerializeField] private float blinkInterval = 0.08f;

    private Transform player;
    private float cooldownTimer;
    private Animator animator;
    private AudioSource audioSource;

    private bool isWindingUp;
    private GameObject telegraphInstance;
    private EnemyAggro2D aggro;
    private EnemyDifficultyProfile difficultyProfile;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>(); // optional (add AudioSource component if you use SFX)

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        else Debug.LogWarning("Mangler Player tag!");

        if (firePoint == null) firePoint = transform;

        // fallback: auto-find sprite if not assigned
        if (enemySprite == null) enemySprite = GetComponentInChildren<SpriteRenderer>();

        aggro = GetComponent<EnemyAggro2D>();
        if (aggro == null) aggro = gameObject.AddComponent<EnemyAggro2D>();

        difficultyProfile = GetComponentInParent<EnemyDifficultyProfile>();
        if (difficultyProfile == null)
            difficultyProfile = GetComponentInChildren<EnemyDifficultyProfile>(true);
    }

    void Update()
    {
        if (aggro != null) player = aggro.CurrentTarget;
        if (player == null) return;
        if (SafeZone2D.IsPlayerProtected(player.position)) return;

        cooldownTimer -= Time.deltaTime;
        if (isWindingUp) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange && cooldownTimer <= 0f)
        {
            StartCoroutine(WindupThenShoot());
        }
    }

    private IEnumerator WindupThenShoot()
    {
        isWindingUp = true;

        // Lock direction at windup start (fair telegraph)
        Vector2 direction = (player.position - firePoint.position).normalized;

        // Face player immediately
        UpdateFacing(direction);

        // Show telegraph object (optional)
        if (telegraphPrefab != null)
        {
            telegraphInstance = Instantiate(telegraphPrefab, transform.position + telegraphOffset, Quaternion.identity, transform);
        }

        // Play windup SFX (optional)
        if (windupSfx != null)
        {
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.PlayOneShot(windupSfx);
        }

        // Simple blink during windup (optional)
        float t = 0f;
        bool visible = true;

        while (t < windupTime)
        {
            t += Time.deltaTime;

            // keep telegraph positioned
            if (telegraphInstance != null)
                telegraphInstance.transform.position = transform.position + telegraphOffset;

            // blink sprite
            if (enemySprite != null)
            {
                // toggle visibility on a fixed interval
                // (pro tip: you can swap this to color flash instead if you prefer)
                if (blinkInterval > 0f)
                {
                    // manual timer using modulo style
                    float phase = Mathf.Repeat(t, blinkInterval * 2f);
                    bool shouldBeVisible = phase < blinkInterval;
                    if (shouldBeVisible != visible)
                    {
                        visible = shouldBeVisible;
                        enemySprite.enabled = visible;
                    }
                }
            }

            yield return null;
        }

        // restore sprite
        if (enemySprite != null) enemySprite.enabled = true;

        // remove telegraph
        if (telegraphInstance != null) Destroy(telegraphInstance);

        // shoot
        if (player != null && !SafeZone2D.IsPlayerProtected(player.position))
            Shoot(direction);

        // start cooldown AFTER the shot
        cooldownTimer = GetNextAttackCooldown();
        isWindingUp = false;
    }

    public void ConfigureCooldownVariation(float minimumMultiplier, float maximumMultiplier)
    {
        minimumCooldownMultiplier = Mathf.Max(0.1f, minimumMultiplier);
        maximumCooldownMultiplier = Mathf.Max(minimumCooldownMultiplier, maximumMultiplier);
        useVariableCooldown = true;
    }

    private float GetNextAttackCooldown()
    {
        if (!useVariableCooldown)
            return attackCooldown;

        return attackCooldown * Random.Range(
            minimumCooldownMultiplier,
            maximumCooldownMultiplier);
    }

    void Shoot(Vector2 direction)
    {
        UpdateFacing(direction);

        if (animator != null)
            animator.SetTrigger("Attack");

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        GameObject projectile = Instantiate(
            fireballPrefab, firePoint.position, rotation);
        if (difficultyProfile != null)
            difficultyProfile.ApplyToSpawnedDamage(projectile);
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (animator == null) return;

        animator.SetBool("IsFacingUp", direction.y > 0);
        animator.SetBool("IsFacingRight", direction.x > 0);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
