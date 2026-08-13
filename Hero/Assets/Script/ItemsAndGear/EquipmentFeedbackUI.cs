using System.Collections;
using TMPro;
using UnityEngine;

public class EquipmentFeedbackUI : MonoBehaviour
{
    [SerializeField] private PlayerEquipment equipment;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip equipSound;
    [SerializeField] private AudioClip invalidSound;
    [SerializeField] private float visibleTime = 1.2f;
    private Coroutine routine;
    private void OnEnable() { if (equipment != null) equipment.EquipmentFeedback += Show; if (group != null) group.alpha = 0f; }
    private void OnDisable() { if (equipment != null) equipment.EquipmentFeedback -= Show; }
    private void Show(string message, bool success)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(message, success));
    }
    private IEnumerator ShowRoutine(string message, bool success)
    {
        messageText.text = message;
        messageText.color = success ? new Color(0.75f, 0.9f, 1f) : new Color(1f, 0.45f, 0.4f);
        group.alpha = 1f;
        if (audioSource != null) audioSource.PlayOneShot(success ? equipSound : invalidSound);
        yield return new WaitForSecondsRealtime(visibleTime);
        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / 0.2f; yield return null; }
        group.alpha = 0f; routine = null;
    }
}
