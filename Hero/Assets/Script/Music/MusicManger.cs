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
        // Tjek om der trykkes på 'P'
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    void PauseGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true); // Vis menuen
            Time.timeScale = 0f;         // Stop tiden i spillet
            isGamePaused = true;
        }
    }

    public void ResumeGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false); // Skjul menuen
            Time.timeScale = 1f;          // Start tiden igen
            isGamePaused = false;
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