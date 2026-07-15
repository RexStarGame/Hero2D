using System;
using UnityEngine;

/// <summary>
/// Shared target memory for normal enemies and bosses.
/// Uses a smaller detection radius and a larger give-up radius (hysteresis),
/// supports multiple players, and immediately rejects players in safe zones.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyAggro2D : MonoBehaviour
{
    private static PlayerHealth[] cachedPlayers = Array.Empty<PlayerHealth>();
    private static float nextPlayerCacheRefresh;

    /// <summary>
    /// Future networking adapters should return true only on the server/host
    /// responsible for this enemy. Single-player permits all enemies.
    /// </summary>
    public static Func<GameObject, bool> HasMovementAuthority { get; set; } = _ => true;

    [Header("Awareness")]
    [Min(0.1f)] [SerializeField] private float detectionRange = 6f;
    [Min(0.1f)] [SerializeField] private float giveUpRange = 10f;
    [Min(0.05f)] [SerializeField] private float scanInterval = 0.15f;

    private Transform currentTarget;
    private float nextScanTime;

    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;
    public float DetectionRange => detectionRange;
    public float GiveUpRange => giveUpRange;

    private void Update()
    {
        if (currentTarget != null && ShouldReleaseCurrentTarget())
            currentTarget = null;

        if (currentTarget == null && Time.unscaledTime >= nextScanTime)
        {
            nextScanTime = Time.unscaledTime + scanInterval;
            currentTarget = FindNearestEligiblePlayer();
        }
    }

    private void OnValidate()
    {
        detectionRange = Mathf.Max(0.1f, detectionRange);
        giveUpRange = Mathf.Max(detectionRange, giveUpRange);
        scanInterval = Mathf.Max(0.05f, scanInterval);
    }

    public void ConfigureRanges(float detectDistance, float disengageDistance)
    {
        detectionRange = Mathf.Max(0.1f, detectDistance);
        giveUpRange = Mathf.Max(detectionRange, disengageDistance);
    }

    public void ClearTarget()
    {
        currentTarget = null;
        nextScanTime = Time.unscaledTime + scanInterval;
    }

    public bool HasAuthority()
    {
        return HasMovementAuthority == null || HasMovementAuthority(gameObject);
    }

    private bool ShouldReleaseCurrentTarget()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            return true;

        if (SafeZone2D.IsPlayerProtected(currentTarget.position))
            return true;

        return ((Vector2)currentTarget.position - (Vector2)transform.position).sqrMagnitude
               > giveUpRange * giveUpRange;
    }

    private Transform FindNearestEligiblePlayer()
    {
        RefreshPlayerCacheIfNeeded();

        Transform best = null;
        float bestSqrDistance = detectionRange * detectionRange;
        Vector2 origin = transform.position;

        for (int i = 0; i < cachedPlayers.Length; i++)
        {
            PlayerHealth player = cachedPlayers[i];
            if (player == null || !player.isActiveAndEnabled) continue;

            Vector2 position = player.transform.position;
            if (SafeZone2D.IsPlayerProtected(position)) continue;

            float sqrDistance = (position - origin).sqrMagnitude;
            if (sqrDistance > bestSqrDistance) continue;

            bestSqrDistance = sqrDistance;
            best = player.transform;
        }

        return best;
    }

    private static void RefreshPlayerCacheIfNeeded()
    {
        if (Time.unscaledTime < nextPlayerCacheRefresh && cachedPlayers != null)
            return;

        nextPlayerCacheRefresh = Time.unscaledTime + 0.5f;
#if UNITY_2023_1_OR_NEWER
        cachedPlayers = UnityEngine.Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
#else
        cachedPlayers = UnityEngine.Object.FindObjectsOfType<PlayerHealth>();
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.15f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.25f, 0.15f, 0.55f);
        Gizmos.DrawWireSphere(transform.position, giveUpRange);
    }
}
