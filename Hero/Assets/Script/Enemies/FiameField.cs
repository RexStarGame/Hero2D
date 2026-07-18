using UnityEngine;
using System.Reflection;


public class FlameField : MonoBehaviour, IDifficultyScaledEnemyDamage
{
    [Header("Movement (projectile)")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float maxLifeTime = 6f;

    [Header("Impact / Explosion")]
    [SerializeField] private LayerMask impactLayers = ~0;
    [SerializeField] private float fuseTime = 0f;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private float explosionDamage = 0f;
    [SerializeField] private GameObject explosionVfx;

    [Header("Burning Field (AoE)")]
    [SerializeField] private LayerMask damageLayers;               
    [SerializeField] private float burnRadius = 2.5f;
    [SerializeField] private float burnDuration = 3f;
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float tickInterval = 0.2f;        
    [SerializeField] private GameObject burnVfx;                  
    [Header("Damage Call")]
    [SerializeField] private string damageMethodName = "TakeDamage";
    private Rigidbody2D rb;
    private Collider2D col;

    private bool exploded = false;
    private float lifeTimer;
    private float fuseTimer;

    private float burnTimer;
    private float tickTimer;

    private Collider2D[] hits = new Collider2D[32];
    private GameObject burnVfxInstance;
    private ContactFilter2D damageFilter;
    private float difficultyDamageMultiplier = 1f;

    public void SetDifficultyDamageMultiplier(float multiplier)
    {
        difficultyDamageMultiplier = Mathf.Max(0f, multiplier);
    }

    private void Awake()
    {
        difficultyDamageMultiplier = EnemyDifficultyProfile.GetDefaultDamageMultiplier();
        EnemyDifficultyProfile sourceProfile =
            GetComponentInParent<EnemyDifficultyProfile>();
        if (sourceProfile != null)
            difficultyDamageMultiplier = sourceProfile.DamageMultiplier;

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        //col.isTrigger = true;
        damageFilter = new ContactFilter2D();
        damageFilter.useLayerMask = true;
        damageFilter.SetLayerMask(damageLayers);

        damageFilter.useTriggers = Physics2D.queriesHitTriggers;
    }
    private void Start()
    {
        rb.linearVelocity = (Vector2)transform.right * speed;

        lifeTimer = maxLifeTime;
        fuseTimer = fuseTime;
    }
    private void Update()
    {
        if (exploded)
        {
            RunBurnField();
            return;
        }
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Explode();
            return;
        }
        // exploder efter fuseTime
        if (fuseTime > 0f)
        {
            fuseTimer -= Time.deltaTime;
            if (fuseTimer <= 0f)
            {
                Explode();
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exploded) return;

        // ignor�r egne colliders
        if (other == col) return;

        // tjek layer mask
        if (((1 << other.gameObject.layer) & impactLayers) == 0)
            return;

        Explode();
    }
    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
       
        if (explosionVfx != null)
        {
            var explosionInstance = Instantiate(explosionVfx, transform.position, Quaternion.identity);
            Destroy(explosionInstance); // instant despawn
        }
        // instant AoE dmg
        if (explosionDamage > 0f)
        {
            damageFilter.SetLayerMask(damageLayers);
            int count = Physics2D.OverlapCircle((Vector2)transform.position, explosionRadius, damageFilter, hits);

            for (int i = 0; i < count; i++)
                TryDealDamage(hits[i], explosionDamage);
        }
        // switch collider to burn area
        if (col is CircleCollider2D cc)
        {
            cc.radius = burnRadius;
            cc.isTrigger = true;
        }
        else
        {
            Destroy(col);
            var newCc = gameObject.AddComponent<CircleCollider2D>();
            newCc.isTrigger = true;
            newCc.radius = burnRadius;
            col = newCc;
        }
        if (burnVfx != null)
            burnVfxInstance = Instantiate(burnVfx, transform.position, Quaternion.identity);

        burnTimer = burnDuration;
        tickTimer = 0f;
    }
    private void RunBurnField()
    {
        burnTimer -= Time.deltaTime;

        if (burnVfxInstance != null)
            burnVfxInstance.transform.position = transform.position;

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            float dt = tickInterval;
            tickTimer = 0f;

            float dmg = damagePerSecond * dt;

            damageFilter.SetLayerMask(damageLayers);
            int count = Physics2D.OverlapCircle((Vector2)transform.position, burnRadius, damageFilter, hits);

            for (int i = 0; i < count; i++)
                TryDealDamage(hits[i], dmg);
        }
        if (burnTimer <= 0f)
        {
            if (burnVfxInstance != null)
            {
                Destroy(burnVfxInstance);
            }

            Destroy(gameObject);
        }
    }
    private void TryDealDamage(Collider2D targetCol, float dmg)
    {
        if (targetCol == null) return;

        float scaledDamage = dmg * difficultyDamageMultiplier;

        // Find en MonoBehaviour p� target og pr�v at kalde TakeDamage(float)
        var behaviours = targetCol.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            var b = behaviours[i];
            if (b == null) continue;

            var t = b.GetType();

            // TakeDamage(float)
            MethodInfo mFloat = t.GetMethod(damageMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
            if (mFloat != null)
            {
                DifficultyDebugTelemetry.RecordEnemyDamage(
                    this, dmg, scaledDamage);
                mFloat.Invoke(b, new object[] { scaledDamage });
                return;
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        Gizmos.DrawWireSphere(transform.position, burnRadius);
    }
}
