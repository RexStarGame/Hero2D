using System;
using UnityEngine;

/// <summary>
/// A stable section of a premade world. The GameObject holding this component
/// stays active; only the assigned child roots are streamed.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldChunk : MonoBehaviour
{
    [Header("Stable Identity")]
    [Tooltip("Persistent ID used by future save/co-op state. Do not change it after shipping a map.")]
    [SerializeField] private string chunkId;

    [Header("Chunk Bounds")]
    [Tooltip("Optional collider used only as the chunk's bounds. It does not need to be a trigger.")]
    [SerializeField] private Collider2D boundsSource;
    [SerializeField] private Vector2 localCenter;
    [SerializeField] private Vector2 size = new Vector2(40f, 40f);

    [Header("Streamed Child Roots")]
    [Tooltip("Terrain/graphics/colliders needed only by a nearby local camera/player.")]
    [SerializeField] private GameObject[] presentationRoots = Array.Empty<GameObject>();

    [Tooltip("Enemies, AI, spawners and other authoritative gameplay objects.")]
    [SerializeField] private GameObject[] simulationRoots = Array.Empty<GameObject>();

    [Tooltip("Objects that must be active when either presentation or simulation is required.")]
    [SerializeField] private GameObject[] sharedRoots = Array.Empty<GameObject>();

    private bool presentationActive = true;
    private bool simulationActive = true;

    public string ChunkId => chunkId;
    public bool PresentationActive => presentationActive;
    public bool SimulationActive => simulationActive;

    public Bounds WorldBounds
    {
        get
        {
            if (boundsSource != null)
                return boundsSource.bounds;

            Vector3 center = transform.TransformPoint(localCenter);
            Vector3 scaledSize = Vector3.Scale(new Vector3(size.x, size.y, 0f), Abs(transform.lossyScale));
            return new Bounds(center, scaledSize);
        }
    }

    private void Reset()
    {
        EnsureStableId();
        boundsSource = GetComponent<Collider2D>();
    }

    private void OnValidate()
    {
        EnsureStableId();
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(0.1f, size.y);
    }

    public void SetStreamingState(bool presentation, bool simulation)
    {
        presentationActive = presentation;
        simulationActive = simulation;

        SetRootsActive(presentationRoots, presentation);
        SetRootsActive(simulationRoots, simulation);
        SetRootsActive(sharedRoots, presentation || simulation);
    }

    public float SqrDistanceTo(Vector2 position)
    {
        Bounds bounds = WorldBounds;
        float x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
        float y = Mathf.Clamp(position.y, bounds.min.y, bounds.max.y);
        return (position - new Vector2(x, y)).sqrMagnitude;
    }

    private void EnsureStableId()
    {
        if (string.IsNullOrWhiteSpace(chunkId))
            chunkId = Guid.NewGuid().ToString("N");
    }

    private void SetRootsActive(GameObject[] roots, bool active)
    {
        if (roots == null) return;

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null || root == gameObject) continue;
            if (root.activeSelf != active) root.SetActive(active);
        }
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private void OnDrawGizmosSelected()
    {
        Bounds bounds = WorldBounds;
        Gizmos.color = new Color(0.15f, 0.8f, 1f, 0.65f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
