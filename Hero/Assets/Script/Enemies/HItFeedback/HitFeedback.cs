using UnityEngine;
using System.Collections; // Rettet her!

public class HitFeedback : MonoBehaviour
{
    [Header("Indstillinger")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void PlayHitFeedback()
    {
        if (spriteRenderer == null || !gameObject.activeInHierarchy) return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Skift til rød
        spriteRenderer.color = flashColor;

        // Vent i de sekunder du har sat i Inspectoren
        yield return new WaitForSeconds(flashDuration);

        // Skift tilbage til den originale farve
        spriteRenderer.color = originalColor;

        flashCoroutine = null;
    }
}