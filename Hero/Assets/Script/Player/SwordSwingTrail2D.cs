using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable sword-swing arc drawn at the outer reach of an AttackHitBox.
/// The effect is visual only and never changes combat collision or damage.
/// </summary>
[DisallowMultipleComponent]
public sealed class SwordSwingTrail2D : MonoBehaviour
{
    [Header("Shape")]
    [Range(90f, 220f)] [SerializeField] private float arcAngle = 180f;
    [Range(8, 32)] [SerializeField] private int smoothness = 20;
    [Min(0f)] [SerializeField] private float radiusOffset = 0.03f;
    [Range(0.15f, 1f)] [SerializeField] private float visibleTrailFraction = 0.55f;
    [SerializeField] private bool reverseSwing;

    [Header("Timing")]
    [Min(0.05f)] [SerializeField] private float swingDuration = 0.16f;
    [Range(0.1f, 0.9f)] [SerializeField] private float fadeStart = 0.65f;

    [Header("Appearance")]
    [SerializeField] private Color trailColor = new Color(1f, 0.88f, 0.45f, 0.95f);
    [Min(0.005f)] [SerializeField] private float trailWidth = 0.08f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 20;

    private Collider2D hitbox;
    private PlayerAttack owner;
    private LineRenderer line;
    private Material runtimeMaterial;
    private Coroutine trailRoutine;
    private Vector2 arcCenterLocal;
    private float arcRadius;
    private readonly Vector3[] positions = new Vector3[32];

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        owner = GetComponentInParent<PlayerAttack>();
        EnsureLineRenderer();
        RecalculateFromHitbox();
        HideImmediately();
    }

    private void OnValidate()
    {
        arcAngle = Mathf.Clamp(arcAngle, 90f, 220f);
        smoothness = Mathf.Clamp(smoothness, 8, 32);
        radiusOffset = Mathf.Max(0f, radiusOffset);
        swingDuration = Mathf.Max(0.05f, swingDuration);
        trailWidth = Mathf.Max(0.005f, trailWidth);
    }

    public void PlaySwing()
    {
        EnsureLineRenderer();
        RecalculateFromHitbox();

        if (trailRoutine != null)
            StopCoroutine(trailRoutine);

        trailRoutine = StartCoroutine(AnimateTrail());
    }

    public void StopTrail()
    {
        if (trailRoutine != null)
        {
            StopCoroutine(trailRoutine);
            trailRoutine = null;
        }

        HideImmediately();
    }

    private IEnumerator AnimateTrail()
    {
        line.enabled = true;
        float elapsed = 0f;

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / swingDuration);
            float head = reverseSwing ? 1f - progress : progress;
            float direction = reverseSwing ? 1f : -1f;
            float tail = Mathf.Clamp01(head + direction * visibleTrailFraction);

            DrawTrail(tail, head, progress);
            yield return null;
        }

        HideImmediately();
        trailRoutine = null;
    }

    private void DrawTrail(float tail, float head, float progress)
    {
        int count = Mathf.Clamp(smoothness, 8, positions.Length);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            float arcProgress = Mathf.Lerp(tail, head, t);
            positions[i] = GetArcPoint(arcProgress);
        }

        float fade = progress <= fadeStart
            ? 1f
            : 1f - Mathf.InverseLerp(fadeStart, 1f, progress);

        Color transparent = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        Color bright = new Color(trailColor.r, trailColor.g, trailColor.b, trailColor.a * fade);

        line.positionCount = count;
        for (int i = 0; i < count; i++)
            line.SetPosition(i, positions[i]);
        line.startColor = transparent;
        line.endColor = bright;
        line.startWidth = trailWidth * 0.25f;
        line.endWidth = trailWidth;
    }

    private Vector3 GetArcPoint(float progress)
    {
        float halfArc = arcAngle * 0.5f;
        float degrees = Mathf.Lerp(-halfArc, halfArc, progress);
        float radians = degrees * Mathf.Deg2Rad;
        return arcCenterLocal + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * arcRadius;
    }

    private void RecalculateFromHitbox()
    {
        if (hitbox == null) hitbox = GetComponent<Collider2D>();
        if (owner == null) owner = GetComponentInParent<PlayerAttack>();

        arcCenterLocal = owner != null
            ? (Vector2)transform.InverseTransformPoint(owner.transform.position)
            : Vector2.zero;

        float forwardEdge = CalculateForwardEdge();
        arcRadius = Mathf.Max(0.1f, forwardEdge - arcCenterLocal.x + radiusOffset);
    }

    private float CalculateForwardEdge()
    {
        if (hitbox is PolygonCollider2D polygon && polygon.points.Length > 0)
        {
            float edge = float.NegativeInfinity;
            Vector2[] points = polygon.points;
            for (int i = 0; i < points.Length; i++)
                edge = Mathf.Max(edge, points[i].x);
            return polygon.offset.x + edge;
        }

        if (hitbox is BoxCollider2D box)
            return box.offset.x + box.size.x * 0.5f;

        if (hitbox is CircleCollider2D circle)
            return circle.offset.x + circle.radius;

        if (hitbox is CapsuleCollider2D capsule)
            return capsule.offset.x + capsule.size.x * 0.5f;

        return 0.7f;
    }

    private void EnsureLineRenderer()
    {
        if (line == null) line = GetComponent<LineRenderer>();
        if (line == null) line = gameObject.AddComponent<LineRenderer>();

        line.useWorldSpace = false;
        line.loop = false;
        line.numCapVertices = 3;
        line.numCornerVertices = 2;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.TransformZ;
        line.sortingLayerName = sortingLayerName;
        line.sortingOrder = sortingOrder;

        if (line.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                runtimeMaterial = new Material(shader) { name = "Sword Swing Trail (Runtime)" };
                line.sharedMaterial = runtimeMaterial;
            }
        }
    }

    private void HideImmediately()
    {
        if (line == null) return;
        line.positionCount = 0;
        line.enabled = false;
    }

    private void OnDisable()
    {
        StopTrail();
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
