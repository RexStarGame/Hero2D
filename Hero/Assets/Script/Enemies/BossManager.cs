using System.Collections;
using UnityEngine;

public class BossManager : MonoBehaviour
{
    [System.Serializable]
    public class BossEntry
    {
        [Tooltip("Boss prefab der kan spawnes.")]
        public GameObject prefab;

        [Tooltip("Bossen spawner når spilleren er nået til (eller over) dette level.")]
        public int minLevel = 1;
    }

    [Header("Referencer")]
    [Tooltip("Objektet med EnemyManager (skal have GetRandomPointInZone()).")]
    public EnemyManager enemyManager;

    [Tooltip("Objektet med PlayerXP (skal have 'level').")]
    public PlayerXP playerXP;

    [Header("Boss List")]
    [Tooltip("Bosses spawnes kun én gang pr. entry (i rækkefølge).")]
    public BossEntry[] bossPool;

    [Header("Indstillinger")]
    [Tooltip("Tag der bruges til at finde aktive bosses i scenen (kræver at tag findes i Tag Manager).")]
    [SerializeField] private string bossTag = "Boss";

    [Tooltip("Hvis true: der spawner kun en boss, hvis der ikke allerede er en aktiv boss i scenen.")]
    [SerializeField] private bool requireNoActiveBoss = true;

    [Tooltip("Z-position ved spawn (2D spil = typisk 0).")]
    [SerializeField] private float spawnZ = 0f;

    [Header("Visible Boss Spawn")]
    [Tooltip("Camera that must see the warning and boss spawn. Auto-finds Camera.main if empty.")]
    [SerializeField] private Camera spawnCamera;

    [Tooltip("Player used to keep the boss from spawning directly on top of them. Auto-finds PlayerMovement if empty.")]
    [SerializeField] private Transform spawnTarget;

    [Tooltip("Seconds the warning remains visible before the boss appears.")]
    [Min(0.1f)] [SerializeField] private float warningDuration = 3f;

    [Tooltip("Optional custom world-space warning prefab. If empty, a pulsing ring and exclamation mark are created automatically.")]
    [SerializeField] private GameObject warningPrefab;

    [Tooltip("Radius of the automatic warning ring and approximate boss clearance.")]
    [Min(0.1f)] [SerializeField] private float warningRadius = 1.25f;

    [Tooltip("Warning colour used by the automatic marker.")]
    [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.08f, 0.9f);

    [Tooltip("Normalized distance from the screen edges. 0.12 keeps the boss inside the middle 76% of the camera.")]
    [Range(0f, 0.45f)] [SerializeField] private float viewportPadding = 0.12f;

    [Tooltip("Minimum world distance between the boss spawn and the player.")]
    [Min(0f)] [SerializeField] private float minimumDistanceFromPlayer = 3f;

    [Tooltip("Solid layers a boss may not overlap when spawning. Triggers are ignored.")]
    [SerializeField] private LayerMask spawnBlockingLayers = ~0;

    [Tooltip("Number of visible spawn positions tested before postponing the boss spawn.")]
    [Min(1)] [SerializeField] private int spawnPointSearchAttempts = 32;

    [Tooltip("Delay before trying again when no safe visible boss position exists.")]
    [Min(0.1f)] [SerializeField] private float failedSpawnRetryDelay = 1f;

    private int lastBossIndexSpawned = -1;
    private bool bossTagUsable = true;
    private bool warnedMissingEnemyManager;
    private bool warnedMissingPlayerXP;
    private Coroutine pendingBossSpawn;
    private GameObject activeWarning;
    private float nextSpawnAttemptTime;

    private void Awake()
    {
        if (enemyManager == null)
            enemyManager = FindAnyObjectByType<EnemyManager>();

        if (playerXP == null)
            playerXP = FindAnyObjectByType<PlayerXP>();

        AutoFindSpawnReferences();

        RestoreDefeatedBossProgress();

        // Valider tag én gang (FindGameObjectsWithTag crasher hvis tag ikke findes)
        if (requireNoActiveBoss && !string.IsNullOrWhiteSpace(bossTag))
        {
            try
            {
                GameObject.FindGameObjectsWithTag(bossTag);
                bossTagUsable = true;
            }
            catch (UnityException)
            {
                bossTagUsable = false;
                Debug.LogWarning($"BossManager: Tag '{bossTag}' findes ikke. Slår bossTag-check fra.");
            }
        }
    }

    private void Update()
    {
        TrySpawnBoss();
    }

    private void TrySpawnBoss()
    {
        if (pendingBossSpawn != null)
            return;

        if (Time.unscaledTime < nextSpawnAttemptTime)
            return;

        if (bossPool == null || bossPool.Length == 0)
            return;

        if (enemyManager == null)
        {
            if (!warnedMissingEnemyManager)
            {
                Debug.LogError("BossManager: Mangler reference til EnemyManager (GetRandomPointInZone()).");
                warnedMissingEnemyManager = true;
            }
            return;
        }

        if (playerXP == null)
        {
            if (!warnedMissingPlayerXP)
            {
                Debug.LogWarning("BossManager: Mangler reference til PlayerXP. Antager level 1.");
                warnedMissingPlayerXP = true;
            }
        }

        int currentLevel = (playerXP != null) ? playerXP.level : 1;

        int nextBossIndex = lastBossIndexSpawned + 1;
        if (nextBossIndex >= bossPool.Length)
            return;

        BossEntry entry = bossPool[nextBossIndex];
        if (entry == null || entry.prefab == null)
            return;

        // Spawn når spilleren er nået til (eller over) kravet
        if (currentLevel < entry.minLevel)
            return;

        if (requireNoActiveBoss && bossTagUsable && !string.IsNullOrWhiteSpace(bossTag))
        {
            if (GameObject.FindGameObjectsWithTag(bossTag).Length > 0)
                return;
        }

        AutoFindSpawnReferences();
        if (spawnCamera == null || spawnTarget == null)
        {
            Debug.LogWarning("BossManager: A camera and spawn target are required for visible boss spawning.", this);
            return;
        }

        pendingBossSpawn = StartCoroutine(SpawnBossWithWarning(nextBossIndex, entry));
    }

    private IEnumerator SpawnBossWithWarning(int bossIndex, BossEntry entry)
    {
        if (!TryFindVisibleBossPoint(out Vector2 spawnPoint))
        {
            nextSpawnAttemptTime = Time.unscaledTime + failedSpawnRetryDelay;
            pendingBossSpawn = null;
            yield break;
        }

        activeWarning = CreateWarning(spawnPoint);
        BossSpawnWarning2D generatedWarning = activeWarning != null
            ? activeWarning.GetComponent<BossSpawnWarning2D>()
            : null;

        float remaining = warningDuration;
        while (remaining > 0f)
        {
            if (entry == null || entry.prefab == null || HasActiveBoss())
            {
                CleanupWarning();
                pendingBossSpawn = null;
                yield break;
            }

            bool pointStillValid = EnemyManager.IsWorldPointVisible(spawnCamera, spawnPoint) &&
                                   enemyManager.IsSpawnPointClear(
                                       spawnPoint, warningRadius, spawnBlockingLayers);

            if (!pointStillValid)
            {
                // The player/camera moved or the location became blocked. Move
                // the warning and restart it so the boss never appears without
                // a full, visible warning at the final location.
                if (!TryFindVisibleBossPoint(out spawnPoint))
                {
                    nextSpawnAttemptTime = Time.unscaledTime + failedSpawnRetryDelay;
                    CleanupWarning();
                    pendingBossSpawn = null;
                    yield break;
                }

                if (activeWarning != null)
                    activeWarning.transform.position = new Vector3(spawnPoint.x, spawnPoint.y, spawnZ);

                remaining = warningDuration;
            }

            float progress = 1f - remaining / Mathf.Max(0.1f, warningDuration);
            if (generatedWarning != null) generatedWarning.SetProgress(progress);

            remaining -= Time.deltaTime;
            yield return null;
        }

        // One final validation closes the one-frame gap between the last loop
        // and Instantiate.
        if (!EnemyManager.IsWorldPointVisible(spawnCamera, spawnPoint) ||
            !enemyManager.IsSpawnPointClear(spawnPoint, warningRadius, spawnBlockingLayers))
        {
            nextSpawnAttemptTime = Time.unscaledTime + failedSpawnRetryDelay;
            CleanupWarning();
            pendingBossSpawn = null;
            yield break;
        }

        Vector3 spawnPosition = new Vector3(spawnPoint.x, spawnPoint.y, spawnZ);
        GameObject spawnedBoss = Instantiate(entry.prefab, spawnPosition, Quaternion.identity);
        RegisterCheckpointReward(spawnedBoss, entry.minLevel);
        lastBossIndexSpawned = bossIndex;

        CleanupWarning();
        pendingBossSpawn = null;
    }

    private bool TryFindVisibleBossPoint(out Vector2 spawnPoint)
    {
        return enemyManager.TryGetVisibleSpawnPoint(
            spawnCamera,
            spawnTarget.position,
            minimumDistanceFromPlayer,
            viewportPadding,
            warningRadius,
            spawnBlockingLayers,
            spawnPointSearchAttempts,
            out spawnPoint);
    }

    private GameObject CreateWarning(Vector2 spawnPoint)
    {
        Vector3 position = new Vector3(spawnPoint.x, spawnPoint.y, spawnZ);
        if (warningPrefab != null)
            return Instantiate(warningPrefab, position, Quaternion.identity);

        BossSpawnWarning2D warning = BossSpawnWarning2D.Create(
            position, warningRadius, warningColor);
        return warning != null ? warning.gameObject : null;
    }

    private bool HasActiveBoss()
    {
        if (!requireNoActiveBoss || !bossTagUsable || string.IsNullOrWhiteSpace(bossTag))
            return false;

        return GameObject.FindGameObjectsWithTag(bossTag).Length > 0;
    }

    private void AutoFindSpawnReferences()
    {
        if (spawnCamera == null) spawnCamera = Camera.main;
        if (spawnTarget != null) return;

        PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
        if (movement != null)
            spawnTarget = movement.transform;
        else if (playerXP != null)
            spawnTarget = playerXP.transform;
    }

    private void CleanupWarning()
    {
        if (activeWarning != null) Destroy(activeWarning);
        activeWarning = null;
    }

    private void OnDisable()
    {
        if (pendingBossSpawn != null) StopCoroutine(pendingBossSpawn);
        pendingBossSpawn = null;
        CleanupWarning();
    }

    private void OnValidate()
    {
        warningDuration = Mathf.Max(0.1f, warningDuration);
        warningRadius = Mathf.Max(0.1f, warningRadius);
        viewportPadding = Mathf.Clamp(viewportPadding, 0f, 0.45f);
        minimumDistanceFromPlayer = Mathf.Max(0f, minimumDistanceFromPlayer);
        spawnPointSearchAttempts = Mathf.Max(1, spawnPointSearchAttempts);
        failedSpawnRetryDelay = Mathf.Max(0.1f, failedSpawnRetryDelay);
    }

    private void RestoreDefeatedBossProgress()
    {
        int checkpointLevel = BossLevelCheckpoint.Level;
        lastBossIndexSpawned = -1;

        if (bossPool == null)
            return;

        // A saved checkpoint proves that its milestone boss was defeated in an
        // earlier run. Skip it so it does not immediately respawn.
        for (int i = 0; i < bossPool.Length; i++)
        {
            BossEntry entry = bossPool[i];
            if (entry == null || entry.minLevel > checkpointLevel)
                break;

            lastBossIndexSpawned = i;
        }
    }

    private static void RegisterCheckpointReward(GameObject spawnedBoss, int checkpointLevel)
    {
        if (spawnedBoss == null)
            return;

        BossHealth bossHealth = spawnedBoss.GetComponentInChildren<BossHealth>(true);
        if (bossHealth == null)
        {
            Debug.LogWarning(
                $"BossManager: '{spawnedBoss.name}' has no BossHealth, so it cannot unlock a level checkpoint.",
                spawnedBoss);
            return;
        }

        // This listener is invoked only by BossHealth's guarded death path.
        bossHealth.onDeath.AddListener(() => BossLevelCheckpoint.TryUnlock(checkpointLevel));
    }

    // Hvis du starter et nyt run/spil og vil tillade bosses igen:
    public void ResetBossProgress()
    {
        lastBossIndexSpawned = -1;
    }
}
