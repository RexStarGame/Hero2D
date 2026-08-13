using System.Collections.Generic;
using UnityEngine;

public enum MinimapTargetKind
{
    Player,
    Enemy
}

[DisallowMultipleComponent]
public sealed class MinimapTarget2D : MonoBehaviour
{
    private static readonly List<MinimapTarget2D> activeTargets = new List<MinimapTarget2D>(64);

    [SerializeField] private MinimapTargetKind kind;

    public static IReadOnlyList<MinimapTarget2D> ActiveTargets => activeTargets;
    public MinimapTargetKind Kind => kind;
    public Transform TrackedTransform => transform;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeTargets.Clear();
    }

    public static MinimapTarget2D Ensure(GameObject owner, MinimapTargetKind targetKind)
    {
        if (owner == null)
            return null;

        MinimapTarget2D target = owner.GetComponent<MinimapTarget2D>();
        if (target == null)
            target = owner.AddComponent<MinimapTarget2D>();

        target.kind = targetKind;
        return target;
    }

    private void OnEnable()
    {
        if (!activeTargets.Contains(this))
            activeTargets.Add(this);
    }

    private void OnDisable()
    {
        activeTargets.Remove(this);
    }

    private void OnDestroy()
    {
        activeTargets.Remove(this);
    }
}
