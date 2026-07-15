using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a player (or another observer) as a target for world streaming.
///
/// Single-player: enable both presentation and simulation.
/// Future co-op client: the local player normally affects both; remote players
/// normally affect simulation only. A dedicated server can disable presentation.
/// This class intentionally has no dependency on a networking package.
/// </summary>
public sealed class WorldStreamingTarget : MonoBehaviour
{
    private static readonly List<WorldStreamingTarget> activeTargets = new List<WorldStreamingTarget>();

    public static IReadOnlyList<WorldStreamingTarget> ActiveTargets => activeTargets;
    public static event Action TargetsChanged;

    [Header("Streaming Channels")]
    [Tooltip("Keep nearby graphics, terrain and local colliders loaded for this target.")]
    [SerializeField] private bool affectsPresentation = true;

    [Tooltip("Keep nearby enemies, spawners and gameplay simulation active for this target.")]
    [SerializeField] private bool affectsSimulation = true;

    public bool AffectsPresentation => affectsPresentation;
    public bool AffectsSimulation => affectsSimulation;
    public Vector2 Position => transform.position;

    private void OnEnable()
    {
        if (!activeTargets.Contains(this))
        {
            activeTargets.Add(this);
            TargetsChanged?.Invoke();
        }
    }

    private void OnDisable()
    {
        if (activeTargets.Remove(this))
            TargetsChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (activeTargets.Remove(this))
            TargetsChanged?.Invoke();
    }

    /// <summary>
    /// Allows a future networking adapter to change a spawned player's role
    /// without this project depending on Mirror, Netcode, Photon, etc.
    /// </summary>
    public void ConfigureChannels(bool presentation, bool simulation)
    {
        affectsPresentation = presentation;
        affectsSimulation = simulation;
        TargetsChanged?.Invoke();
    }
}
