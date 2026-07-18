using UnityEngine;

/// <summary>
/// Lightweight world-space boss warning. BossManager creates this automatically
/// when no custom warning prefab is assigned.
/// </summary>
[DisallowMultipleComponent]
public sealed class BossSpawnWarning2D : MonoBehaviour
{
    private const int CircleSegments = 48;

    private LineRenderer ring;
    private LineRenderer exclamationStem;
    private LineRenderer exclamationDot;
    private Material runtimeMaterial;
    private Color baseColor = new Color(1f, 0.25f, 0.08f, 0.9f);
    private float radius = 1.25f;
    private float progress;

    public static BossSpawnWarning2D Create(Vector3 position, float radius, Color color)
    {
        GameObject warningObject = new GameObject("BossSpawnWarning");
        warningObject.transform.position = position;

        BossSpawnWarning2D warning = warningObject.AddComponent<BossSpawnWarning2D>();
        warning.Configure(radius, color);
        return warning;
    }

    private void Awake()
    {
        BuildVisuals();
    }

    public void Configure(float newRadius, Color newColor)
    {
        radius = Mathf.Max(0.1f, newRadius);
        baseColor = newColor;
        BuildRingPositions();
        SetProgress(0f);
    }

    public void SetProgress(float normalizedProgress)
    {
        progress = Mathf.Clamp01(normalizedProgress);
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 10f) * 0.08f;
        transform.localScale = Vector3.one * pulse;

        Color urgentColor = Color.Lerp(baseColor, Color.red, progress);
        urgentColor.a = Mathf.Lerp(0.65f, 1f, progress);
        ApplyColor(ring, urgentColor);
        ApplyColor(exclamationStem, urgentColor);
        ApplyColor(exclamationDot, urgentColor);
    }

    private void Update()
    {
        // Continue pulsing even when gameplay is paused by a menu.
        SetProgress(progress);
    }

    private void BuildVisuals()
    {
        if (ring != null) return;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader != null) runtimeMaterial = new Material(shader);

        ring = CreateLine("WarningRing", true, 0.08f, CircleSegments);
        exclamationStem = CreateLine("WarningExclamationStem", false, 0.12f, 2);
        exclamationDot = CreateLine("WarningExclamationDot", true, 0.12f, 12);

        BuildRingPositions();
        BuildExclamationPositions();
    }

    private LineRenderer CreateLine(string objectName, bool loop, float width, int pointCount)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);

        LineRenderer line = child.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = loop;
        line.positionCount = pointCount;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.sortingOrder = 500;
        if (runtimeMaterial != null) line.sharedMaterial = runtimeMaterial;
        return line;
    }

    private void BuildRingPositions()
    {
        if (ring == null) return;

        for (int i = 0; i < CircleSegments; i++)
        {
            float angle = i / (float)CircleSegments * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f));
        }

        BuildExclamationPositions();
    }

    private void BuildExclamationPositions()
    {
        if (exclamationStem == null || exclamationDot == null) return;

        float iconHeight = radius * 0.75f;
        exclamationStem.SetPosition(0, new Vector3(0f, -iconHeight * 0.05f, 0f));
        exclamationStem.SetPosition(1, new Vector3(0f, iconHeight, 0f));

        float dotRadius = Mathf.Max(0.06f, radius * 0.09f);
        for (int i = 0; i < exclamationDot.positionCount; i++)
        {
            float angle = i / (float)exclamationDot.positionCount * Mathf.PI * 2f;
            exclamationDot.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * dotRadius,
                -iconHeight * 0.35f + Mathf.Sin(angle) * dotRadius,
                0f));
        }
    }

    private static void ApplyColor(LineRenderer line, Color color)
    {
        if (line == null) return;
        line.startColor = color;
        line.endColor = color;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}
