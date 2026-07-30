using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the global regional-enemy budget. Sleeping enemies remain registered,
/// so disabling a streamed chunk never causes replacement enemies elsewhere.
/// </summary>
[DisallowMultipleComponent]
public sealed class RegionalSpawnDirector : MonoBehaviour
{
    public static RegionalSpawnDirector Instance { get; private set; }

    [Header("Single-player / Future Network Authority")]
    [Tooltip(
        "Leave empty for single-player. In a future online game, assign a " +
        "MonoBehaviour implementing IRegionalSpawnAuthority on the host/server.")]
    [SerializeField] private MonoBehaviour authorityProvider;

    [Header("Global Living Enemy Limits")]
    [Tooltip("Easy keeps the current baseline.")]
    [Min(1)] [SerializeField] private int easyMaximum = 12;
    [Min(1)] [SerializeField] private int normalMaximum = 15;
    [Min(1)] [SerializeField] private int hardMaximum = 18;
    [Min(1)] [SerializeField] private int extremeMaximum = 20;
    [Min(1)] [SerializeField] private int nightmareMaximum = 22;

    [Header("Debug")]
    [SerializeField] private bool logRejectedAuthorityProvider = true;

    private readonly HashSet<SpawnedEnemyRegionLink> livingEnemies =
        new HashSet<SpawnedEnemyRegionLink>();
    private IRegionalSpawnAuthority authority;

    public int LivingEnemyCount
    {
        get
        {
            RemoveMissingLinks();
            return livingEnemies.Count;
        }
    }

    public int CurrentMaximum => GetMaximum(DifficultyManager.CurrentDifficulty);

    public bool HasSpawnAuthority =>
        authorityProvider == null ||
        (authority != null && authority.HasRegionalSpawnAuthority);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "[RegionalSpawnDirector] Only one active director may exist per scene.",
                this);
            enabled = false;
            return;
        }

        Instance = this;
        ResolveAuthority();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        easyMaximum = Mathf.Max(1, easyMaximum);
        normalMaximum = Mathf.Max(easyMaximum, normalMaximum);
        hardMaximum = Mathf.Max(normalMaximum, hardMaximum);
        extremeMaximum = Mathf.Max(hardMaximum, extremeMaximum);
        nightmareMaximum = Mathf.Max(extremeMaximum, nightmareMaximum);
        ResolveAuthority();
    }

    public bool CanSpawn()
    {
        return isActiveAndEnabled &&
               HasSpawnAuthority &&
               LivingEnemyCount < CurrentMaximum;
    }

    public int ScaleLocalPopulation(int easyPopulation)
    {
        easyPopulation = Mathf.Max(0, easyPopulation);
        if (easyPopulation == 0) return 0;

        float scale = CurrentMaximum / (float)Mathf.Max(1, easyMaximum);
        return Mathf.Max(1, Mathf.RoundToInt(easyPopulation * scale));
    }

    internal void Register(SpawnedEnemyRegionLink link)
    {
        if (link != null) livingEnemies.Add(link);
    }

    internal void Unregister(SpawnedEnemyRegionLink link)
    {
        if (link != null) livingEnemies.Remove(link);
    }

    private int GetMaximum(GameDifficulty difficulty)
    {
        switch (difficulty)
        {
            case GameDifficulty.Normal:
                return normalMaximum;
            case GameDifficulty.Hard:
                return hardMaximum;
            case GameDifficulty.Extreme:
                return extremeMaximum;
            case GameDifficulty.Nightmare:
                return nightmareMaximum;
            default:
                return easyMaximum;
        }
    }

    private void ResolveAuthority()
    {
        authority = authorityProvider as IRegionalSpawnAuthority;
        if (authorityProvider != null && authority == null && logRejectedAuthorityProvider)
        {
            Debug.LogWarning(
                "[RegionalSpawnDirector] Authority Provider must implement " +
                "IRegionalSpawnAuthority. Spawning is blocked until it is corrected.",
                this);
        }
    }

    private void RemoveMissingLinks()
    {
        livingEnemies.RemoveWhere(link => link == null);
    }
}
