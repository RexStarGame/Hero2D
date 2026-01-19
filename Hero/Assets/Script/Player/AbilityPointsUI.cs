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

            timer = showDuration;
            blinkTimer = 0f;

            if (pressPText != null)
            {
                pressPText.text = "Tryk på P";
                pressPText.gameObject.SetActive(true);
            }
        }

        // håndtér visning + blink i 10 sekunder efter level up
        if (pressPText != null)
        {
            if (timer > 0f)
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
}
