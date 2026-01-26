using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Indstillinger")]
    [SerializeField] private float speed = 7f;
    [SerializeField] private float lifeTime = 4f;

    [SerializeField] private float damage = 10f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Hit: " + other.name + " | Tag: " + other.tag);
        // Ignore enemies (including the shooter)
        if (other.CompareTag("Enemy")) return;

        // Ignore zone/controller if needed
        if (other.CompareTag("GameController")) return;

        // Damage player if PlayerHealth exists (works even if collider is on a child)
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }
        if (other.CompareTag("Environment"))
        {
            Destroy(gameObject);
            return;
        }


        // Anything else (walls, props, etc.)
        Destroy(gameObject);
    }
}
