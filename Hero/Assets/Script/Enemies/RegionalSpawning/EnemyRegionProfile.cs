using UnityEngine;

[CreateAssetMenu(
    fileName = "Enemy Region Profile",
    menuName = "Hero2D/Enemies/Enemy Region Profile")]
public sealed class EnemyRegionProfile : ScriptableObject
{
    [Header("Region Identity")]
    [SerializeField] private string regionName = "New Enemy Region";
    [TextArea]
    [SerializeField] private string description;

    [Header("Recommended Player Level")]
    [Min(1)] [SerializeField] private int recommendedMinLevel = 1;
    [Min(1)] [SerializeField] private int recommendedMaxLevel = 5;

    [Header("Editor")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.55f, 0.1f, 0.65f);

    public string RegionName => string.IsNullOrWhiteSpace(regionName)
        ? name
        : regionName;
    public string Description => description;
    public int RecommendedMinLevel => recommendedMinLevel;
    public int RecommendedMaxLevel => recommendedMaxLevel;
    public Color GizmoColor => gizmoColor;

    private void OnValidate()
    {
        recommendedMinLevel = Mathf.Max(1, recommendedMinLevel);
        recommendedMaxLevel = Mathf.Max(recommendedMinLevel, recommendedMaxLevel);
    }
}
