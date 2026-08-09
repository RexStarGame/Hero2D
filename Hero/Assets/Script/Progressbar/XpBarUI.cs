using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XPBarUI : MonoBehaviour
{
    public Slider slider;
    public PlayerXP player;

    [Header("XP Text")]
    [Tooltip("Optional. If empty, a centered TextMeshPro label is created automatically.")]
    [SerializeField] private TMP_Text xpText;

    private void OnEnable()
    {
        ResolveReferences();

        if (player != null)
            player.ProgressChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (player != null)
            player.ProgressChanged -= Refresh;
    }

    private void ResolveReferences()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        if (player == null)
            player = FindAnyObjectByType<PlayerXP>();

        if (xpText == null)
        {
            Transform existingText = transform.Find("XPText");
            if (existingText != null)
                xpText = existingText.GetComponent<TMP_Text>();
        }

        if (xpText == null)
            xpText = CreateXpText();
    }

    private TMP_Text CreateXpText()
    {
        GameObject textObject = new GameObject(
            "XPText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    private void Refresh()
    {
        if (player == null)
            return;

        int requiredXp = Mathf.Max(1, player.xpToNextLevel);
        int currentXp = Mathf.Clamp(player.xp, 0, requiredXp);

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = (float)currentXp / requiredXp;
        }

        if (xpText != null)
            xpText.text = $"{currentXp:N0} / {requiredXp:N0} EXP";
    }
}
