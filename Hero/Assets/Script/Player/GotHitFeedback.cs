using UnityEngine;
using System.Collections;

public class GotHitFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;   // auto-finder hvis den sidder på samme object
    [SerializeField] private SpriteRenderer[] sprites;    // auto-finder i children hvis tom

    [Header("Flash Settings")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashDuration = 0.12f; // hvor længe den er rød

    private float lastHealth;
    private Color[] originalColors;
    private Coroutine flashRoutine;

    void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (sprites == null || sprites.Length == 0)
            sprites = GetComponentsInChildren<SpriteRenderer>(true);

        originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            originalColors[i] = sprites[i] != null ? sprites[i].color : Color.white;
    }

    void Start()
    {
        if (playerHealth != null)
            lastHealth = playerHealth.health;
    }

    void Update()
    {
        if (playerHealth == null || sprites == null || sprites.Length == 0) return;

        float currentHealth = playerHealth.health;

        // Trigger kun når HP falder
        if (currentHealth < lastHealth)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRed());
        }

        lastHealth = currentHealth;
    }

    private IEnumerator FlashRed()
    {
        SetSpritesColor(hitColor);

        // bruger realtime så det virker selv hvis Time.timeScale = 0
        yield return new WaitForSecondsRealtime(flashDuration);

        RestoreOriginalColors();
        flashRoutine = null;
    }

    private void SetSpritesColor(Color c)
    {
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = c;
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = originalColors[i];
    }

    void OnDisable()
    {
        // sikkerhed: hvis object bliver disabled midt i flash
        if (sprites != null && originalColors != null && originalColors.Length == sprites.Length)
            RestoreOriginalColors();
    }
}