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
    [Tooltip("Det maksimale antal fjender der må være på én gang.")]
    [SerializeField] private int maxSpawn = 10;

    [Tooltip("Hvis antallet af fjender kommer under dette tal, begynder vi at spawne igen.")]
    [SerializeField] private int minSpawn = 3;

    private float timer;
    private bool isSpawningActive = true;

    void Start()
    {
        timer = spawnInterval;
    }

    void Update()
    {
        // Husk at dine fjender skal have tagget "Enemy"
        int currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        bool wasActive = isSpawningActive;

        if (currentEnemyCount >= maxSpawn)
        {
            isSpawningActive = false;
        }
        else if (currentEnemyCount <= minSpawn)
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
        if (currentEnemyCount >= maxSpawn)
            return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnEnemy();
            timer = spawnInterval;
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

        Vector2 spawnPosition = enemyManager.GetRandomPointInZone();
        Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity); 
    }

    GameObject GetSpawnPrefab()
    {
        // Hvis der ikke er en pool, brug fallback
        if (enemyPool == null || enemyPool.Length == 0)
            return enemyPrefab;

        int currentLevel = (playerXP != null) ? playerXP.level : 1;

        // Saml alle eligible fjender (minLevel <= currentLevel)
        List<GameObject> eligible = new List<GameObject>(enemyPool.Length);
        for (int i = 0; i < enemyPool.Length; i++)
        {
            var entry = enemyPool[i];
            if (entry != null && entry.prefab != null && entry.minLevel <= currentLevel)
                eligible.Add(entry.prefab);
        }

        if (eligible.Count == 0)
            return enemyPrefab;

        return eligible[Random.Range(0, eligible.Count)];
    }
}
