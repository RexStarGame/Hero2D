using UnityEngine;

/// <summary>
/// Connects one dynamically spawned enemy to its exact region entry.
/// OnDisable intentionally does not remove it: streaming sleep is not death.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpawnedEnemyRegionLink : MonoBehaviour
{
    private EnemySpawnZone2D owner;
    private RegionalSpawnDirector director;
    private EnemyHealth enemyHealth;
    private int entryIndex = -1;
    private bool initialized;
    private bool released;
    private bool subscribed;

    internal void Initialize(
        EnemySpawnZone2D spawnOwner,
        int spawnEntryIndex,
        RegionalSpawnDirector spawnDirector)
    {
        Release();

        owner = spawnOwner;
        entryIndex = spawnEntryIndex;
        director = spawnDirector;
        initialized = true;
        released = false;

        enemyHealth = GetComponentInChildren<EnemyHealth>(true);
        Subscribe();
        director?.Register(this);
    }

    private void OnEnable()
    {
        if (initialized && !released) Subscribe();
    }

    private void OnDisable()
    {
        // A streamed chunk disables its whole Simulation root. Keep this enemy
        // registered and only remove the event subscription while sleeping.
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Release();
    }

    private void HandleDeath()
    {
        Release();
    }

    private void Subscribe()
    {
        if (subscribed || enemyHealth == null) return;
        enemyHealth.Died += HandleDeath;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || enemyHealth == null) return;
        enemyHealth.Died -= HandleDeath;
        subscribed = false;
    }

    private void Release()
    {
        if (!initialized || released) return;

        released = true;
        Unsubscribe();
        director?.Unregister(this);
        owner?.NotifyEnemyReleased(entryIndex);
    }
}
