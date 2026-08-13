using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SafeZoneFeedbackUI : MonoBehaviour
{
    [Header("Optional HUD References")]
    [SerializeField] private CanvasGroup safeBadge;
    [SerializeField] private Image safeIcon;
    [SerializeField] private TMP_Text safeLabel;
    [SerializeField] private CanvasGroup notificationGroup;
    [SerializeField] private TMP_Text notificationText;

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

    [Header("Text")]
    [SerializeField] private string safeLabelText = "SAFE";
    [SerializeField] private string enteredMessage = "SAFE ZONE\nCombat is disabled";
    [SerializeField] private string exitedMessage = "COMBAT ENABLED";
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
            safeLabel.text = safeLabelText;

        if (safeIcon != null)
            safeIcon.enabled = visible;

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
        if (safeBadge != null && safeLabel != null && notificationGroup != null && notificationText != null)
            return;

        GameObject canvasObject = new GameObject("Safe Zone Feedback Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject badgeObject = new GameObject("Safe Badge", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        badgeObject.transform.SetParent(canvasObject.transform, false);
        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(1f, 1f);
        badgeRect.anchoredPosition = new Vector2(-28f, -28f);
        badgeRect.sizeDelta = new Vector2(118f, 42f);

        Image badgeBackground = badgeObject.GetComponent<Image>();
        badgeBackground.color = new Color(0.04f, 0.24f, 0.20f, 0.88f);
        badgeBackground.raycastTarget = false;
        safeBadge = badgeObject.GetComponent<CanvasGroup>();

        safeLabel = CreateText("Safe Label", badgeObject.transform, 24f, FontStyles.Bold);
        safeLabel.rectTransform.anchorMin = Vector2.zero;
        safeLabel.rectTransform.anchorMax = Vector2.one;
        safeLabel.rectTransform.offsetMin = Vector2.zero;
        safeLabel.rectTransform.offsetMax = Vector2.zero;
        safeLabel.color = new Color(0.75f, 1f, 0.88f, 1f);

        GameObject notificationObject = new GameObject("Safe Zone Notification", typeof(RectTransform), typeof(CanvasGroup));
        notificationObject.transform.SetParent(canvasObject.transform, false);
        RectTransform notificationRect = notificationObject.GetComponent<RectTransform>();
        notificationRect.anchorMin = new Vector2(0.5f, 1f);
        notificationRect.anchorMax = new Vector2(0.5f, 1f);
        notificationRect.pivot = new Vector2(0.5f, 1f);
        notificationRect.anchoredPosition = new Vector2(0f, -105f);
        notificationRect.sizeDelta = new Vector2(620f, 100f);

        notificationGroup = notificationObject.GetComponent<CanvasGroup>();
        notificationText = CreateText("Notification Text", notificationObject.transform, 30f, FontStyles.Bold);
        notificationText.rectTransform.anchorMin = Vector2.zero;
        notificationText.rectTransform.anchorMax = Vector2.one;
        notificationText.rectTransform.offsetMin = Vector2.zero;
        notificationText.rectTransform.offsetMax = Vector2.zero;
        notificationText.color = new Color(0.82f, 1f, 0.92f, 1f);
        notificationText.enableWordWrapping = false;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, float fontSize, FontStyles style)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private void OnValidate()
    {
        statusCheckInterval = Mathf.Max(0.05f, statusCheckInterval);
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        visibleDuration = Mathf.Max(0f, visibleDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        blockedMessageCooldown = Mathf.Max(0f, blockedMessageCooldown);
    }
}
