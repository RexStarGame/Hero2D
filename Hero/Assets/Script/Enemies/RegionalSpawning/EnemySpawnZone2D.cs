using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Regional RPG spawner. Every entry owns its prefab, physical spawn area,
/// population, timer, ground rules and environment-blocking rules.
/// Put this object beneath a WorldChunk Simulation root.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemySpawnZone2D : MonoBehaviour
{
    [Serializable]
    public sealed class EnemyEntry
    {
        [Header("Enemy")]
        public string displayName = "Enemy";
        public GameObject enemyPrefab;

        [Tooltip(
            "Assign a trigger BoxCollider2D, CircleCollider2D or PolygonCollider2D " +
            "from an empty child GameObject. Only this enemy uses this area.")]
        public Collider2D spawnArea;

        [Header("Population on Easy")]
        [Tooltip(
            "Living population desired in this area on Easy. Harder difficulties " +
            "scale it using the director's global limits.")]
        [Min(0)] public int desiredPopulation = 3;

        [Header("Independent Respawn Timer")]
        [Min(0f)] public float initialSpawnDelay = 0.5f;
        [Min(0.05f)] public float minimumRespawnTime = 8f;
        [Min(0.05f)] public float maximumRespawnTime = 14f;
        [Min(0.05f)] public float failedPointRetryTime = 1.5f;

        [Header("Distance and Visibility")]
        [Min(0f)] public float minimumDistanceFromPlayers = 7f;
        [Min(0.1f)] public float maximumDistanceFromPlayers = 28f;
        public bool requireOffscreen = true;
        [Min(0f)] public float offscreenPadding = 1.25f;

        [Header("Allowed Ground")]
        [Tooltip(
            "Enable this when grass/path/walkable ground has Collider2D components " +
            "on dedicated layers.")]
        public bool requireAllowedGround;
        public LayerMask allowedGroundLayers;
        public bool allowTriggerGround = true;

        [Header("Never Spawn Here")]
        [Tooltip(
            "Include Walls, OtherStuff, trees, rocks and other solid environment layers.")]
        public LayerMask blockedEnvironmentLayers;
        [Tooltip(
            "Optional additional tag rejection, for example Walls and OtherStuff. " +
            "Layers are faster and should be preferred.")]
        public string[] blockedTags;
        [Min(0f)] public float enemyClearanceRadius = 0.5f;
        public bool blockedTriggersCount;

        [Header("Search")]
        [Min(1)] public int spawnPointSearchAttempts = 32;

        [NonSerialized] internal int livingCount;
        [NonSerialized] internal float nextSpawnTime;
        [NonSerialized] internal bool runtimeInitialized;
    }

    [Header("Region")]
    [SerializeField] private EnemyRegionProfile regionProfile;
    [Tooltip("Optional. Auto-finds the active RegionalSpawnDirector if empty.")]
    [SerializeField] private RegionalSpawnDirector director;

    [Header("Spawn Ownership")]
    [Tooltip(
        "Assign a DynamicEnemies child beneath the same WorldChunk Simulation root. " +
        "If empty, enemies become children of this zone.")]
    [SerializeField] private Transform dynamicEnemiesRoot;

    [Tooltip(
        "Used for the local player's offscreen check. Auto-finds Camera.main if empty. " +
        "Distance checks still protect every registered co-op streaming target.")]
    [SerializeField] private Camera localObserverCamera;

    [Header("Enemy Entries")]
    [SerializeField] private List<EnemyEntry> enemies = new List<EnemyEntry>();

    [Header("Performance")]
    [Tooltip("The zone does not need to evaluate every frame.")]
    [Min(0.05f)] [SerializeField] private float evaluationInterval = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool drawSpawnAreas = true;
    [SerializeField] private bool logConfigurationProblems = true;

    private float nextEvaluationTime;

    public EnemyRegionProfile RegionProfile => regionProfile;
    public IReadOnlyList<EnemyEntry> Enemies => enemies;

    private void Awake()
    {
        AutoFindReferences();
        InitializeEntries();
    }

    private void OnEnable()
    {
        AutoFindReferences();
        InitializeEntries();
        nextEvaluationTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextEvaluationTime) return;
        nextEvaluationTime = Time.unscaledTime + evaluationInterval;

        if (director == null || !director.CanSpawn()) return;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (!director.CanSpawn()) break;
            TrySpawnEntry(i, enemies[i]);
        }
    }

    private void TrySpawnEntry(int index, EnemyEntry entry)
    {
        if (entry == null || entry.enemyPrefab == null || entry.spawnArea == null)
            return;

        int desired = director.ScaleLocalPopulation(entry.desiredPopulation);
        if (entry.livingCount >= desired) return;

        // A difficulty increase can raise the desired local population while
        // the entry was previously full and therefore had no pending timer.
        if (float.IsPositiveInfinity(entry.nextSpawnTime))
            entry.nextSpawnTime = Time.time + GetRespawnDelay(entry);

        if (Time.time < entry.nextSpawnTime) return;

        if (!TryFindSpawnPoint(entry, out Vector2 spawnPoint))
        {
            entry.nextSpawnTime = Time.time + entry.failedPointRetryTime;
            return;
        }

        Transform parent = dynamicEnemiesRoot != null ? dynamicEnemiesRoot : transform;
        GameObject spawned = Instantiate(
            entry.enemyPrefab,
            spawnPoint,
            Quaternion.identity,
            parent);

        SpawnedEnemyRegionLink link =
            spawned.GetComponent<SpawnedEnemyRegionLink>();
        if (link == null) link = spawned.AddComponent<SpawnedEnemyRegionLink>();

        entry.livingCount++;
        link.Initialize(this, index, director, entry.spawnArea);
        entry.nextSpawnTime = entry.livingCount >= desired
            ? float.PositiveInfinity
            : Time.time + GetRespawnDelay(entry);
    }

    private bool TryFindSpawnPoint(EnemyEntry entry, out Vector2 spawnPoint)
    {
        spawnPoint = entry.spawnArea.bounds.center;
        int attempts = Mathf.Max(1, entry.spawnPointSearchAttempts);
        Bounds bounds = entry.spawnArea.bounds;

        for (int i = 0; i < attempts; i++)
        {
            Vector2 candidate = new Vector2(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y));

            if (!entry.spawnArea.OverlapPoint(candidate)) continue;
            if (!IsCorrectDistanceFromPlayers(entry, candidate)) continue;

            if (entry.requireOffscreen &&
                localObserverCamera != null &&
                EnemyManager.IsWorldPointVisible(
                    localObserverCamera,
                    candidate,
                    entry.offscreenPadding + entry.enemyClearanceRadius))
                continue;

            if (SafeZone2D.IsEnemyMovementBlocked(
                    candidate,
                    entry.enemyClearanceRadius))
                continue;

            if (!HasAllowedGround(entry, candidate)) continue;
            if (TouchesBlockedEnvironment(entry, candidate)) continue;

            spawnPoint = candidate;
            return true;
        }

        return false;
    }

    private bool IsCorrectDistanceFromPlayers(EnemyEntry entry, Vector2 candidate)
    {
        IReadOnlyList<WorldStreamingTarget> targets =
            WorldStreamingTarget.ActiveTargets;

        bool foundSimulationTarget = false;
        bool closeEnoughToOne = false;
        float minimumSqr = entry.minimumDistanceFromPlayers *
                           entry.minimumDistanceFromPlayers;
        float maximumSqr = entry.maximumDistanceFromPlayers *
                           entry.maximumDistanceFromPlayers;

        for (int i = 0; i < targets.Count; i++)
        {
            WorldStreamingTarget target = targets[i];
            if (target == null ||
                !target.isActiveAndEnabled ||
                !target.AffectsSimulation)
                continue;

            foundSimulationTarget = true;
            float sqrDistance = (target.Position - candidate).sqrMagnitude;

            // Protect every co-op player from enemies appearing beside them.
            if (sqrDistance < minimumSqr) return false;
            if (sqrDistance <= maximumSqr) closeEnoughToOne = true;
        }

        // No active target means no player needs this region simulated.
        return foundSimulationTarget && closeEnoughToOne;
    }

    private bool HasAllowedGround(EnemyEntry entry, Vector2 candidate)
    {
        if (!entry.requireAllowedGround) return true;
        if (entry.allowedGroundLayers.value == 0) return false;

        Collider2D[] ground = Physics2D.OverlapPointAll(
            candidate,
            entry.allowedGroundLayers);

        for (int i = 0; i < ground.Length; i++)
        {
            Collider2D hit = ground[i];
            if (hit == null || hit == entry.spawnArea) continue;
            if (!entry.allowTriggerGround && hit.isTrigger) continue;
            return true;
        }

        return false;
    }

    private bool TouchesBlockedEnvironment(EnemyEntry entry, Vector2 candidate)
    {
        float radius = Mathf.Max(0.01f, entry.enemyClearanceRadius);

        if (entry.blockedEnvironmentLayers.value != 0)
        {
            Collider2D[] blocked = Physics2D.OverlapCircleAll(
                candidate,
                radius,
                entry.blockedEnvironmentLayers);

            for (int i = 0; i < blocked.Length; i++)
            {
                Collider2D hit = blocked[i];
                if (hit == null || hit == entry.spawnArea) continue;
                if (!entry.blockedTriggersCount && hit.isTrigger) continue;
                return true;
            }
        }

        if (entry.blockedTags == null || entry.blockedTags.Length == 0)
            return false;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(candidate, radius);
        for (int i = 0; i < nearby.Length; i++)
        {
            Collider2D hit = nearby[i];
            if (hit == null || hit == entry.spawnArea) continue;
            if (!entry.blockedTriggersCount && hit.isTrigger) continue;

            string hitTag = hit.gameObject.tag;
            for (int tagIndex = 0; tagIndex < entry.blockedTags.Length; tagIndex++)
            {
                string blockedTag = entry.blockedTags[tagIndex];
                if (!string.IsNullOrWhiteSpace(blockedTag) && hitTag == blockedTag)
                    return true;
            }
        }

        return false;
    }

    internal void NotifyEnemyReleased(int entryIndex)
    {
        if (entryIndex < 0 || entryIndex >= enemies.Count) return;

        EnemyEntry entry = enemies[entryIndex];
        if (entry == null) return;

        entry.livingCount = Mathf.Max(0, entry.livingCount - 1);
        entry.nextSpawnTime = Time.time + GetRespawnDelay(entry);
    }

    private void InitializeEntries()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyEntry entry = enemies[i];
            if (entry == null || entry.runtimeInitialized) continue;

            entry.runtimeInitialized = true;
            entry.livingCount = 0;
            entry.nextSpawnTime = Time.time + Mathf.Max(0f, entry.initialSpawnDelay);

            if (logConfigurationProblems)
                ValidateEntryConfiguration(i, entry);
        }
    }

    private void ValidateEntryConfiguration(int index, EnemyEntry entry)
    {
        string prefix = $"[EnemySpawnZone2D] Entry {index} ({entry.displayName})";

        if (entry.enemyPrefab == null)
            Debug.LogWarning($"{prefix} has no Enemy Prefab.", this);
        if (entry.spawnArea == null)
            Debug.LogWarning($"{prefix} has no Spawn Area collider.", this);
        else if (!entry.spawnArea.isTrigger)
            Debug.LogWarning(
                $"{prefix} Spawn Area should be a trigger so it does not block movement.",
                entry.spawnArea);
        if (entry.requireAllowedGround && entry.allowedGroundLayers.value == 0)
            Debug.LogWarning(
                $"{prefix} requires allowed ground but Allowed Ground Layers is empty.",
                this);
    }

    private float GetRespawnDelay(EnemyEntry entry)
    {
        return UnityEngine.Random.Range(
            Mathf.Max(0.05f, entry.minimumRespawnTime),
            Mathf.Max(entry.minimumRespawnTime, entry.maximumRespawnTime));
    }

    private void AutoFindReferences()
    {
        if (director == null) director = RegionalSpawnDirector.Instance;
        if (director == null)
            director = FindAnyObjectByType<RegionalSpawnDirector>();
        if (localObserverCamera == null) localObserverCamera = Camera.main;
    }

    private void OnValidate()
    {
        evaluationInterval = Mathf.Max(0.05f, evaluationInterval);

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyEntry entry = enemies[i];
            if (entry == null) continue;

            entry.desiredPopulation = Mathf.Max(0, entry.desiredPopulation);
            entry.initialSpawnDelay = Mathf.Max(0f, entry.initialSpawnDelay);
            entry.minimumRespawnTime = Mathf.Max(0.05f, entry.minimumRespawnTime);
            entry.maximumRespawnTime = Mathf.Max(
                entry.minimumRespawnTime,
                entry.maximumRespawnTime);
            entry.failedPointRetryTime = Mathf.Max(0.05f, entry.failedPointRetryTime);
            entry.minimumDistanceFromPlayers = Mathf.Max(
                0f,
                entry.minimumDistanceFromPlayers);
            entry.maximumDistanceFromPlayers = Mathf.Max(
                entry.minimumDistanceFromPlayers + 0.1f,
                entry.maximumDistanceFromPlayers);
            entry.offscreenPadding = Mathf.Max(0f, entry.offscreenPadding);
            entry.enemyClearanceRadius = Mathf.Max(0f, entry.enemyClearanceRadius);
            entry.spawnPointSearchAttempts = Mathf.Max(
                1,
                entry.spawnPointSearchAttempts);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawSpawnAreas || enemies == null) return;

        Color color = regionProfile != null
            ? regionProfile.GizmoColor
            : new Color(1f, 0.55f, 0.1f, 0.65f);
        Gizmos.color = color;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyEntry entry = enemies[i];
            if (entry?.spawnArea == null) continue;
            Bounds bounds = entry.spawnArea.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    private static T FindAnyObjectByType<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindAnyObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }
}
