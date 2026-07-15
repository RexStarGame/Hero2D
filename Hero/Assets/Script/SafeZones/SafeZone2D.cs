using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy-free, combat-free 2D area.
/// Existing enemies are recognised through EnemyHealth or BossHealth.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class SafeZone2D : MonoBehaviour
{
    private static readonly List<SafeZone2D> activeZones = new List<SafeZone2D>();

    /// <summary>
    /// A future networking adapter can replace this with a server/host authority check.
    /// Single-player defaults to true for every enemy.
    /// </summary>
    public static Func<GameObject, bool> HasSimulationAuthority { get; set; } = _ => true;

    [Header("Stable Identity")]
    [Tooltip("Keep this unchanged after a map ships. Useful for future save/network state.")]
    [SerializeField] private string zoneId;

    [Header("Rules")]
    [SerializeField] private bool blockEnemies = true;
    [SerializeField] private bool protectPlayersFromDamage = true;
    [SerializeField] private bool blockPlayerAttacks = true;
    [SerializeField] private bool destroyEnemyProjectiles = true;

    [Header("Enemy Boundary")]
    [Tooltip("Small distance placed between an expelled enemy and the trigger edge.")]
    [Min(0f)] [SerializeField] private float exitPadding = 0.05f;

    [Tooltip("Optional layer filter in addition to EnemyHealth/BossHealth detection.")]
    [SerializeField] private LayerMask enemyLayers = ~0;

    [Header("Optional Feedback")]
    [SerializeField] private bool logBlockedPlayerAttacks;

    private Collider2D zoneCollider;
    private Rigidbody2D zoneBody;

    public string ZoneId => zoneId;
    public bool DestroysEnemyProjectiles => destroyEnemyProjectiles;

    private void Reset()
    {
        EnsureStableId();
        zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider != null) zoneCollider.isTrigger = true;
        ConfigureZoneBody();
    }

    private void Awake()
    {
        EnsureStableId();
        zoneCollider = GetComponent<Collider2D>();
        ConfigureZoneBody();

        if (zoneCollider != null && !zoneCollider.isTrigger)
        {
            Debug.LogWarning("[SafeZone2D] Safe-zone collider must be a trigger. It was corrected automatically.", this);
            zoneCollider.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        if (!activeZones.Contains(this)) activeZones.Add(this);
    }

    private void OnDisable()
    {
        activeZones.Remove(this);
    }

    private void OnDestroy()
    {
        activeZones.Remove(this);
    }

    private void OnValidate()
    {
        EnsureStableId();
        exitPadding = Mathf.Max(0f, exitPadding);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnforceEnemyBoundary(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        EnforceEnemyBoundary(other);
    }

    public bool Contains(Vector2 worldPosition)
    {
        return isActiveAndEnabled && zoneCollider != null && zoneCollider.OverlapPoint(worldPosition);
    }

    public static bool IsPlayerProtected(Vector2 worldPosition)
    {
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            SafeZone2D zone = activeZones[i];
            if (zone == null)
            {
                activeZones.RemoveAt(i);
                continue;
            }

            if (zone.protectPlayersFromDamage && zone.Contains(worldPosition))
                return true;
        }

        return false;
    }

    public static bool IsPlayerAttackBlocked(Vector2 attackerPosition, bool writeFeedback = true)
    {
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            SafeZone2D zone = activeZones[i];
            if (zone == null)
            {
                activeZones.RemoveAt(i);
                continue;
            }

            if (!zone.blockPlayerAttacks || !zone.Contains(attackerPosition)) continue;

            if (writeFeedback && zone.logBlockedPlayerAttacks)
                Debug.Log("[SafeZone2D] Combat is disabled while the player is inside the safe zone.", zone);

            return true;
        }

        return false;
    }

    public static bool IsEnemyProjectileBlocked(Vector2 projectilePosition)
    {
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            SafeZone2D zone = activeZones[i];
            if (zone == null)
            {
                activeZones.RemoveAt(i);
                continue;
            }

            if (zone.destroyEnemyProjectiles && zone.Contains(projectilePosition))
                return true;
        }

        return false;
    }

    private void EnforceEnemyBoundary(Collider2D other)
    {
        if (!blockEnemies || other == null || zoneCollider == null) return;

        GameObject enemyRoot = FindEnemyRoot(other);
        if (enemyRoot == null) return;
        if (((1 << enemyRoot.layer) & enemyLayers.value) == 0) return;
        if (HasSimulationAuthority != null && !HasSimulationAuthority(enemyRoot)) return;

        ColliderDistance2D separation = zoneCollider.Distance(other);
        if (!separation.isOverlapped) return;

        Vector2 outward = separation.normal;
        if (outward.sqrMagnitude < 0.0001f)
            outward = ((Vector2)enemyRoot.transform.position - (Vector2)zoneCollider.bounds.center).normalized;
        if (outward.sqrMagnitude < 0.0001f) outward = Vector2.up;

        outward.Normalize();
        Vector2 correction = outward * (-separation.distance + exitPadding);

        Rigidbody2D body = other.attachedRigidbody;
        if (body == null) body = enemyRoot.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.position += correction;

            float inwardSpeed = Vector2.Dot(body.linearVelocity, -outward);
            if (inwardSpeed > 0f)
                body.linearVelocity += outward * inwardSpeed;
        }
        else
        {
            enemyRoot.transform.position += (Vector3)correction;
        }

        Physics2D.SyncTransforms();
    }

    private static GameObject FindEnemyRoot(Collider2D other)
    {
        EnemyHealth normalEnemy = other.GetComponentInParent<EnemyHealth>();
        if (normalEnemy != null) return normalEnemy.gameObject;

        BossHealth boss = other.GetComponentInParent<BossHealth>();
        return boss != null ? boss.gameObject : null;
    }

    private void EnsureStableId()
    {
        if (string.IsNullOrWhiteSpace(zoneId))
            zoneId = Guid.NewGuid().ToString("N");
    }

    private void ConfigureZoneBody()
    {
        zoneBody = GetComponent<Rigidbody2D>();
        if (zoneBody == null) return;

        zoneBody.bodyType = RigidbodyType2D.Kinematic;
        zoneBody.gravityScale = 0f;
        zoneBody.constraints = RigidbodyConstraints2D.FreezeAll;
        zoneBody.simulated = true;
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D col = zoneCollider != null ? zoneCollider : GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0.15f, 0.9f, 0.55f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
