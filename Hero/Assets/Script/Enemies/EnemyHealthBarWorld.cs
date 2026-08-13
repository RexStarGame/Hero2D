using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public sealed class EnemyHealthBarWorld : MonoBehaviour
{
    private const float BarWidth = 80f;
    private const float BarHeight = 8f;
    private const float BorderSize = 1.5f;
    private const float WorldPixelScale = 0.01f;

    [Header("World placement")]
    [Min(0f)] [SerializeField] private float heightAboveEnemy = 0.8f;
    [SerializeField] private int sortingOrder = 100;

    [Header("Appearance")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.02f, 0.02f, 0.9f);
    [SerializeField] private Color fillColor = new Color(0.2f, 0.9f, 0.25f, 1f);

    private EnemyHealth enemyHealth;
    private Canvas canvas;
    private RectTransform fillRect;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        BuildBar();
    }

    private void OnEnable()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        enemyHealth.HealthChanged += HandleHealthChanged;
        enemyHealth.Died += HandleDeath;
        HandleHealthChanged(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
    }

    private void OnDisable()
    {
        if (enemyHealth == null)
            return;

        enemyHealth.HealthChanged -= HandleHealthChanged;
        enemyHealth.Died -= HandleDeath;
    }

    private void BuildBar()
    {
        GameObject canvasObject = new GameObject("Enemy Health Bar", typeof(RectTransform), typeof(Canvas));
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.SetParent(transform, false);
        canvasRect.anchorMin = canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.sizeDelta = new Vector2(BarWidth, BarHeight);
        canvasRect.localPosition = transform.InverseTransformVector(Vector3.up * heightAboveEnemy);

        Vector3 parentScale = transform.lossyScale;
        canvasRect.localScale = new Vector3(
            SafeInverseScale(parentScale.x),
            SafeInverseScale(parentScale.y),
            SafeInverseScale(parentScale.z));

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        Image background = CreateImage("Background", canvasRect, backgroundColor);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = Vector2.zero;
        background.rectTransform.offsetMax = Vector2.zero;

        Image fill = CreateImage("Fill", canvasRect, fillColor);
        fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(BorderSize, BorderSize);
        fillRect.offsetMax = new Vector2(-BorderSize, -BorderSize);

        canvas.enabled = false;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void HandleHealthChanged(int current, int maximum)
    {
        if (canvas == null || fillRect == null)
            return;

        float normalizedHealth = maximum > 0
            ? Mathf.Clamp01((float)current / maximum)
            : 0f;

        // Changing the horizontal anchor avoids Slider and layout rebuild overhead.
        Vector2 anchorMax = fillRect.anchorMax;
        anchorMax.x = normalizedHealth;
        fillRect.anchorMax = anchorMax;
        canvas.enabled = current > 0 && normalizedHealth < 0.9999f;
    }

    private void HandleDeath()
    {
        if (canvas != null)
            canvas.enabled = false;
    }

    private static float SafeInverseScale(float parentScale)
    {
        float absoluteScale = Mathf.Abs(parentScale);
        return absoluteScale > 0.0001f ? WorldPixelScale / absoluteScale : WorldPixelScale;
    }

    private void OnValidate()
    {
        heightAboveEnemy = Mathf.Max(0f, heightAboveEnemy);
    }
}
