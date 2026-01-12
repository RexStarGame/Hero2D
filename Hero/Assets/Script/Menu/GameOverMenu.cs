using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour
{
    [Header("UI Paneler")]
    public GameObject gameOverPanel;

    [Header("Døds Effekter")]
    public GameObject deathParticlePrefab; // Træk din partikel-effekt herind

    private bool isGameOver = false;

    void Start()
    {
        gameOverPanel.SetActive(false);
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // 1. Spawn partikler der hvor spilleren dør
            if (deathParticlePrefab != null)
            {
                Instantiate(deathParticlePrefab, player.transform.position, Quaternion.identity);
            }

            // 2. Skjul spilleren (I stedet for SetActive(false), så koden stadig kan køre færdig)
            if (player.GetComponent<SpriteRenderer>() != null)
                player.GetComponent<SpriteRenderer>().enabled = false;

            if (player.GetComponent<Collider2D>() != null)
                player.GetComponent<Collider2D>().enabled = false;

            // 3. Deaktiver styring
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;
        }

        // Vis menuen efter et lille delay eller med det samme
        Invoke("ShowMenu", 0.5f); // 0.5 sekunder delay så man ser eksplosionen
    }

    void ShowMenu()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0.2f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        // 1. Nulstil tiden med det samme
        Time.timeScale = 1f;

        // 2. Skjul panelet med det samme (valgfrit, da LoadScene gør det for dig)
        gameOverPanel.SetActive(false);

        // 3. Genindlæs banen
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // load current scene igen
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // loader en hel ny scene
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}