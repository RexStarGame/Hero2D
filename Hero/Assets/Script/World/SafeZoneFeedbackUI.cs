using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SafeZoneFeedbackUI : MonoBehaviour
{
    [Header("Optional Custom HUD References")]
    [Tooltip("Assign your own UI references, or leave them empty to use the automatic HUD.")]
    [SerializeField] private CanvasGroup safeBadge;
    [SerializeField] private Image badgeBackground;
    [SerializeField] private Image safeIcon;
    [SerializeField] private TMP_Text safeLabel;
    [SerializeField] private CanvasGroup notificationGroup;
    [SerializeField] private TMP_Text notificationText;

    [Header("Automatic HUD")]
    [SerializeField] private bool createAutomaticHud = true;
    [SerializeField] private int canvasSortingOrder = 50;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Header("Safe Badge Layout")]
    [SerializeField] private Vector2 badgeAnchor = new Vector2(1f, 1f);
    [SerializeField] private Vector2 badgePivot = new Vector2(1f, 1f);
    [SerializeField] private Vector2 badgePosition = new Vector2(-28f, -28f);
    [SerializeField] private Vector2 badgeSize = new Vector2(150f, 46f);
    [SerializeField] private Color badgeBackgroundColor = new Color(0.04f, 0.24f, 0.20f, 0.88f);
    [SerializeField] private bool showBadgeBackground = true;

    [Header("Safe Icon")]
    [SerializeField] private Sprite safeIconSprite;
    [SerializeField] private Color safeIconColor = new Color(0.75f, 1f, 0.88f, 1f);
    [SerializeField] private bool showSafeIcon = true;
    [SerializeField] private Vector2 iconPosition = new Vector2(14f, 0f);
    [SerializeField] private Vector2 iconSize = new Vector2(28f, 28f);

    [Header("Safe Label")]
    [SerializeField] private string safeLabelText = "SAFE";
    [SerializeField] private TMP_FontAsset safeLabelFont;
    [Min(1f)]
    [SerializeField] private float safeLabelFontSize = 24f;
    [SerializeField] private FontStyles safeLabelFontStyle = FontStyles.Bold;
    [SerializeField] private Color safeLabelColor = new Color(0.75f, 1f, 0.88f, 1f);
    [SerializeField] private bool showSafeLabel = true;
    [SerializeField] private Vector2 labelOffset = new Vector2(18f, 0f);

    [Header("Notification Layout")]
    [SerializeField] private Vector2 notificationAnchor = new Vector2(0.5f, 1f);
    [SerializeField] private Vector2 notificationPivot = new Vector2(0.5f, 1f);
    [SerializeField] private Vector2 notificationPosition = new Vector2(0f, -105f);
    [SerializeField] private Vector2 notificationSize = new Vector2(620f, 100f);
    [SerializeField] private TMP_FontAsset notificationFont;
    [Min(1f)]
    [SerializeField] private float notificationFontSize = 30f;
    [SerializeField] private FontStyles notificationFontStyle = FontStyles.Bold;
    [SerializeField] private Color notificationColor = new Color(0.82f, 1f, 0.92f, 1f);

    [Header("Status Check")]
    [Min(0.05f)]
    [SerializeField] private float statusCheckInterval = 0.15f;

    [Header("Notification Timing")]
    [Min(0f)]
    [SerializeField] private float fadeInDuration = 0.2f;
    [Min(0f)]
    [SerializeField] private float visibleDuration = 1.6f;
    [Min(0f)]
    [SerializeField] private float fadeOutDuration = 0.65f;
    [Min(0f)]
    [SerializeField] private float blockedMessageCooldown = 1.5f;

    [Header("Notification Text")]
    [TextArea]
    [SerializeField] private string enteredMessage = "SAFE ZONE\nCombat is disabled";
    [TextArea]
    [SerializeField] private string exitedMessage = "COMBAT ENABLED";
    [TextArea]
    [SerializeField] private string blockedMessage = "Combat is disabled here";

    private Coroutine statusRoutine;
    private Coroutine notificationRoutine;
    private WaitForSecondsRealtime statusWait;
    private bool hasKnownStatus;
    private bool isInSafeZone;
    private float nextBlockedMessageTime;

    public bool IsInSafeZone => isInSafeZone;

    private void Awake()
    {
        EnsureHud();
        ApplyVisualSettings();
        ApplySafeBadge(false);
        SetNotificationVisible(false);
        statusWait = new WaitForSecondsRealtime(Mathf.Max(0.05f, statusCheckInterval));
    }

    private void OnEnable()
    {
        if (statusRoutine == null)
            statusRoutine = StartCoroutine(MonitorSafeZone());
    }

    private void OnDisable()
    {
        if (statusRoutine != null)
        {
            StopCoroutine(statusRoutine);
            statusRoutine = null;
        }

        if (notificationRoutine != null)
        {
            StopCoroutine(notificationRoutine);
            notificationRoutine = null;
        }

        hasKnownStatus = false;
        ApplySafeBadge(false);
        SetNotificationVisible(false);
    }

    public void ShowAttackBlocked()
    {
        if (!isActiveAndEnabled || Time.unscaledTime < nextBlockedMessageTime)
            return;

        nextBlockedMessageTime = Time.unscaledTime + Mathf.Max(0f, blockedMessageCooldown);
        ShowNotification(blockedMessage);
    }

    private IEnumerator MonitorSafeZone()
    {
        while (true)
        {
            bool currentStatus = SafeZone2D.IsPlayerAttackBlocked(transform.position);

            if (!hasKnownStatus || currentStatus != isInSafeZone)
            {
                bool hadPreviousStatus = hasKnownStatus;
                hasKnownStatus = true;
                isInSafeZone = currentStatus;
                ApplySafeBadge(isInSafeZone);

                if (isInSafeZone)
                    ShowNotification(enteredMessage);
                else if (hadPreviousStatus)
                    ShowNotification(exitedMessage);
            }

            yield return statusWait;
        }
    }

    private void ApplySafeBadge(bool visible)
    {
        if (safeLabel != null)
        {
            safeLabel.text = safeLabelText;
            safeLabel.enabled = visible && showSafeLabel;
        }

        if (safeIcon != null)
            safeIcon.enabled = visible && showSafeIcon && safeIcon.sprite != null;

        if (badgeBackground != null)
            badgeBackground.enabled = visible && showBadgeBackground;

        if (safeBadge == null)
            return;

        safeBadge.alpha = visible ? 1f : 0f;
        safeBadge.interactable = false;
        safeBadge.blocksRaycasts = false;
    }

    private void ShowNotification(string message)
    {
        if (notificationText == null || string.IsNullOrWhiteSpace(message))
            return;

        if (notificationRoutine != null)
            StopCoroutine(notificationRoutine);

        notificationText.text = message;
        notificationRoutine = StartCoroutine(PlayNotification());
    }

    private IEnumerator PlayNotification()
    {
        SetNotificationVisible(true);

        if (fadeInDuration > 0f)
            yield return FadeNotification(0f, 1f, fadeInDuration);
        else if (notificationGroup != null)
            notificationGroup.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < visibleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (fadeOutDuration > 0f)
            yield return FadeNotification(1f, 0f, fadeOutDuration);

        SetNotificationVisible(false);
        notificationRoutine = null;
    }

    private IEnumerator FadeNotification(float from, float to, float duration)
    {
        if (notificationGroup == null)
            yield break;

        float elapsed = 0f;
        notificationGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            notificationGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        notificationGroup.alpha = to;
    }

    private void SetNotificationVisible(bool visible)
    {
        if (notificationGroup == null)
            return;

        notificationGroup.alpha = visible ? 1f : 0f;
        notificationGroup.interactable = false;
        notificationGroup.blocksRaycasts = false;
    }

    private void EnsureHud()
    {
        bool hasCustomHud = safeBadge != null && safeLabel != null && notificationGroup != null && notificationText != null;
        if (hasCustomHud || !createAutomaticHud)
            return;

        GameObject canvasObject = new GameObject("Safe Zone Feedback Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject badgeObject = new GameObject("Safe Badge", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        badgeObject.transform.SetParent(canvasObject.transform, false);
        badgeBackground = badgeObject.GetComponent<Image>();
        badgeBackground.raycastTarget = false;
        safeBadge = badgeObject.GetComponent<CanvasGroup>();

        GameObject iconObject = new GameObject("Safe Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(badgeObject.transform, false);
        safeIcon = iconObject.GetComponent<Image>();
        safeIcon.preserveAspect = true;
        safeIcon.raycastTarget = false;

        safeLabel = CreateText("Safe Label", badgeObject.transform);

        GameObject notificationObject = new GameObject("Safe Zone Notification", typeof(RectTransform), typeof(CanvasGroup));
        notificationObject.transform.SetParent(canvasObject.transform, false);
        notificationGroup = notificationObject.GetComponent<CanvasGroup>();
        notificationText = CreateText("Notification Text", notificationObject.transform);
        notificationText.enableWordWrapping = false;
    }

    private void ApplyVisualSettings()
    {
        if (safeBadge != null && safeBadge.transform is RectTransform badgeRect)
            ConfigureRect(badgeRect, badgeAnchor, badgePivot, badgePosition, badgeSize);

        if (badgeBackground != null)
        {
            badgeBackground.color = badgeBackgroundColor;
            badgeBackground.raycastTarget = false;
        }

        if (safeIcon != null)
        {
            if (safeIconSprite != null)
                safeIcon.sprite = safeIconSprite;
            safeIcon.color = safeIconColor;
            safeIcon.preserveAspect = true;
            safeIcon.raycastTarget = false;
            RectTransform iconRect = safeIcon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = iconPosition;
            iconRect.sizeDelta = iconSize;
        }

        if (safeLabel != null)
        {
            safeLabel.text = safeLabelText;
            if (safeLabelFont != null)
                safeLabel.font = safeLabelFont;
            safeLabel.fontSize = safeLabelFontSize;
            safeLabel.fontStyle = safeLabelFontStyle;
            safeLabel.color = safeLabelColor;
            safeLabel.alignment = TextAlignmentOptions.Center;
            safeLabel.raycastTarget = false;
            safeLabel.rectTransform.anchorMin = Vector2.zero;
            safeLabel.rectTransform.anchorMax = Vector2.one;
            safeLabel.rectTransform.offsetMin = new Vector2(labelOffset.x, labelOffset.y);
            safeLabel.rectTransform.offsetMax = new Vector2(labelOffset.x, labelOffset.y);
        }

        if (notificationGroup != null && notificationGroup.transform is RectTransform notificationRect)
            ConfigureRect(notificationRect, notificationAnchor, notificationPivot, notificationPosition, notificationSize);

        if (notificationText != null)
        {
            if (notificationFont != null)
                notificationText.font = notificationFont;
            notificationText.fontSize = notificationFontSize;
            notificationText.fontStyle = notificationFontStyle;
            notificationText.color = notificationColor;
            notificationText.alignment = TextAlignmentOptions.Center;
            notificationText.raycastTarget = false;
            notificationText.rectTransform.anchorMin = Vector2.zero;
            notificationText.rectTransform.anchorMax = Vector2.one;
            notificationText.rectTransform.offsetMin = Vector2.zero;
            notificationText.rectTransform.offsetMax = Vector2.zero;
        }
    }

    private static void ConfigureRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static TMP_Text CreateText(string objectName, Transform parent)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TextMeshProUGUI>();
    }

    private void OnValidate()
    {
        statusCheckInterval = Mathf.Max(0.05f, statusCheckInterval);
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        visibleDuration = Mathf.Max(0f, visibleDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        blockedMessageCooldown = Mathf.Max(0f, blockedMessageCooldown);
        safeLabelFontSize = Mathf.Max(1f, safeLabelFontSize);
        notificationFontSize = Mathf.Max(1f, notificationFontSize);
        referenceResolution.x = Mathf.Max(1f, referenceResolution.x);
        referenceResolution.y = Mathf.Max(1f, referenceResolution.y);

        if (Application.isPlaying)
            ApplyVisualSettings();
    }
}
