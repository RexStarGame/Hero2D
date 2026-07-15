using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Patrol Area")]
    [SerializeField] private BoxCollider2D patrolArea;

    [Header("Safe-Zone Avoidance")]
    [Min(1)] [SerializeField] private int pointSearchAttempts = 32;
    [Min(0.05f)] [SerializeField] private float routeCheckSpacing = 0.25f;
    [Min(0f)] [SerializeField] private float safeZoneClearance = 0.35f;

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

            if (!IsPatrolRouteValid(requesterPosition, candidate)) continue;

            return candidate;
        }

        // Staying still is safer than repeatedly walking into a forbidden zone.
        // The enemy will request another point after its normal wait cycle.
        return requesterPosition;
    }

    public bool IsPatrolRouteValid(Vector2 start, Vector2 end)
    {
        if (patrolArea == null) return false;
        if (!patrolArea.OverlapPoint(end)) return false;
        if (SafeZone2D.IsEnemyMovementBlocked(end, safeZoneClearance)) return false;

        float distance = Vector2.Distance(start, end);
        int checks = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.05f, routeCheckSpacing)));

        for (int i = 0; i <= checks; i++)
        {
            Vector2 sample = Vector2.Lerp(start, end, i / (float)checks);
            if (SafeZone2D.IsEnemyMovementBlocked(sample, safeZoneClearance))
                return false;
        }

        return true;
    }

    private void OnValidate()
    {
        pointSearchAttempts = Mathf.Max(1, pointSearchAttempts);
        routeCheckSpacing = Mathf.Max(0.05f, routeCheckSpacing);
        safeZoneClearance = Mathf.Max(0f, safeZoneClearance);
    }

    private void OnDrawGizmosSelected()
    {
        if (patrolArea == null) return;
        Gizmos.color = new Color(0.2f, 0.65f, 1f, 0.8f);
        Gizmos.DrawWireCube(patrolArea.bounds.center, patrolArea.bounds.size);
    }
}
