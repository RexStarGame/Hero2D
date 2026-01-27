using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 7f;
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private float damage = 10f;

    [Header("Layer Rules")]
    [SerializeField] private LayerMask ignoreLayers;
    [SerializeField] private LayerMask damageLayers;
    [SerializeField] private LayerMask destroyOnLayers;

    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        col.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = (Vector2)transform.right * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int layer = other.gameObject.layer;

        if (((1 << layer) & ignoreLayers) != 0)
            return;

        if (((1 << layer) & damageLayers) != 0)
        {
            var hp = other.GetComponentInParent<PlayerHealth>();
            if (hp != null) hp.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (((1 << layer) & destroyOnLayers) != 0)
        {
            Destroy(gameObject);
        }
    }
}
