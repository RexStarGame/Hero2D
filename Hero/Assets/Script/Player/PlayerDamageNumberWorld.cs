using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttack))]
public sealed class PlayerDamageNumberWorld : MonoBehaviour
{
    [Header("Motion")]
    [Min(0.1f)] [SerializeField] private float lifetime = 1.15f;
    [Min(0f)] [SerializeField] private float riseDistance = 80f;
    [SerializeField] private Vector2 localStartOffset = new Vector2(0f, 120f);
    [Min(0f)] [SerializeField] private float horizontalSpacing = 22f;

    [Header("Appearance")]
    [SerializeField] private Color normalColor = new Color(1f, 0.95f, 0.82f, 1f);
    [SerializeField] private Color criticalColor = new Color(1f, 0.55f, 0.08f, 1f);
    [Min(1f)] [SerializeField] private float normalFontSize = 34f;
    [Min(1f)] [SerializeField] private float criticalFontSize = 42f;

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

        DamageNumber number = available.Count > 0
            ? available.Dequeue()
            : CreateDamageNumber();

        float horizontalOffset = ((sequence++ % 3) - 1) * horizontalSpacing;
        number.StartPosition = localStartOffset + Vector2.right * horizontalOffset;
        number.Elapsed = 0f;
        number.RectTransform.anchoredPosition = number.StartPosition;
        number.Text.text = isCritical ? $"CRIT {damage}!" : $"{damage}!";
        number.Text.fontSize = isCritical ? criticalFontSize : normalFontSize;
        number.Text.color = isCritical ? criticalColor : normalColor;
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
        canvasRect.localScale = Vector3.one * 0.01f;
        canvasRect.sizeDelta = new Vector2(400f, 300f);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
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
        rect.sizeDelta = new Vector2(280f, 70f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;
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
        initialPoolSize = Mathf.Max(1, initialPoolSize);
    }
}
