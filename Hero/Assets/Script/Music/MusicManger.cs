using UnityEngine;
using UnityEngine.UI; // Vigtigt: Giver adgang til UI elementer som Slider

public class MusicManager : MonoBehaviour
{
    [Header("UI Opsætning")]
    public GameObject pauseMenuUI; // Træk dit Pause Panel herind
    public Slider volumeSlider;    // Træk din Volume Slider herind

    [Header("Audio Opsætning")]
    public AudioSource musicSource; // Træk din AudioSource (musikken) herind

    private bool isGamePaused = false;
    private GameObject navigationSettingsPanel;

    // Start køres før første frame
    void Start()
    {
        // Sørg for at pausemenuen er skjult fra start
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        // Sæt sliderens værdi til at matche den nuværende musik-volumen
        if (musicSource != null && volumeSlider != null)
        {
            volumeSlider.value = musicSource.volume;

            // Dette sikrer, at funktionen kaldes, når man rykker på slideren
            volumeSlider.onValueChanged.AddListener(SetLevel);
        }

        BuildNavigationSettingsUI();
    }

    // Update køres hver frame
    void Update()
    {
        if (navigationSettingsPanel != null && navigationSettingsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            navigationSettingsPanel.SetActive(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                // Block opening pause if Upgrade menu is open
                if (!MenuLock.CanOpen(MenuOwner.Pause))
                    return;

                PauseGame();
            }
        }
    }
    void PauseGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
            isGamePaused = true;

            MenuLock.Set(MenuOwner.Pause);
        }
    }

    public void ResumeGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
            if (navigationSettingsPanel != null)
                navigationSettingsPanel.SetActive(false);
            Time.timeScale = 1f;
            isGamePaused = false;

            MenuLock.Clear(MenuOwner.Pause);
        }
    }
    private void OnDisable()
    {
        if (isGamePaused)
        {
            MenuLock.Clear(MenuOwner.Pause);
            Time.timeScale = 1f;
        }
    }
    private void BuildNavigationSettingsUI()
    {
        if (pauseMenuUI == null || navigationSettingsPanel != null)
            return;

        Button settingsButton = CreateButton("Navigation Settings Button", pauseMenuUI.transform, "Settings");
        RectTransform settingsButtonRect = settingsButton.GetComponent<RectTransform>();
        settingsButtonRect.anchorMin = new Vector2(1f, 0f);
        settingsButtonRect.anchorMax = new Vector2(1f, 0f);
        settingsButtonRect.pivot = new Vector2(1f, 0f);
        settingsButtonRect.anchoredPosition = new Vector2(-28f, 28f);
        settingsButtonRect.sizeDelta = new Vector2(190f, 50f);
        settingsButton.onClick.AddListener(OpenNavigationSettings);

        navigationSettingsPanel = CreatePanel("Navigation Settings", pauseMenuUI.transform, new Color(0.035f, 0.055f, 0.075f, 0.98f));
        RectTransform panelRect = navigationSettingsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Text title = CreateLabel("Title", navigationSettingsPanel.transform, "NAVIGATION SETTINGS", 30, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.78f), new Vector2(520f, 60f), Vector2.zero);

        Toggle mapToggle = CreateToggle("Show Waypoint On Map", navigationSettingsPanel.transform, "Show waypoint on map");
        SetRect(mapToggle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.58f), new Vector2(430f, 52f), Vector2.zero);
        mapToggle.SetIsOnWithoutNotify(LiveMinimapHUD.ShowWaypointOnMap);
        mapToggle.onValueChanged.AddListener(LiveMinimapHUD.SetShowWaypointOnMap);

        Toggle hudToggle = CreateToggle("Show HUD Direction Arrow", navigationSettingsPanel.transform, "Show direction arrow on HUD");
        SetRect(hudToggle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.46f), new Vector2(430f, 52f), Vector2.zero);
        hudToggle.SetIsOnWithoutNotify(LiveMinimapHUD.ShowHudDirectionArrow);
        hudToggle.onValueChanged.AddListener(LiveMinimapHUD.SetShowHudDirectionArrow);

        Text savedText = CreateLabel("Saved Hint", navigationSettingsPanel.transform, "Changes are saved automatically", 18, TextAnchor.MiddleCenter);
        savedText.color = new Color(0.75f, 0.82f, 0.9f, 1f);
        SetRect(savedText.rectTransform, new Vector2(0.5f, 0.32f), new Vector2(430f, 38f), Vector2.zero);

        Button backButton = CreateButton("Back Button", navigationSettingsPanel.transform, "Back");
        SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.18f), new Vector2(190f, 50f), Vector2.zero);
        backButton.onClick.AddListener(() => navigationSettingsPanel.SetActive(false));

        navigationSettingsPanel.SetActive(false);
        navigationSettingsPanel.transform.SetAsLastSibling();
    }

    private void OpenNavigationSettings()
    {
        if (navigationSettingsPanel != null)
        {
            navigationSettingsPanel.SetActive(true);
            navigationSettingsPanel.transform.SetAsLastSibling();
        }
    }

    private static GameObject CreatePanel(string objectName, Transform parent, Color color)
    {
        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Button CreateButton(string objectName, Transform parent, string label)
    {
        GameObject buttonObject = CreatePanel(objectName, parent, new Color(0.16f, 0.34f, 0.48f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        Text text = CreateLabel("Label", buttonObject.transform, label, 21, TextAnchor.MiddleCenter);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static Toggle CreateToggle(string objectName, Transform parent, string label)
    {
        GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(Toggle));
        root.transform.SetParent(parent, false);
        Toggle toggle = root.GetComponent<Toggle>();

        GameObject backgroundObject = CreatePanel("Background", root.transform, new Color(0.12f, 0.16f, 0.2f, 1f));
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0f, 0.5f);
        backgroundRect.pivot = new Vector2(0f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(40f, 40f);

        GameObject checkmarkObject = CreatePanel("Checkmark", backgroundObject.transform, new Color(0.1f, 0.9f, 1f, 1f));
        RectTransform checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = Vector2.zero;
        checkmarkRect.anchorMax = Vector2.one;
        checkmarkRect.offsetMin = new Vector2(7f, 7f);
        checkmarkRect.offsetMax = new Vector2(-7f, -7f);

        Text text = CreateLabel("Label", root.transform, label, 21, TextAnchor.MiddleLeft);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(58f, 0f);
        text.rectTransform.offsetMax = Vector2.zero;

        toggle.targetGraphic = backgroundObject.GetComponent<Image>();
        toggle.graphic = checkmarkObject.GetComponent<Image>();
        return toggle;
    }

    private static Text CreateLabel(string objectName, Transform parent, string value, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    // Denne funktion kaldes af Slideren
    public void SetLevel(float sliderValue)
    {
        if (musicSource != null)
        {
            musicSource.volume = sliderValue;
        }
    }
}