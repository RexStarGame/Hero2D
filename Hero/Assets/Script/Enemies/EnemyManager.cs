using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Patrol Area")]
    [SerializeField] private BoxCollider2D patrolArea;

    [Header("Safe-Zone Avoidance")]
    [Min(1)] [SerializeField] private int pointSearchAttempts = 32;
    [Min(0.05f)] [SerializeField] private float routeCheckSpacing = 0.25f;
    [Min(0f)] [SerializeField] private float safeZoneClearance = 0.35f;

    [Header("Spawn Validation")]
    [Tooltip("Extra attempts used when a spawner asks this manager for a valid point.")]
    [Min(1)] [SerializeField] private int defaultSpawnSearchAttempts = 32;

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

    /// <summary>
    /// Finds a point close to the target, but fully outside the supplied camera.
    /// Intended for normal enemies only. Bosses use TryGetVisibleSpawnPoint.
    /// </summary>
    public bool TryGetOffscreenSpawnPoint(
        Camera viewCamera,
        Vector2 targetPosition,
        float minimumDistance,
        float maximumDistance,
        float offscreenPadding,
        float clearanceRadius,
        LayerMask blockingLayers,
        int searchAttempts,
        out Vector2 spawnPoint)
    {
        spawnPoint = targetPosition;
        if (patrolArea == null || viewCamera == null) return false;

        minimumDistance = Mathf.Max(0f, minimumDistance);
        maximumDistance = Mathf.Max(minimumDistance + 0.01f, maximumDistance);
        offscreenPadding = Mathf.Max(0f, offscreenPadding);
        clearanceRadius = Mathf.Max(0f, clearanceRadius);
        int attempts = searchAttempts > 0 ? searchAttempts : defaultSpawnSearchAttempts;

        for (int i = 0; i < attempts; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt(Random.Range(
                minimumDistance * minimumDistance,
                maximumDistance * maximumDistance));

            Vector2 candidate = targetPosition + new Vector2(
                Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            // Include the enemy radius so a large sprite cannot be partly
            // visible on the exact frame where it is created.
            if (IsWorldPointVisible(viewCamera, candidate, offscreenPadding + clearanceRadius))
                continue;

            if (!IsSpawnPointClear(candidate, clearanceRadius, blockingLayers))
                continue;

            spawnPoint = candidate;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds a safe point inside the camera view. Intended for telegraphed bosses.
    /// viewportPadding is normalized: 0.1 keeps the point inside the middle 80%.
    /// </summary>
    public bool TryGetVisibleSpawnPoint(
        Camera viewCamera,
        Vector2 targetPosition,
        float minimumDistanceFromTarget,
        float viewportPadding,
        float clearanceRadius,
        LayerMask blockingLayers,
        int searchAttempts,
        out Vector2 spawnPoint)
    {
        spawnPoint = targetPosition;
        if (patrolArea == null || viewCamera == null) return false;

        minimumDistanceFromTarget = Mathf.Max(0f, minimumDistanceFromTarget);
        viewportPadding = Mathf.Clamp(viewportPadding, 0f, 0.45f);
        clearanceRadius = Mathf.Max(0f, clearanceRadius);
        int attempts = searchAttempts > 0 ? searchAttempts : defaultSpawnSearchAttempts;
        float worldPlaneDepth = Mathf.Abs(viewCamera.transform.position.z - patrolArea.transform.position.z);

        for (int i = 0; i < attempts; i++)
        {
            float viewportX = Random.Range(viewportPadding, 1f - viewportPadding);
            float viewportY = Random.Range(viewportPadding, 1f - viewportPadding);
            Vector3 world = viewCamera.ViewportToWorldPoint(
                new Vector3(viewportX, viewportY, worldPlaneDepth));
            Vector2 candidate = world;

            if (Vector2.Distance(candidate, targetPosition) < minimumDistanceFromTarget)
                continue;

            if (!IsInsideViewport(viewCamera, candidate, viewportPadding))
                continue;

            if (!IsSpawnPointClear(candidate, clearanceRadius, blockingLayers))
                continue;

            spawnPoint = candidate;
            return true;
        }

        return false;
    }

    public bool IsSpawnPointClear(
        Vector2 worldPosition,
        float clearanceRadius,
        LayerMask blockingLayers)
    {
        if (patrolArea == null || !patrolArea.OverlapPoint(worldPosition)) return false;

        clearanceRadius = Mathf.Max(0f, clearanceRadius);
        if (SafeZone2D.IsEnemyMovementBlocked(
                worldPosition, safeZoneClearance + clearanceRadius))
            return false;

        if (blockingLayers.value == 0) return true;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(
            worldPosition, Mathf.Max(0.01f, clearanceRadius), blockingLayers);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D hit = overlaps[i];
            if (hit == null || hit == patrolArea || hit.isTrigger) continue;
            return false;
        }

        return true;
    }

    public static bool IsWorldPointVisible(
        Camera viewCamera,
        Vector2 worldPosition,
        float worldPadding = 0f)
    {
        if (viewCamera == null) return false;

        Vector3 viewport = viewCamera.WorldToViewportPoint(worldPosition);
        if (viewport.z < 0f) return false;

        float horizontalPadding = 0f;
        float verticalPadding = 0f;
        if (viewCamera.orthographic && viewCamera.orthographicSize > 0.001f)
        {
            float visibleHeight = viewCamera.orthographicSize * 2f;
            float visibleWidth = visibleHeight * viewCamera.aspect;
            horizontalPadding = Mathf.Max(0f, worldPadding) / Mathf.Max(0.001f, visibleWidth);
            verticalPadding = Mathf.Max(0f, worldPadding) / visibleHeight;
        }

        return viewport.x >= -horizontalPadding && viewport.x <= 1f + horizontalPadding &&
               viewport.y >= -verticalPadding && viewport.y <= 1f + verticalPadding;
    }

    private static bool IsInsideViewport(
        Camera viewCamera,
        Vector2 worldPosition,
        float normalizedPadding)
    {
        if (viewCamera == null) return false;

        Vector3 viewport = viewCamera.WorldToViewportPoint(worldPosition);
        if (viewport.z < 0f) return false;

        normalizedPadding = Mathf.Clamp(normalizedPadding, 0f, 0.45f);
        return viewport.x >= normalizedPadding && viewport.x <= 1f - normalizedPadding &&
               viewport.y >= normalizedPadding && viewport.y <= 1f - normalizedPadding;
    }

    private void OnValidate()
    {
        pointSearchAttempts = Mathf.Max(1, pointSearchAttempts);
        routeCheckSpacing = Mathf.Max(0.05f, routeCheckSpacing);
        safeZoneClearance = Mathf.Max(0f, safeZoneClearance);
        defaultSpawnSearchAttempts = Mathf.Max(1, defaultSpawnSearchAttempts);
    }

    private void OnDrawGizmosSelected()
    {
        if (patrolArea == null) return;
        Gizmos.color = new Color(0.2f, 0.65f, 1f, 0.8f);
        Gizmos.DrawWireCube(patrolArea.bounds.center, patrolArea.bounds.size);
    }
}
