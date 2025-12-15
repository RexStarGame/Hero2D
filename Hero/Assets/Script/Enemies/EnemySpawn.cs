using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    [Header("Referencer")]
    [Tooltip("Træk din Enemy Prefab herind")]
    public GameObject enemyPrefab;

    [Tooltip("Træk objektet med EnemyManager scriptet herind")]
    public EnemyManager enemyManager;

    [Header("Spawn Tider")]
    [Tooltip("Tid i sekunder mellem hver spawn")]
    public float spawnInterval = 3.0f;

    [Header("Spawn Limits")]
    [Tooltip("Det maksimale antal fjender der må være på én gang")]
    [SerializeField] private int maxSpawn = 10;

    [Tooltip("Hvis antallet af fjender kommer under dette tal, begynder vi at spawne igen")]
    [SerializeField] private int minSpawn = 3;

    // Intern timer
    private float timer;

    // Styrer om vi er i gang med at "fylde op" med fjender
    private bool isSpawningActive = true;

    void Start()
    {
        timer = spawnInterval;
    }

    void Update()
    {
        // 1. Find ud af hvor mange fjender der er lige nu
        // (Husk at dine fjender skal have tagget "Enemy")
        int currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        // 2. Tjek vores Min/Max logik
        if (currentEnemyCount >= maxSpawn)
        {
            // Vi har nået loftet, stop med at spawne
            isSpawningActive = false;
        }
        else if (currentEnemyCount <= minSpawn)
        {
            // Vi er kommet under minimum, start med at spawne igen
            isSpawningActive = true;
        }

        // 3. Hvis vi må spawne, så kør timeren
        if (isSpawningActive)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                SpawnEnemy();
                timer = spawnInterval;
            }
        }
    }

    void SpawnEnemy()
    {
        if (enemyManager == null)
        {
            Debug.LogError("Mangler reference til EnemyManager!");
            return;
        }

        Vector2 spawnPosition = enemyManager.GetRandomPointInZone();
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}