using UnityEngine;
using System.Collections;

public class BossAttackAnimation : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Triggers (must match Animator exactly)")]
    [SerializeField] private string idleTrigger = "Idle";
    [SerializeField] private string attackTrigger = "Attack";

    [Header("Optional fallback")]
    [Tooltip("Hvis du vil tvinge tilbage til Idle efter X sekunder (brug kun hvis din Attack-state ellers ikke går tilbage).")]
    [SerializeField] private bool forceIdleAfterDelay = false;
    [SerializeField] private float idleDelaySeconds = 0.6f;

    private Coroutine idleRoutine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void PlayAttack()
    {
        if (animator == null) return;

        animator.ResetTrigger(idleTrigger);
        animator.SetTrigger(attackTrigger);

        if (idleRoutine != null) StopCoroutine(idleRoutine);

        if (forceIdleAfterDelay && idleDelaySeconds > 0f)
            idleRoutine = StartCoroutine(ReturnToIdleAfter(idleDelaySeconds));
    }

    public void PlayIdle()
    {
        if (animator == null) return;

        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }

        animator.ResetTrigger(attackTrigger);
        animator.SetTrigger(idleTrigger);
    }

    private IEnumerator ReturnToIdleAfter(float t)
    {
        yield return new WaitForSeconds(t);
        PlayIdle();
    }
}
