using UnityEngine;
using UnityEngine.UI;

public class AttackCooldownIndicatorUI : MonoBehaviour
{
    [SerializeField] private PlayerAttack playerAttack;

    [Header("UI")]
    [SerializeField] private Image cooldownFill;      // Image type = Filled (Radial 360)
    [SerializeField] private GameObject readyIcon;    // fx et lille ikon der vises når klar

    [Header("Optional feedback")]
    [SerializeField] private AudioSource readySfx;    // spiller lyd når den bliver klar (valgfrit)

    private bool wasReady;

    private void Awake()
    {
        if (playerAttack == null)
            playerAttack = FindFirstObjectByType<PlayerAttack>();
    }

    private void Update()
    {
        if (playerAttack == null) return;

        bool ready = playerAttack.CanAttack;

        if (cooldownFill != null)
            cooldownFill.fillAmount = ready ? 1f : playerAttack.Cooldown01;

        if (readyIcon != null)
            readyIcon.SetActive(ready);

        // Kun trig lyd én gang når den går fra "ikke klar" -> "klar"
        if (!wasReady && ready && readySfx != null)
            readySfx.Play();

        wasReady = ready;
    }
}
