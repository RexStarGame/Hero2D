using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DifficultyMenuUI : MonoBehaviour
{
    [Header("Difficulty buttons")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private Button extremeButton;
    [SerializeField] private Button nightmareButton;

    [Header("Status and confirmation")]
    [SerializeField] private TMP_Text currentDifficultyText;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private GameDifficulty pendingDifficulty;

    private const string WarningMessage =
        "Changing difficulty changes enemy and boss maximum health, " +
        "attack damage, and normal-enemy spawn limits. Choose a harder " +
        "mode only when you are ready.";

    private void Awake()
    {
        AddButtonListener(easyButton, GameDifficulty.Easy);
        AddButtonListener(normalButton, GameDifficulty.Normal);
        AddButtonListener(hardButton, GameDifficulty.Hard);
        AddButtonListener(extremeButton, GameDifficulty.Extreme);
        AddButtonListener(nightmareButton, GameDifficulty.Nightmare);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmDifficulty);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelDifficulty);

        if (warningText != null)
            warningText.text = WarningMessage;
    }

    private void OnEnable()
    {
        DifficultyManager.DifficultyChanged += OnDifficultyChanged;
        CancelDifficulty();
        RefreshCurrentDifficulty();
    }

    private void OnDisable()
    {
        DifficultyManager.DifficultyChanged -= OnDifficultyChanged;
        CancelDifficulty();
    }

    public void RequestEasy() => RequestDifficulty(GameDifficulty.Easy);
    public void RequestNormal() => RequestDifficulty(GameDifficulty.Normal);
    public void RequestHard() => RequestDifficulty(GameDifficulty.Hard);
    public void RequestExtreme() => RequestDifficulty(GameDifficulty.Extreme);
    public void RequestNightmare() => RequestDifficulty(GameDifficulty.Nightmare);

    public void ShowMenu()
    {
        gameObject.SetActive(true);
    }

    public void HideMenu()
    {
        CancelDifficulty();
        gameObject.SetActive(false);
    }

    public void RequestDifficulty(GameDifficulty difficulty)
    {
        if (difficulty == DifficultyManager.CurrentDifficulty)
        {
            CancelDifficulty();
            return;
        }

        pendingDifficulty = difficulty;
        if (warningText != null)
        {
            warningText.text =
                $"Change difficulty to {difficulty}?\n\n{WarningMessage}";
        }

        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    public void ConfirmDifficulty()
    {
        DifficultyManager.Instance.SetDifficulty(pendingDifficulty);
        CancelDifficulty();
    }

    public void CancelDifficulty()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    public void Configure(
        Button easy,
        Button normal,
        Button hard,
        Button extreme,
        Button nightmare,
        TMP_Text current,
        GameObject confirmation,
        TMP_Text warning,
        Button confirm,
        Button cancel)
    {
        easyButton = easy;
        normalButton = normal;
        hardButton = hard;
        extremeButton = extreme;
        nightmareButton = nightmare;
        currentDifficultyText = current;
        confirmationPanel = confirmation;
        warningText = warning;
        confirmButton = confirm;
        cancelButton = cancel;
    }

    private void AddButtonListener(Button button, GameDifficulty difficulty)
    {
        if (button != null)
            button.onClick.AddListener(() => RequestDifficulty(difficulty));
    }

    private void OnDifficultyChanged(GameDifficulty difficulty)
    {
        RefreshCurrentDifficulty();
    }

    private void RefreshCurrentDifficulty()
    {
        GameDifficulty current = DifficultyManager.CurrentDifficulty;
        if (currentDifficultyText != null)
            currentDifficultyText.text = $"Current: {current}";

        SetSelected(easyButton, current == GameDifficulty.Easy);
        SetSelected(normalButton, current == GameDifficulty.Normal);
        SetSelected(hardButton, current == GameDifficulty.Hard);
        SetSelected(extremeButton, current == GameDifficulty.Extreme);
        SetSelected(nightmareButton, current == GameDifficulty.Nightmare);
    }

    private static void SetSelected(Button button, bool selected)
    {
        if (button == null || button.targetGraphic == null)
            return;

        button.targetGraphic.color = selected
            ? new Color(0.20f, 0.55f, 0.30f, 1f)
            : new Color(0.16f, 0.18f, 0.21f, 1f);
    }
}
