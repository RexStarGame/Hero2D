using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    // Vi bruger en Collider til visuelt at tegne området i Editoren
    [SerializeField] private BoxCollider2D patrolArea;

    // Denne funktion kan MageRed kalde for at få et lovligt punkt
    public Vector2 GetRandomPointInZone()
    {
        // Vi henter grænserne (Bounds) fra vores BoxCollider
        Bounds bounds = patrolArea.bounds;

        // Vælg et tilfældigt tal mellem venstre/højre og op/ned
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);

        return new Vector2(randomX, randomY);
    }
}