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
    }

    // Update køres hver frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                // Block opening this menu while another local menu owns input
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
            isGamePaused = true;

            MenuLock.Set(MenuOwner.Pause);
        }
    }

    public void ResumeGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
            isGamePaused = false;

            MenuLock.Clear(MenuOwner.Pause);
        }
    }

    private void OnDisable()
    {
        if (isGamePaused)
        {
            MenuLock.Clear(MenuOwner.Pause);
        }
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
