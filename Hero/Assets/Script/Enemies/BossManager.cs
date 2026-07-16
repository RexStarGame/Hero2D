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

    private int lastBossIndexSpawned = -1;
    private bool bossTagUsable = true;
    private bool warnedMissingEnemyManager;
    private bool warnedMissingPlayerXP;

    private void Awake()
    {
        if (enemyManager == null)
            enemyManager = FindAnyObjectByType<EnemyManager>();

        if (playerXP == null)
            playerXP = FindAnyObjectByType<PlayerXP>();

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

        Vector2 p2 = enemyManager.GetRandomPointInZone();
        Vector3 spawnPos = new Vector3(p2.x, p2.y, spawnZ);

        GameObject spawnedBoss = Instantiate(entry.prefab, spawnPos, Quaternion.identity);
        RegisterCheckpointReward(spawnedBoss, entry.minLevel);
        lastBossIndexSpawned = nextBossIndex;
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
