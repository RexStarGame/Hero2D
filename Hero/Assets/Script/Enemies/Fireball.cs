using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Fireball : MonoBehaviour, IDifficultyScaledEnemyDamage
{
    [Header("Movement")]
    [SerializeField] private float speed = 7f;
    [SerializeField] private float lifeTime = 4f;

    [Header("Damage")]
    [SerializeField] private float damage = 10f;

    [Header("Layer Rules (set in Inspector)")]
    [Tooltip("Layers the projectile should ignore completely (no destroy, no damage).")]
    [SerializeField] private LayerMask ignoreLayers;

    [Tooltip("Layers that should be damaged (Player layer etc.).")]
    [SerializeField] private LayerMask damageLayers;

    [Tooltip("Layers that destroy the projectile (Walls, Obstacles, Tilemap collider layer, etc.).")]
    [SerializeField] private LayerMask destroyOnLayers;

    private Rigidbody2D rb;
    private Collider2D col;
    private float difficultyDamageMultiplier = 1f;

    public void SetDifficultyDamageMultiplier(float multiplier)
    {
        difficultyDamageMultiplier = Mathf.Max(0f, multiplier);
    }

    private void Awake()
    {
        difficultyDamageMultiplier = EnemyDifficultyProfile.GetDefaultDamageMultiplier();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Make it stable for projectiles
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Use trigger hits (recommended)
        col.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (SafeZone2D.IsEnemyProjectileBlocked(transform.position))
        {
            Destroy(gameObject);
            return;
        }

        // Always move forward based on rotation
        rb.linearVelocity = (Vector2)transform.right * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SafeZone2D safeZone = other.GetComponent<SafeZone2D>();
        if (safeZone != null && safeZone.DestroysEnemyProjectiles)
        {
            Destroy(gameObject);
            return;
        }

        int otherLayer = other.gameObject.layer;

        Debug.Log($"Hit: {other.name} | Layer: {LayerMask.LayerToName(otherLayer)} | Tag: {other.tag}");

        // 1) Ignore layers
        if (((1 << otherLayer) & ignoreLayers) != 0)
            return;

        // 2) Damage layers (player)
        if (((1 << otherLayer) & damageLayers) != 0)
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                float scaledDamage = damage * difficultyDamageMultiplier;
                DifficultyDebugTelemetry.RecordEnemyDamage(
                    this, damage, scaledDamage);
                health.TakeDamage(scaledDamage);
            }

            Destroy(gameObject);
            return;
        }

        // 3) Destroy on environment/obstacles
        if (((1 << otherLayer) & destroyOnLayers) != 0)
        {
            Destroy(gameObject);
            return;
        }
    }
}
