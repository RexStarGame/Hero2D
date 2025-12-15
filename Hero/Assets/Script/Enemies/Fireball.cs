using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Indstillinger")]
    [Tooltip("Hvor hurtigt flyver fireballen?")]
    [SerializeField] private float speed = 7f;

    [Tooltip("Hvor mange sekunder går der før den forsvinder af sig selv?")]
    [SerializeField] private float lifeTime = 4f;

    void Start()
    {
        // Denne linje fortæller Unity: "Slet mig om 'lifeTime' sekunder"
        // Det sker helt automatisk i baggrunden.
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Flyv fremad i den retning vi kigger (Vector2.right er "fremad" i 2D rotation)
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    // Denne funktion kører, når fireballens Collider rammer en anden Collider
    void OnTriggerEnter2D(Collider2D other)
    {
        // Vi tjekker ikke HVAD vi rammer. Rammer vi noget, så dør fireballen.
        // (Her kan du senere tilføje skade til spilleren, før den dør)

        Destroy(gameObject);
    }
}