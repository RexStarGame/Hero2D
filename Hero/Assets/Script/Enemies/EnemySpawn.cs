using System.Collections.Generic;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("Enemy prefab der kan spawnes.")]
        public GameObject prefab;

        [Tooltip("Minimum player level for at denne fjende må spawnes (fx 1, 5, 9, 13).")]
        public int minLevel = 1;
    }
    private readonly List<GameObject> eligible = new List<GameObject>(32);
    private int lastLevel = -1;

    [Header("Referencer")]
    [Tooltip("Fallback enemy prefab (bruges hvis Enemy List er tom eller ingen er eligible).")]
    public GameObject enemyPrefab;

    [Tooltip("Træk objektet med EnemyManager scriptet herind (skal have GetRandomPointInZone()).")]
    public EnemyManager enemyManager;

    [Tooltip("Træk PlayerXP herind for at låse op for nye fjender ud fra level.")]
    public PlayerXP playerXP;

    [Header("Enemy List")]
    [Tooltip("Alle fjender med minLevel <= player level kan spawnes (inkl. gamle fjender).")]
    public EnemySpawnEntry[] enemyPool;

    [Header("Spawn Tider")]
    [Tooltip("Tid i sekunder mellem hver spawn.")]
    [Min(0.01f)]
    public float spawnInterval = 3.0f;

    [Header("Spawn Limits")]
    [Tooltip("Easy baseline. Det maksimale antal fjender der må være på én gang.")]
    [SerializeField] private int maxSpawn = 12;

    [Tooltip("Easy baseline. Hvis antallet kommer under dette tal, begynder spawning igen.")]
    [SerializeField] private int minSpawn = 3;

    [Header("Difficulty Spawn Limits")]
    [Tooltip("Normal difficulty. Spawn interval and spawn distance are not changed.")]
    [SerializeField] private int normalMinSpawn = 4;
    [SerializeField] private int normalMaxSpawn = 15;

    [Tooltip("Hard difficulty. Spawn interval and spawn distance are not changed.")]
    [SerializeField] private int hardMinSpawn = 6;
    [SerializeField] private int hardMaxSpawn = 18;

    [Tooltip("Nightmare difficulty. Spawn interval and spawn distance are not changed.")]
    [SerializeField] private int nightmareMinSpawn = 8;
    [SerializeField] private int nightmareMaxSpawn = 22;

    [Header("Offscreen Spawn Area")]
    [Tooltip("Camera used to keep normal enemy spawning outside the visible screen. Auto-finds Camera.main if empty.")]
    [SerializeField] private Camera spawnCamera;

    [Tooltip("Player/target used as the centre of the spawn distance. Auto-finds PlayerMovement if empty.")]
    [SerializeField] private Transform spawnTarget;

    [Tooltip("Minimum distance from the player. Increase this if enemies feel too close when they enter the screen.")]
    [Min(0f)] [SerializeField] private float minimumSpawnDistance = 6f;

    [Tooltip("Maximum distance from the player. Normal enemies never intentionally spawn farther away than this.")]
    [Min(0.1f)] [SerializeField] private float maximumSpawnDistance = 12f;

    [Tooltip("Extra hidden distance outside the camera edge before an enemy may spawn.")]
    [Min(0f)] [SerializeField] private float offscreenPadding = 1.25f;

    [Tooltip("Approximate enemy body radius used to keep the whole sprite offscreen and away from obstacles.")]
    [Min(0f)] [SerializeField] private float spawnClearanceRadius = 0.5f;

    [Tooltip("Solid layers that normal enemies may not overlap when spawning. Triggers are ignored.")]
    [SerializeField] private LayerMask spawnBlockingLayers = ~0;

    [Tooltip("Number of candidate positions tested before this spawn is safely skipped.")]
    [Min(1)] [SerializeField] private int spawnPointSearchAttempts = 32;

    private float timer;
    private bool isSpawningActive = true;

    public int ActiveMinSpawn
    {
        get
        {
            GetActiveSpawnLimits(out int activeMin, out _);
            return activeMin;
        }
    }

    public int ActiveMaxSpawn
    {
        get
        {
            GetActiveSpawnLimits(out _, out int activeMax);
            return activeMax;
        }
    }

    void Start()
    {
        AutoFindSpawnReferences();
        timer = spawnInterval;
    }

    void Update()
    {
        GetActiveSpawnLimits(out int activeMinSpawn, out int activeMaxSpawn);

        // Husk at dine fjender skal have tagget "Enemy"
        int currentEnemyCount = EnemyCounter.Count;

        bool wasActive = isSpawningActive;

        if (currentEnemyCount >= activeMaxSpawn)
        {
            isSpawningActive = false;
        }
        else if (currentEnemyCount <= activeMinSpawn)
        {
            isSpawningActive = true;
        }

        // Hvis vi lige er startet igen (fra false -> true), så reset timer så det ikke spawner instant pga gammel timer
        if (!wasActive && isSpawningActive)
        {
            timer = spawnInterval;
        }

        if (!isSpawningActive)
            return;

        // Safety: hvis vi allerede er på max, spawn ikke (selv hvis timer rammer 0)
        if (currentEnemyCount >= activeMaxSpawn)
            return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnEnemy();
            timer = spawnInterval;
        }
    }

    private void GetActiveSpawnLimits(out int activeMin, out int activeMax)
    {
        switch (DifficultyManager.CurrentDifficulty)
        {
            case GameDifficulty.Normal:
                activeMin = normalMinSpawn;
                activeMax = normalMaxSpawn;
                break;
            case GameDifficulty.Hard:
                activeMin = hardMinSpawn;
                activeMax = hardMaxSpawn;
                break;
            case GameDifficulty.Nightmare:
                activeMin = nightmareMinSpawn;
                activeMax = nightmareMaxSpawn;
                break;
            default:
                activeMin = minSpawn;
                activeMax = maxSpawn;
                break;
        }
    }

    void SpawnEnemy()
    {

        if (enemyManager == null)
        {
            Debug.LogError("Mangler reference til EnemyManager!");
            return;
        }

        GameObject prefabToSpawn = GetSpawnPrefab();
        if (prefabToSpawn == null)
        {
            Debug.LogError("Ingen fjende prefab at spawne. Tjek EnemySpawn (enemyPrefab/enemyPool).");
            return;
        }

        AutoFindSpawnReferences();
        if (spawnCamera == null || spawnTarget == null)
        {
            Debug.LogWarning("EnemySpawn: A camera and spawn target are required for safe offscreen spawning.", this);
            return;
        }

        if (!enemyManager.TryGetOffscreenSpawnPoint(
                spawnCamera,
                spawnTarget.position,
                minimumSpawnDistance,
                maximumSpawnDistance,
                offscreenPadding,
                spawnClearanceRadius,
                spawnBlockingLayers,
                spawnPointSearchAttempts,
                out Vector2 spawnPosition))
        {
            // Skipping is intentional: an onscreen or invalid spawn would be
            // more noticeable than waiting for the next interval.
            return;
        }

        GameObject spawned = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        if (!spawned.TryGetComponent<EnemyCounter>(out _))
            spawned.AddComponent<EnemyCounter>();

    }

    private void AutoFindSpawnReferences()
    {
        if (spawnCamera == null) spawnCamera = Camera.main;

        if (spawnTarget == null)
        {
            PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
            if (movement != null)
                spawnTarget = movement.transform;
            else if (playerXP != null)
                spawnTarget = playerXP.transform;
        }
    }

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0.01f, spawnInterval);
        maxSpawn = Mathf.Max(1, maxSpawn);
        minSpawn = Mathf.Clamp(minSpawn, 0, maxSpawn);
        normalMaxSpawn = Mathf.Max(1, normalMaxSpawn);
        normalMinSpawn = Mathf.Clamp(normalMinSpawn, 0, normalMaxSpawn);
        hardMaxSpawn = Mathf.Max(1, hardMaxSpawn);
        hardMinSpawn = Mathf.Clamp(hardMinSpawn, 0, hardMaxSpawn);
        nightmareMaxSpawn = Mathf.Max(1, nightmareMaxSpawn);
        nightmareMinSpawn = Mathf.Clamp(nightmareMinSpawn, 0, nightmareMaxSpawn);
        minimumSpawnDistance = Mathf.Max(0f, minimumSpawnDistance);
        maximumSpawnDistance = Mathf.Max(minimumSpawnDistance + 0.1f, maximumSpawnDistance);
        offscreenPadding = Mathf.Max(0f, offscreenPadding);
        spawnClearanceRadius = Mathf.Max(0f, spawnClearanceRadius);
        spawnPointSearchAttempts = Mathf.Max(1, spawnPointSearchAttempts);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnTarget == null) return;

        Gizmos.color = new Color(1f, 0.75f, 0.15f, 0.8f);
        Gizmos.DrawWireSphere(spawnTarget.position, minimumSpawnDistance);

        Gizmos.color = new Color(0.15f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireSphere(spawnTarget.position, maximumSpawnDistance);
    }

    GameObject GetSpawnPrefab()
    {
        if (enemyPool == null || enemyPool.Length == 0)
            return enemyPrefab;

        int currentLevel = (playerXP != null) ? playerXP.level : 1;

        if (currentLevel != lastLevel)
        {
            lastLevel = currentLevel;
            eligible.Clear();

            for (int i = 0; i < enemyPool.Length; i++)
            {
                var entry = enemyPool[i];
                if (entry != null && entry.prefab != null && entry.minLevel <= currentLevel)
                    eligible.Add(entry.prefab);
            }
        }

        if (eligible.Count == 0)
            return enemyPrefab;

        return eligible[Random.Range(0, eligible.Count)];
    }
}
