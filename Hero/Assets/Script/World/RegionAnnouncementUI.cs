using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class RegionAnnouncementUI : MonoBehaviour
{
    public static RegionAnnouncementUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TMP_Text regionNameText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float fadeInDuration = 0.35f;
    [Min(0f)]
    [SerializeField] private float visibleDuration = 3f;
    [Min(0f)]
    [SerializeField] private float fadeOutDuration = 1.25f;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine announcementRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one RegionAnnouncementUI is active. The newest one will be ignored.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(string regionName)
    {
        if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(regionName))
            return;

        if (regionNameText == null)
        {
            Debug.LogWarning("RegionAnnouncementUI needs a TextMeshPro text reference.", this);
            return;
        }

        if (announcementRoutine != null)
            StopCoroutine(announcementRoutine);

        regionNameText.text = regionName;
        announcementRoutine = StartCoroutine(PlayAnnouncement());
    }

    private IEnumerator PlayAnnouncement()
    {
        SetVisible(true);
        canvasGroup.alpha = fadeInDuration <= 0f ? 1f : 0f;

        if (fadeInDuration > 0f)
            yield return Fade(0f, 1f, fadeInDuration);

        if (visibleDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < visibleDuration)
            {
                elapsed += DeltaTime;
                yield return null;
            }
        }

        if (fadeOutDuration > 0f)
            yield return Fade(1f, 0f, fadeOutDuration);

        SetVisible(false);
        announcementRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += DeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnValidate()
    {
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        visibleDuration = Mathf.Max(0f, visibleDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
    }
}
