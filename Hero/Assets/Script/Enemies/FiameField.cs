using UnityEngine;
using System.Reflection;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FlameField : MonoBehaviour
{
    [Header("Movement (projectile)")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float maxLifeTime = 6f;

    [Header("Impact / Explosion")]
    [SerializeField] private LayerMask impactLayers = ~0;          // Hvad kan den ramme for at explodere
    [SerializeField] private float fuseTime = 0f;                  // 0 = kun ved hit, ellers exploder efter X sek
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private float explosionDamage = 0f;           // s�t >0 hvis du vil have instant dmg ved explosion
    [SerializeField] private GameObject explosionVfx;

    [Header("Burning Field (AoE)")]
    [SerializeField] private LayerMask damageLayers;               // Hvem tager skade (fx Player layer)
    [SerializeField] private float burnRadius = 2.5f;
    [SerializeField] private float burnDuration = 3f;
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float tickInterval = 0.2f;            // hvor ofte vi giver dmg (stabilt dps)
    [SerializeField] private GameObject burnVfx;                   // valgfri (fx particle som bliver siddende)

    [Header("Damage Call")]
    [SerializeField] private string damageMethodName = "TakeDamage"; // hvis din PlayerHealth har TakeDamage(...)

    private Rigidbody2D rb;
    private Collider2D col;

    private bool exploded = false;
    private float lifeTimer;
    private float fuseTimer;

    private float burnTimer;
    private float tickTimer;

    private Collider2D[] hits = new Collider2D[32];
    private GameObject burnVfxInstance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Topdown standard
        rb.gravityScale = 0f;

        // Hvis du bruger trigger-collision til hit:
        // (Hvis din collider er non-trigger og du vil bruge OnCollisionEnter2D, s� skift selv)
        col.isTrigger = true;
    }

    private void Start()
    {
        // Flyv i den retning prefab�en er roteret (EnemyAttack s�tter rotation)
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

        // auto-destruction hvis den aldrig rammer noget
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Explode();
            return;
        }

        // optional: exploder efter fuseTime
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

        // stop projectile
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        // VFX explosion
        if (explosionVfx != null)
            Instantiate(explosionVfx, transform.position, Quaternion.identity);

        // instant AoE dmg (valgfrit)
        if (explosionDamage > 0f)
        {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, explosionRadius, hits, damageLayers);
            for (int i = 0; i < count; i++)
                TryDealDamage(hits[i], explosionDamage);
        }

        // skift collider til burn-area
        if (col is CircleCollider2D cc)
        {
            cc.radius = burnRadius;
            cc.isTrigger = true;
        }
        else
        {
            // hvis ikke circle collider, s� tilf�j en
            Destroy(col);
            var newCc = gameObject.AddComponent<CircleCollider2D>();
            newCc.isTrigger = true;
            newCc.radius = burnRadius;
            col = newCc;
        }

        // burn VFX (valgfrit)
        if (burnVfx != null)
        {
            burnVfxInstance = Instantiate(burnVfx, transform.position, Quaternion.identity);
        }

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

            int count = Physics2D.OverlapCircleNonAlloc(transform.position, burnRadius, hits, damageLayers);
            for (int i = 0; i < count; i++)
                TryDealDamage(hits[i], dmg);
        }

        if (burnTimer <= 0f)
        {
            if (burnVfxInstance != null) Destroy(burnVfxInstance);
            Destroy(gameObject);
        }
    }

    private void TryDealDamage(Collider2D targetCol, float dmg)
    {
        if (targetCol == null) return;

        // Find en MonoBehaviour p� target og pr�v at kalde TakeDamage(float) eller TakeDamage(int)
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
                mFloat.Invoke(b, new object[] { dmg });
                return;
            }

            // TakeDamage(int)
            MethodInfo mInt = t.GetMethod(damageMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
            if (mInt != null)
            {
                mInt.Invoke(b, new object[] { Mathf.RoundToInt(dmg) });
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
