using System.Collections;
using UnityEngine;

public class EnemySpiralShooter : MonoBehaviour
{
    [Header("Prefab + Spawn Point")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Targeting")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackCooldown = 2.5f;

    [Header("Telegraph (warning before spiral)")]
    [SerializeField] private float windupTime = 0.45f;
    [SerializeField] private GameObject telegraphPrefab;   // exclamation/glow prefab
    [SerializeField] private Vector3 telegraphOffset = new Vector3(0, 0.6f, 0);
    [SerializeField] private AudioClip windupSfx;

    [Header("Spiral Pattern")]
    [SerializeField] private int bulletCount = 24;
    [SerializeField] private float shotDelay = 0.06f;
    [SerializeField] private float angleStep = 15f;
    [SerializeField] private bool clockwise = true;
    [SerializeField] private bool lockFirstAim = true;

    // ----- Cooldown/Ready info (like PlayerAttack) -----
    public bool CanShoot => !isShooting && Time.time >= AttackReadyTime;
    public float AttackReadyTime { get; private set; } // Time.time when next burst can start

    public float BurstDuration => windupTime + (bulletCount * shotDelay);
    public float AttackCycleDuration => BurstDuration + attackCooldown;

    public float CooldownRemaining => Mathf.Max(0f, AttackReadyTime - Time.time);

    public float Cooldown01
    {
        get
        {
            float d = Mathf.Max(0.0001f, AttackCycleDuration);
            return Mathf.Clamp01(1f - (CooldownRemaining / d));
        }
    }
    // -----------------------------------------------

    private Transform player;
    private bool isShooting;
    private Coroutine shootRoutine;
    private AudioSource audioSource;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (firePoint == null) firePoint = transform;

        // Start as ready
        AttackReadyTime = Time.time;

        audioSource = GetComponent<AudioSource>(); // optional (add AudioSource if you want SFX)
    }

    private void Update()
    {
        if (player == null || bulletPrefab == null) return;
        if (isShooting || shootRoutine != null) return;
        if (Time.time < AttackReadyTime) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > attackRange) return;

        shootRoutine = StartCoroutine(WindupAndSpiral());
    }

    private IEnumerator WindupAndSpiral()
    {
        isShooting = true;

        // Set when we will be ready again (includes windup + burst + cooldown)
        AttackReadyTime = Time.time + AttackCycleDuration;

        // Snapshot player's position ONCE (first bullet aims here)
        Vector2 targetPos = player.position;

        // Spawn telegraph (warning icon/glow)
        GameObject telegraphInstance = null;
        if (telegraphPrefab != null)
            telegraphInstance = Instantiate(telegraphPrefab, transform.position + telegraphOffset, Quaternion.identity, transform);

        // Play windup SFX (optional)
        if (windupSfx != null)
        {
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.PlayOneShot(windupSfx);
        }

        // Wind-up delay
        if (windupTime > 0f)
            yield return new WaitForSeconds(windupTime);

        if (telegraphInstance != null)
            Destroy(telegraphInstance);

        // Base direction for first bullet
        Vector2 baseDir = (targetPos - (Vector2)firePoint.position).normalized;
        if (baseDir.sqrMagnitude < 0.001f) baseDir = Vector2.right;

        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        float step = clockwise ? -angleStep : angleStep;

        for (int i = 0; i < bulletCount; i++)
        {
            float ang = (i == 0 && lockFirstAim) ? baseAngle : (baseAngle + (i * step));
            Quaternion rot = Quaternion.Euler(0f, 0f, ang);

            Instantiate(bulletPrefab, firePoint.position, rot);

            if (shotDelay > 0f)
                yield return new WaitForSeconds(shotDelay);
        }

        // Cooldown happens automatically via AttackReadyTime
        isShooting = false;
        shootRoutine = null;
    }

    private void OnDisable()
    {
        if (shootRoutine != null)
        {
            StopCoroutine(shootRoutine);
            shootRoutine = null;
        }
        isShooting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
