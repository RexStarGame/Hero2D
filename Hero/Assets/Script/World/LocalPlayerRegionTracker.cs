using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LocalPlayerRegionTracker : MonoBehaviour
{
    [Header("Local HUD")]
    [Tooltip("Assign the local player's region HUD. If empty, the active RegionAnnouncementUI is used.")]
    [SerializeField] private RegionAnnouncementUI announcementUI;

    private readonly Dictionary<RegionZone2D, int> overlappingRegions = new Dictionary<RegionZone2D, int>();
    private RegionZone2D activeRegion;

    public string CurrentRegionId => activeRegion == null ? string.Empty : activeRegion.RegionId;

    private void OnEnable()
    {
        RefreshActiveRegion();
    }

    private void OnDisable()
    {
        overlappingRegions.Clear();
        activeRegion = null;
    }

    public void EnterRegion(RegionZone2D region)
    {
        if (region == null)
            return;

        overlappingRegions.TryGetValue(region, out int colliderCount);
        overlappingRegions[region] = colliderCount + 1;
        RefreshActiveRegion();
    }

    public void ExitRegion(RegionZone2D region)
    {
        if (region == null || !overlappingRegions.TryGetValue(region, out int colliderCount))
            return;

        if (colliderCount <= 1)
            overlappingRegions.Remove(region);
        else
            overlappingRegions[region] = colliderCount - 1;

        RefreshActiveRegion();
    }

    private void RefreshActiveRegion()
    {
        RegionZone2D bestRegion = null;

        foreach (KeyValuePair<RegionZone2D, int> entry in overlappingRegions)
        {
            RegionZone2D candidate = entry.Key;
            if (candidate == null || entry.Value <= 0)
                continue;

            if (bestRegion == null || candidate.Priority > bestRegion.Priority)
                bestRegion = candidate;
        }

        if (bestRegion == activeRegion)
            return;

        activeRegion = bestRegion;

        if (activeRegion == null)
            return;

        RegionAnnouncementUI targetUI = announcementUI != null
            ? announcementUI
            : RegionAnnouncementUI.Instance;

        if (targetUI == null)
        {
            Debug.LogWarning("No RegionAnnouncementUI is available for the local player.", this);
            return;
        }

        targetUI.Show(activeRegion.DisplayName);
    }
}
