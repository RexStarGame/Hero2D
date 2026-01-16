using UnityEngine;

public class TelegraphPulse2D : MonoBehaviour
{
    [Header("References (optional - auto finds)")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private LineRenderer line;

    [Header("Force Visible Colors")]
    [SerializeField] private bool forceUnlitMaterial = true;

    [Header("Colors")]
    [SerializeField] private Color soonColor = new Color(1f, 0.9f, 0.2f, 0.75f);
    [SerializeField] private Color imminentColor = new Color(1f, 0.2f, 0.2f, 0.95f);

    [Header("Timing")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float imminentStartFraction = 0.50f;
    [SerializeField] private float blinkHz = 12f;

    [Header("Auto Cleanup")]
    [Tooltip("If true, the telegraph GameObject destroys itself after the countdown finishes.")]
    [SerializeField] private bool autoDestroyAfterCountdown = true;

    [Tooltip("Extra time after countdown (lets impact happen before removing visuals).")]
    [SerializeField] private float destroyBufferSeconds = 0.15f;

    [Tooltip("Failsafe: always destroy after this many seconds even if Init is never called.")]
    [SerializeField] private float failsafeDestroySeconds = 8f;

    private float duration;
    private float startTime;
    private bool inited;

    private Material runtimeMat;
    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>(true);
        if (line == null) line = GetComponentInChildren<LineRenderer>(true);

        if (forceUnlitMaterial)
            ForceUnlit();

        // If someone spawns this and forgets to Init(), it will still self-clean.
        if (failsafeDestroySeconds > 0f)
            Invoke(nameof(DestroySelf), failsafeDestroySeconds);
    }

    public void Init(float timeToHit)
    {
        duration = Mathf.Max(0.05f, timeToHit);
        startTime = Time.time;
        inited = true;

        ApplyColor(soonColor);

        // Replace the failsafe with the real lifetime once we know duration
        CancelInvoke(nameof(DestroySelf));
        if (autoDestroyAfterCountdown)
            Invoke(nameof(DestroySelf), duration + Mathf.Max(0f, destroyBufferSeconds));
        else if (failsafeDestroySeconds > 0f)
            Invoke(nameof(DestroySelf), failsafeDestroySeconds);
    }

    private void Update()
    {
        if (!inited) return;

        float elapsed = Time.time - startTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float imminentStartT = 1f - imminentStartFraction;

        if (t < imminentStartT)
        {
            ApplyColor(soonColor);
            return;
        }

        float s = (Mathf.Sin(Time.time * Mathf.PI * 2f * blinkHz) + 1f) * 0.5f;
        ApplyColor(Color.Lerp(soonColor, imminentColor, s));
    }

    private void ApplyColor(Color c)
    {
        if (sprite != null)
            sprite.color = c;

        if (line != null)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) }
            );
            line.colorGradient = g;

            var mat = line.material;
            if (mat != null)
            {
                if (mat.HasProperty(ColorProp)) mat.SetColor(ColorProp, c);
                else if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            }
        }
    }

    private void ForceUnlit()
    {
        Shader s =
            Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ??
            Shader.Find("Sprites/Default") ??
            Shader.Find("Unlit/Color");

        if (s == null) return;

        // Use a runtime material instance so it doesn't modify the shared asset
        runtimeMat = new Material(s);

        if (sprite != null) sprite.material = runtimeMat;
        if (line != null) line.material = runtimeMat;
    }

    private void DestroySelf()
    {
        // Safe even if already destroyed by another script
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Prevent material leaks (runtime materials must be destroyed)
        if (runtimeMat != null)
        {
            Destroy(runtimeMat);
            runtimeMat = null;
        }
    }
}
