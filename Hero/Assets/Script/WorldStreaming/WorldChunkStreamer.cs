using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Distance-based streaming for a premade 2D world.
/// Supports multiple targets and separate presentation/simulation channels,
/// which keeps the foundation compatible with future co-op.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldChunkStreamer : MonoBehaviour
{
    [Header("Chunks")]
    [Tooltip("Automatically finds WorldChunk components in loaded scenes.")]
    [SerializeField] private bool autoDiscoverChunks = true;
    [SerializeField] private List<WorldChunk> chunks = new List<WorldChunk>();

    [Header("Targets")]
    [Tooltip("WorldStreamingTarget components register automatically. These are optional extra targets.")]
    [SerializeField] private List<WorldStreamingTarget> manualTargets = new List<WorldStreamingTarget>();

    [Header("Presentation Distance")]
    [Min(0f)] [SerializeField] private float presentationLoadDistance = 55f;
    [Min(0f)] [SerializeField] private float presentationUnloadDistance = 70f;

    [Header("Simulation Distance")]
    [Min(0f)] [SerializeField] private float simulationLoadDistance = 45f;
    [Min(0f)] [SerializeField] private float simulationUnloadDistance = 60f;

    [Header("Performance")]
    [Tooltip("Streaming does not need to run every frame.")]
    [Min(0.05f)] [SerializeField] private float refreshInterval = 0.25f;
    [SerializeField] private bool logDuplicateChunkIds = true;

    private readonly List<WorldStreamingTarget> targetBuffer = new List<WorldStreamingTarget>();
    private float nextRefreshTime;

    private void Awake()
    {
        NormalizeSettings();
        RefreshChunks();
        ValidateChunkIds();
        RefreshNow();
    }

    private void OnEnable()
    {
        WorldStreamingTarget.TargetsChanged += HandleTargetsChanged;
    }

    private void OnDisable()
    {
        WorldStreamingTarget.TargetsChanged -= HandleTargetsChanged;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime) return;
        RefreshNow();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    public void RefreshChunks()
    {
        RemoveMissingChunks();
        if (!autoDiscoverChunks) return;

#if UNITY_2023_1_OR_NEWER
        WorldChunk[] found = FindObjectsByType<WorldChunk>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        WorldChunk[] found = FindObjectsOfType<WorldChunk>(true);
#endif
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && !chunks.Contains(found[i]))
                chunks.Add(found[i]);
        }
    }

    public void RefreshNow()
    {
        nextRefreshTime = Time.unscaledTime + refreshInterval;
        BuildTargetBuffer();

        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            WorldChunk chunk = chunks[i];
            if (chunk == null)
            {
                chunks.RemoveAt(i);
                continue;
            }

            bool presentation = ShouldBeActive(
                chunk,
                true,
                chunk.PresentationActive,
                presentationLoadDistance,
                presentationUnloadDistance);

            bool simulation = ShouldBeActive(
                chunk,
                false,
                chunk.SimulationActive,
                simulationLoadDistance,
                simulationUnloadDistance);

            chunk.SetStreamingState(presentation, simulation);
        }
    }

    private bool ShouldBeActive(
        WorldChunk chunk,
        bool presentationChannel,
        bool currentlyActive,
        float loadDistance,
        float unloadDistance)
    {
        float threshold = currentlyActive ? unloadDistance : loadDistance;
        float sqrThreshold = threshold * threshold;

        for (int i = 0; i < targetBuffer.Count; i++)
        {
            WorldStreamingTarget target = targetBuffer[i];
            if (target == null || !target.isActiveAndEnabled) continue;

            bool affectsChannel = presentationChannel
                ? target.AffectsPresentation
                : target.AffectsSimulation;

            if (affectsChannel && chunk.SqrDistanceTo(target.Position) <= sqrThreshold)
                return true;
        }

        return false;
    }

    private void BuildTargetBuffer()
    {
        targetBuffer.Clear();

        IReadOnlyList<WorldStreamingTarget> registered = WorldStreamingTarget.ActiveTargets;
        for (int i = 0; i < registered.Count; i++)
            AddTargetIfValid(registered[i]);

        for (int i = 0; i < manualTargets.Count; i++)
            AddTargetIfValid(manualTargets[i]);
    }

    private void AddTargetIfValid(WorldStreamingTarget target)
    {
        if (target != null && target.isActiveAndEnabled && !targetBuffer.Contains(target))
            targetBuffer.Add(target);
    }

    private void HandleTargetsChanged()
    {
        RefreshNow();
    }

    private void RemoveMissingChunks()
    {
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            if (chunks[i] == null) chunks.RemoveAt(i);
        }
    }

    private void ValidateChunkIds()
    {
        if (!logDuplicateChunkIds) return;

        HashSet<string> ids = new HashSet<string>();
        for (int i = 0; i < chunks.Count; i++)
        {
            WorldChunk chunk = chunks[i];
            if (chunk == null || string.IsNullOrWhiteSpace(chunk.ChunkId)) continue;

            if (!ids.Add(chunk.ChunkId))
            {
                Debug.LogError(
                    $"[WorldChunkStreamer] Duplicate chunk ID '{chunk.ChunkId}' on '{chunk.name}'. " +
                    "Give duplicated chunks a new ID before relying on saves or co-op state.",
                    chunk);
            }
        }
    }

    private void NormalizeSettings()
    {
        refreshInterval = Mathf.Max(0.05f, refreshInterval);
        presentationLoadDistance = Mathf.Max(0f, presentationLoadDistance);
        presentationUnloadDistance = Mathf.Max(presentationLoadDistance, presentationUnloadDistance);
        simulationLoadDistance = Mathf.Max(0f, simulationLoadDistance);
        simulationUnloadDistance = Mathf.Max(simulationLoadDistance, simulationUnloadDistance);
    }
}
