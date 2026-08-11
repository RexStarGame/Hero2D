using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttack))]
public sealed class PlayerDamageNumberWorld : MonoBehaviour
{
    [Header("Spawn Position")]
    [Tooltip("Where each number starts, measured in Canvas units. X moves it left/right and Y moves it up/down relative to the player.")]
    [SerializeField] private Vector2 localStartOffset = new Vector2(0f, 120f);
    [Tooltip("Separates consecutive damage numbers horizontally so rapid hits remain readable.")]
    [Min(0f)] [SerializeField] private float horizontalSpacing = 22f;

    [Header("Motion And Timing")]
    [Tooltip("How many seconds the number remains visible.")]
    [Min(0.1f)] [SerializeField] private float lifetime = 1.15f;
    [Tooltip("How far the number travels upward before disappearing, measured in Canvas units.")]
    [Min(0f)] [SerializeField] private float riseDistance = 80f;

    [Header("Text Appearance")]
    [SerializeField] private Color normalColor = new Color(1f, 0.95f, 0.82f, 1f);
    [SerializeField] private Color criticalColor = new Color(1f, 0.55f, 0.08f, 1f);
    [Tooltip("Font size used by normal damage numbers.")]
    [Min(1f)] [SerializeField] private float normalFontSize = 34f;
    [Tooltip("Font size used by critical-hit damage numbers.")]
    [Min(1f)] [SerializeField] private float criticalFontSize = 42f;
    [Tooltip("Text placed before critical damage. Include a trailing space if desired.")]
    [SerializeField] private string criticalPrefix = "CRIT ";
    [Tooltip("Text placed after every damage value. Set this to empty to remove it.")]
    [SerializeField] private string damageSuffix = "!";

    [Header("Guard Feedback")]
    [Tooltip("Color used when Guard successfully reduces incoming damage.")]
    [SerializeField] private Color guardColor = new Color(0.35f, 0.85f, 1f, 1f);
    [Tooltip("Font size used by the two-line Guard popup.")]
    [Min(1f)] [SerializeField] private float guardFontSize = 27f;
    [Tooltip("Size available to the two-line Guard popup.")]
    [SerializeField] private Vector2 guardTextBoxSize = new Vector2(360f, 110f);

    [Header("World Space Canvas")]
    [Tooltip("Overall world-space size of the complete effect. Lower values make all text and movement smaller on screen.")]
    [Min(0.0001f)] [SerializeField] private float worldScale = 0.01f;
    [Tooltip("Internal size of the generated World Space Canvas.")]
    [SerializeField] private Vector2 canvasSize = new Vector2(400f, 300f);
    [Tooltip("Size available to each TMP damage label. Increase this only if long values are clipped.")]
    [SerializeField] private Vector2 textBoxSize = new Vector2(280f, 70f);
    [Tooltip("Render priority for the damage-number Canvas.")]
    [SerializeField] private int sortingOrder = 100;

    [Header("Pooling")]
    [Min(1)] [SerializeField] private int initialPoolSize = 10;

    private readonly Queue<DamageNumber> available = new Queue<DamageNumber>();
    private readonly List<DamageNumber> active = new List<DamageNumber>();
    private RectTransform canvasRect;
    private int sequence;

    private sealed class DamageNumber
    {
        public GameObject GameObject;
        public RectTransform RectTransform;
        public TextMeshProUGUI Text;
        public Vector2 StartPosition;
        public float Elapsed;
    }

    private void Awake()
    {
        BuildCanvas();

        for (int i = 0; i < initialPoolSize; i++)
            available.Enqueue(CreateDamageNumber());

        enabled = false;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            DamageNumber number = active[i];
            number.Elapsed += deltaTime;
            float progress = Mathf.Clamp01(number.Elapsed / lifetime);
            float easedProgress = 1f - (1f - progress) * (1f - progress);

            number.RectTransform.anchoredPosition = number.StartPosition + Vector2.up * (riseDistance * easedProgress);

            Color color = number.Text.color;
            color.a = 1f - Mathf.SmoothStep(0f, 1f, progress);
            number.Text.color = color;

            if (progress >= 1f)
                Recycle(i, number);
        }

        if (active.Count == 0)
            enabled = false;
    }

    public void Show(int damage, bool isCritical)
    {
        if (damage <= 0)
            return;

        DamageNumber number = AcquireNumber();
        number.RectTransform.sizeDelta = textBoxSize;
        number.Text.text = isCritical
            ? $"{criticalPrefix}{damage}{damageSuffix}"
            : $"{damage}{damageSuffix}";
        number.Text.fontSize = isCritical ? criticalFontSize : normalFontSize;
        number.Text.color = isCritical ? criticalColor : normalColor;
        ActivateNumber(number);
    }

    public void ShowGuard(float preventedDamage, float blockedPercent)
    {
        if (preventedDamage <= 0f || blockedPercent <= 0f)
            return;

        DamageNumber number = AcquireNumber();
        number.RectTransform.sizeDelta = guardTextBoxSize;
        number.Text.text =
            $"GUARD! -{FormatGuardDamage(preventedDamage)} DMG\n{blockedPercent:0.##}% blocked";
        number.Text.fontSize = guardFontSize;
        number.Text.color = guardColor;
        ActivateNumber(number);
    }

    private static string FormatGuardDamage(float value)
    {
        if (value >= 1f)
            return value.ToString("0.##");
        if (value >= 0.01f)
            return value.ToString("0.###");
        return value.ToString("0.####");
    }

    private DamageNumber AcquireNumber()
    {
        DamageNumber number = available.Count > 0
            ? available.Dequeue()
            : CreateDamageNumber();

        float horizontalOffset = ((sequence++ % 3) - 1) * horizontalSpacing;
        number.StartPosition = localStartOffset + Vector2.right * horizontalOffset;
        number.Elapsed = 0f;
        number.RectTransform.anchoredPosition = number.StartPosition;
        return number;
    }

    private void ActivateNumber(DamageNumber number)
    {
        number.GameObject.SetActive(true);
        active.Add(number);
        enabled = true;
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject(
            "Player Damage Numbers",
            typeof(RectTransform),
            typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.layer = gameObject.layer;

        canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.localPosition = Vector3.zero;
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale = Vector3.one * worldScale;
        canvasRect.sizeDelta = canvasSize;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
    }

    private DamageNumber CreateDamageNumber()
    {
        GameObject textObject = new GameObject(
            "Damage Number",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasRect, false);
        textObject.layer = gameObject.layer;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = textBoxSize;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        textObject.SetActive(false);
        return new DamageNumber
        {
            GameObject = textObject,
            RectTransform = rect,
            Text = text
        };
    }

    private void Recycle(int activeIndex, DamageNumber number)
    {
        number.GameObject.SetActive(false);
        int lastIndex = active.Count - 1;
        active[activeIndex] = active[lastIndex];
        active.RemoveAt(lastIndex);
        available.Enqueue(number);
    }

    private void OnDisable()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            DamageNumber number = active[i];
            number.GameObject.SetActive(false);
            available.Enqueue(number);
        }

        active.Clear();
    }

    private void OnValidate()
    {
        lifetime = Mathf.Max(0.1f, lifetime);
        riseDistance = Mathf.Max(0f, riseDistance);
        horizontalSpacing = Mathf.Max(0f, horizontalSpacing);
        normalFontSize = Mathf.Max(1f, normalFontSize);
        criticalFontSize = Mathf.Max(1f, criticalFontSize);
        guardFontSize = Mathf.Max(1f, guardFontSize);
        worldScale = Mathf.Max(0.0001f, worldScale);
        canvasSize.x = Mathf.Max(1f, canvasSize.x);
        canvasSize.y = Mathf.Max(1f, canvasSize.y);
        textBoxSize.x = Mathf.Max(1f, textBoxSize.x);
        textBoxSize.y = Mathf.Max(1f, textBoxSize.y);
        guardTextBoxSize.x = Mathf.Max(1f, guardTextBoxSize.x);
        guardTextBoxSize.y = Mathf.Max(1f, guardTextBoxSize.y);
        initialPoolSize = Mathf.Max(1, initialPoolSize);
    }
}
