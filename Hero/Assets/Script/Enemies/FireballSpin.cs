using UnityEngine;

public class FireballSpin : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 7f;
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private float angularSpeed = 4f; // radians/sec
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float selfSpinSpeed = 720; // degrees per second


    private Transform enemyTarget;
    private float angle;
    private bool attached;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Always spin around itself
        transform.Rotate(0f, 0f, selfSpinSpeed * Time.deltaTime);

        if (!attached)
        {
            // Fly forward first
            transform.Translate(Vector2.right * speed * Time.deltaTime);
            return;
        }

        if (enemyTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        // --- ORBIT AROUND ENEMY ---
        angle += angularSpeed * Time.deltaTime;

        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Sin(angle) * radius;

        transform.position = enemyTarget.position + new Vector3(x, y, 0f);
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        // Attach to enemy once
        if (!attached && other.CompareTag("Enemy"))
        {
            enemyTarget = other.transform;
            attached = true;

            angle = Random.Range(0f, Mathf.PI * 2f);
            return;
        }

        // Damage ONLY if player is hit
        if (attached && other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
                Destroy(gameObject); // remove fireball after hit
            }
        }
    }
}
