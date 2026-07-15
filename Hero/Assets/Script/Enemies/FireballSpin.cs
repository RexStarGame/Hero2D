using UnityEngine;

public class FireballSpin : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 7f;
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private float angularSpeed = 4f; 
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float selfSpinSpeed = 720; 


    private Transform enemyTarget;
    private float angle;
    private bool attached;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (SafeZone2D.IsEnemyProjectileBlocked(transform.position))
        {
            Destroy(gameObject);
            return;
        }

        // Always spin around itself
        transform.Rotate(0f, 0f, selfSpinSpeed * Time.deltaTime);

        if (!attached)
        {
            transform.Translate(Vector2.right * speed * Time.deltaTime);
            return;
        }

        if (enemyTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        // Spin fireball around the enemy
        angle += angularSpeed * Time.deltaTime;

        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Sin(angle) * radius;

        transform.position = enemyTarget.position + new Vector3(x, y, 0f);
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        SafeZone2D safeZone = other.GetComponent<SafeZone2D>();
        if (safeZone != null && safeZone.DestroysEnemyProjectiles)
        {
            Destroy(gameObject);
            return;
        }

        // Fireball attaches to the enemy
        if (!attached && other.CompareTag("Enemy"))
        {
            enemyTarget = other.transform;
            attached = true;

            angle = Random.Range(0f, Mathf.PI * 2f);
            return;
        }

        // Damage if the player gets hit
        if (attached && other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
