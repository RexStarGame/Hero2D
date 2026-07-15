using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Patrol Area")]
    [SerializeField] private BoxCollider2D patrolArea;

    [Header("Safe-Zone Avoidance")]
    [Min(1)] [SerializeField] private int pointSearchAttempts = 32;
    [Min(0.05f)] [SerializeField] private float routeCheckSpacing = 0.25f;

    public Vector2 GetRandomPointInZone()
    {
        return GetRandomPointInZone(transform.position);
    }

    public Vector2 GetRandomPointInZone(Vector2 requesterPosition)
    {
        if (patrolArea == null)
        {
            Debug.LogWarning("[EnemyManager] Patrol Area is not assigned.", this);
            return requesterPosition;
        }

        Bounds bounds = patrolArea.bounds;
        int attempts = Mathf.Max(1, pointSearchAttempts);

        for (int i = 0; i < attempts; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y));

            if (SafeZone2D.IsEnemyMovementBlocked(candidate)) continue;
            if (RouteCrossesSafeZone(requesterPosition, candidate)) continue;

            return candidate;
        }

        // Staying still is safer than repeatedly walking into a forbidden zone.
        // The enemy will request another point after its normal wait cycle.
        return requesterPosition;
    }

    private bool RouteCrossesSafeZone(Vector2 start, Vector2 end)
    {
        float distance = Vector2.Distance(start, end);
        int checks = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.05f, routeCheckSpacing)));

        for (int i = 0; i <= checks; i++)
        {
            Vector2 sample = Vector2.Lerp(start, end, i / (float)checks);
            if (SafeZone2D.IsEnemyMovementBlocked(sample))
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        pointSearchAttempts = Mathf.Max(1, pointSearchAttempts);
        routeCheckSpacing = Mathf.Max(0.05f, routeCheckSpacing);
    }

    private void OnDrawGizmosSelected()
    {
        if (patrolArea == null) return;
        Gizmos.color = new Color(0.2f, 0.65f, 1f, 0.8f);
        Gizmos.DrawWireCube(patrolArea.bounds.center, patrolArea.bounds.size);
    }
}
