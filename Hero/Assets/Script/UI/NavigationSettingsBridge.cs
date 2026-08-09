using UnityEngine;
using UnityEngine.UI;

public sealed class NavigationSettingsBridge : MonoBehaviour
{
    [Header("Optional Toggle References")]
    [Tooltip("Optional. Assign this to keep the toggle synchronized with the saved map-waypoint setting.")]
    [SerializeField] private Toggle waypointOnMapToggle;

    [Tooltip("Optional. Assign this to keep the toggle synchronized with the saved HUD-arrow setting.")]
    [SerializeField] private Toggle hudDirectionArrowToggle;

    private void OnEnable()
    {
        RefreshToggleValues();
    }

    public void SetWaypointOnMap(bool isEnabled)
    {
        LiveMinimapHUD.SetShowWaypointOnMap(isEnabled);
    }

    public void SetHUDDirectionArrow(bool isEnabled)
    {
        LiveMinimapHUD.SetShowHudDirectionArrow(isEnabled);
    }

    public void RefreshToggleValues()
    {
        if (waypointOnMapToggle != null)
            waypointOnMapToggle.SetIsOnWithoutNotify(LiveMinimapHUD.ShowWaypointOnMap);

        if (hudDirectionArrowToggle != null)
            hudDirectionArrowToggle.SetIsOnWithoutNotify(LiveMinimapHUD.ShowHudDirectionArrow);
    }
}
