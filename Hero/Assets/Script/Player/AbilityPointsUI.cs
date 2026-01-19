using UnityEngine;
using TMPro;

public class AbilityPointsUI : MonoBehaviour
{
    public PlayerXP player;
    public TMP_Text pointsText;
    public TMP_Text pressPText;

    public float showDuration = 10f;     // hvor længe teksten vises
    public float blinkInterval = 0.5f;   // hvor hurtigt den blinker

    int lastLevel;
    float timer = 0f;
    float blinkTimer = 0f;

    bool dismissed = false;              // <-- NY

    void Start()
    {
        if (player != null) lastLevel = player.level;

        if (pressPText != null)
            pressPText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        // ability points tekst
        if (pointsText != null)
            pointsText.text = "Ability Points: " + player.abilityPoints;

        // tjek om spilleren lige har fået et level
        if (player.level != lastLevel)
        {
            lastLevel = player.level;

            dismissed = false;           // <-- NY (reset ved level up)
            timer = showDuration;
            blinkTimer = 0f;

            if (pressPText != null)
            {
                pressPText.text = "Tryk på P";
                pressPText.enabled = true;       // <-- så den starter synlig
                pressPText.gameObject.SetActive(true);
            }
        }

        if (pressPText == null) return;

        // <-- NY: hvis man trykker P mens den vises, stop blink og skjul
        if (!dismissed && timer > 0f && Input.GetKeyDown(KeyCode.P))
        {
            dismissed = true;
            timer = 0f;
            pressPText.enabled = true;           // reset så den ikke ender usynlig
            pressPText.gameObject.SetActive(false);
            return;
        }

        // håndtér visning + blink i 10 sekunder efter level up (kun hvis ikke dismissed)
        if (!dismissed && timer > 0f)
        {
            timer -= Time.deltaTime;

            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                pressPText.enabled = !pressPText.enabled; // blink
            }

            if (timer <= 0f)
            {
                pressPText.enabled = true; // reset så den ikke ender “usynlig”
                pressPText.gameObject.SetActive(false);
            }
        }
        else
        {
            // hvis ingen countdown er aktiv, så er den skjult
            if (pressPText.gameObject.activeSelf)
                pressPText.gameObject.SetActive(false);
        }
    }
}
