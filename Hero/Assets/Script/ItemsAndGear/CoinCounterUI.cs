using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoinCounterUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TMP_Text coinText;
    [Tooltip("Optional coin Image. The counter itself animates when this is empty.")]
    [SerializeField] private Graphic coinImage;
    [Tooltip("Optional reusable gain label. One is created automatically when empty.")]
    [SerializeField] private TMP_Text gainText;

    [Header("Counter")]
    [SerializeField] private string prefix = "Coins: ";

    [Header("Gain Text")]
    [SerializeField] private string gainPrefix = "+";
    [SerializeField] private string gainSuffix = " Coins";
    [Min(1f)] [SerializeField] private float gainFontSize = 24f;
    [SerializeField] private Color gainColor = new Color(1f, 0.82f, 0.2f, 1f);
    [Min(0.05f)] [SerializeField] private float feedbackDuration = 1f;
    [SerializeField] private Vector2 gainStartOffset = new Vector2(0f, -34f);
    [SerializeField] private Vector2 gainTravel = new Vector2(0f, 34f);

    [Header("Coin Shake And Pop")]
    [Min(0f)] [SerializeField] private float shakeDuration = 0.35f;
    [Min(0f)] [SerializeField] private float shakeStrength = 7f;
    [Min(1f)] [SerializeField] private float popScale = 1.18f;

    private RectTransform animatedTarget;
    private RectTransform gainRect;
    private Vector2 targetRestPosition;
    private Vector3 targetRestScale;
    private int pendingGain;
    private float feedbackTime;
    private bool feedbackActive;

    private void Awake()
    {
        if (coinText == null)
            coinText = GetComponent<TMP_Text>();

        PreparePresentation();
        FindWallet();
        Refresh();
    }

    private void OnEnable()
    {
        FindWallet();

        if (wallet != null)
        {
            wallet.GoldChanged += Refresh;
            wallet.GoldAdded += ShowGoldGain;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (wallet != null)
        {
            wallet.GoldChanged -= Refresh;
            wallet.GoldAdded -= ShowGoldGain;
        }

        ResetPresentation();
    }

    private void Update()
    {
        if (!feedbackActive)
            return;

        feedbackTime += Time.unscaledDeltaTime;
        float duration = Mathf.Max(0.05f, feedbackDuration);
        float normalizedTime = Mathf.Clamp01(feedbackTime / duration);

        if (gainRect != null)
            gainRect.anchoredPosition = gainStartOffset + gainTravel * normalizedTime;

        if (gainText != null)
        {
            Color color = gainColor;
            color.a *= 1f - normalizedTime;
            gainText.color = color;
        }

        AnimateTarget();

        if (normalizedTime >= 1f)
            ResetPresentation();
    }

    public void Refresh()
    {
        if (coinText == null)
            return;

        coinText.text = wallet == null
            ? $"{prefix}N/A"
            : $"{prefix}<color=#FFD166>{wallet.Gold}</color>";
    }

    private void ShowGoldGain(int amount)
    {
        if (amount <= 0)
            return;

        pendingGain += amount;
        feedbackTime = 0f;
        feedbackActive = true;

        if (gainText != null)
        {
            gainText.text = $"{gainPrefix}{pendingGain}{gainSuffix}";
            gainText.fontSize = gainFontSize;
            gainText.color = gainColor;
            gainText.gameObject.SetActive(true);
        }

        if (gainRect != null)
            gainRect.anchoredPosition = gainStartOffset;
    }

    private void PreparePresentation()
    {
        animatedTarget = coinImage != null
            ? coinImage.rectTransform
            : transform as RectTransform;

        if (animatedTarget != null)
        {
            targetRestPosition = animatedTarget.anchoredPosition;
            targetRestScale = animatedTarget.localScale;
        }

        if (gainText == null && coinText != null)
            gainText = CreateGainText();

        if (gainText != null)
        {
            gainRect = gainText.rectTransform;
            gainText.raycastTarget = false;
            gainText.gameObject.SetActive(false);
        }
    }

    private TMP_Text CreateGainText()
    {
        GameObject label = new GameObject(
            "CoinGainText", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(220f, 44f);

        TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
        labelText.font = coinText.font;
        labelText.fontSharedMaterial = coinText.fontSharedMaterial;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.enableWordWrapping = false;
        return labelText;
    }

    private void AnimateTarget()
    {
        if (animatedTarget == null)
            return;

        float safeShakeDuration = Mathf.Max(0.0001f, shakeDuration);
        float shakeProgress = Mathf.Clamp01(feedbackTime / safeShakeDuration);
        float strength = shakeStrength * (1f - shakeProgress);
        float x = Mathf.Sin(feedbackTime * 62f) * strength;
        float y = Mathf.Sin(feedbackTime * 47f) * strength * 0.45f;
        animatedTarget.anchoredPosition = targetRestPosition + new Vector2(x, y);

        float pop = 1f + (Mathf.Max(1f, popScale) - 1f) *
            Mathf.Sin(shakeProgress * Mathf.PI);
        animatedTarget.localScale = targetRestScale * pop;
    }

    private void ResetPresentation()
    {
        feedbackActive = false;
        feedbackTime = 0f;
        pendingGain = 0;

        if (gainText != null)
            gainText.gameObject.SetActive(false);

        if (animatedTarget != null)
        {
            animatedTarget.anchoredPosition = targetRestPosition;
            animatedTarget.localScale = targetRestScale;
        }
    }

    private void FindWallet()
    {
        if (wallet != null)
            return;

#if UNITY_2023_1_OR_NEWER
        wallet = FindAnyObjectByType<PlayerWallet>();
#else
        wallet = FindObjectOfType<PlayerWallet>();
#endif
    }
}
