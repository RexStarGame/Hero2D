using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class RegionZone2D : MonoBehaviour
{
    [Header("Region")]
    [Tooltip("A stable unique ID for this region. It can later be reused by quests, saves or networking.")]
    [SerializeField] private string regionId = "new-region";
    [Tooltip("The name shown to the player when this region becomes active.")]
    [SerializeField] private string displayName = "New Region";
    [Tooltip("Higher-priority regions win while trigger areas overlap.")]
    [SerializeField] private int priority;

    public string RegionId => regionId;
    public string DisplayName => displayName;
    public int Priority => priority;

    private void Reset()
    {
        Collider2D zoneCollider = GetComponent<Collider2D>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        LocalPlayerRegionTracker tracker = other.GetComponentInParent<LocalPlayerRegionTracker>();
        if (tracker != null && tracker.isActiveAndEnabled)
            tracker.EnterRegion(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        LocalPlayerRegionTracker tracker = other.GetComponentInParent<LocalPlayerRegionTracker>();
        if (tracker != null)
            tracker.ExitRegion(this);
    }

    private void OnValidate()
    {
        Collider2D zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider != null)
            zoneCollider.isTrigger = true;

        regionId = regionId == null ? string.Empty : regionId.Trim();
        displayName = displayName == null ? string.Empty : displayName.Trim();
    }
}
