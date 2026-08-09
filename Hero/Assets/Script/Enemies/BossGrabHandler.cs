using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossGrabHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform grabPoint;
    [SerializeField] private string playerTag = "Player";

    [Header("Grab Trigger Zone (optional extra filter)")]
    [Tooltip("Hvis du bruger GrabZone2D, sætter den denne bool. Grab kræver stadig distance-check.")]
    [SerializeField] private bool requireGrabZone = false;

    [Header("Grab Charge + Cooldown (BALANCE)")]
    [Tooltip("Hvor lang tid bossen charger før den forsøger grab.")]
    [SerializeField] private float grabWindupTime = 0.75f;

    [Tooltip("Cooldown efter grab-attempt (uanset om den rammer).")]
    [SerializeField] private float grabCooldown = 2.0f;

    [Tooltip("Radius omkring boss/anchor som tænder charge og som grab skal ramme indenfor ved attempt.")]
    [SerializeField] private float grabRadius = 1.6f;

    [Tooltip("Ekstra sikkerhedsafstand til selve grab (hvis du vil skelne). Ofte = grabRadius.")]
    [SerializeField] private float maxGrabDistance = 1.8f;

    [Header("Grab Warning Visual (always follows boss)")]
    [Tooltip("Visual-only ring prefab. Kan være samme prefab som AOE telegraph.")]
    [SerializeField] private GameObject grabWarningPrefab;

    [Tooltip("Ringens center. Sæt til boss transform eller en hånd/anchor.")]
    [SerializeField] private Transform warningAnchor;

    [SerializeField] private int warningSortingOrder = 999;
    [SerializeField] private int warningLineSegments = 64;

    [Tooltip("Hvis prefab har TelegraphPulse2D (fra AOE), disable den så den ikke overskriver farver her.")]
    [SerializeField] private bool disableTelegraphPulseOnGrabRing = true;

    [Header("Ring Colors")]
    [SerializeField] private Color readyColor = new Color(0.2f, 1f, 0.35f, 0.75f);
    [SerializeField] private Color cooldownColor = new Color(0.4f, 0.6f, 1f, 0.45f);
    [SerializeField] private Color chargeSoonColor = new Color(1f, 0.9f, 0.2f, 0.75f);
    [SerializeField] private Color chargeImminentColor = new Color(1f, 0.2f, 0.2f, 0.9f);

    [Tooltip("Hvor stor del af charge-tiden der blinkes hårdt til sidst (0.35 = sidste 35%).")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float chargeBlinkStartFraction = 0.35f;

    [SerializeField] private float chargeBlinkHz = 10f;

    [Header("Grab Hold + Throw")]
    [SerializeField] private float holdTime = 0.5f;
    [SerializeField] private float damage = 20f;
    private EnemyDifficultyProfile difficultyProfile;
    [SerializeField] private float throwForce = 12f;

    [Range(0f, 45f)]
    [SerializeField] private float randomAngleDegrees = 10f;

    [SerializeField] private float stunAfterThrow = 0.25f;
    [SerializeField] private bool forceDynamicForThrow = true;

    [Header("Disable player control while held/stunned")]
    [SerializeField] private MonoBehaviour[] playerScriptsToDisable;

    [Tooltip("Auto-disable PlayerMovement.")]
    [SerializeField] private bool autoDisablePlayerMovement = true;

    [Tooltip("Auto-disable PlayerAttack.")]
    [SerializeField] private bool autoDisablePlayerAttack = true;

    [SerializeField] private bool logDebug = false;

    private Transform player;
    private Rigidbody2D playerRb;
    private BossBehaviorController bossController;

    private bool playerInGrabZone;
    private bool grabbing;
    private Coroutine grabRoutine;
    private readonly Dictionary<MonoBehaviour, bool> disabledScriptStates =
        new Dictionary<MonoBehaviour, bool>();

    public bool IsRunning => grabbing || charging;

    // auto-cached scripts
    private MonoBehaviour cachedMovement;
    private MonoBehaviour cachedAttack;

    // physics cache for grab
    private RigidbodyType2D oldBodyType;
    private float oldGravity;
    private bool oldSimulated;
    private Transform oldParent;
    private Vector3 savedWorldScale;

    // charge/cooldown state
    private bool charging;
    private float chargeStartTime;
    private float nextReadyTime; // cooldown end timestamp

    // ring instance
    private GameObject ring;
    private SpriteRenderer ringSR;
    private LineRenderer ringLR;
    private Material ringMat;
    private float lastDrawnRadius = float.NaN;
    private int lastDrawnSegments = -1;
    private static readonly int ColorProp = Shader.PropertyToID("_Color");
    private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        difficultyProfile = GetComponentInParent<EnemyDifficultyProfile>();
        bossController = GetComponentInParent<BossBehaviorController>();
        if (bossController == null)
            bossController = GetComponentInChildren<BossBehaviorController>(true);

        if (warningAnchor == null)
            warningAnchor = (grabPoint != null) ? grabPoint : transform;

        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) CachePlayer(p.transform);

        // Spawn ring immediately (so you can always see cooldown/ready status)
        SpawnOrEnsureRing();
        nextReadyTime = Time.time; // ready at start
    }

    private void Update()
    {
        if (grabbing) return;

        if (bossController != null && bossController.IsAttacking)
        {
            charging = false;
            return;
        }

        EnsurePlayer();

        SpawnOrEnsureRing();
        UpdateRingTransform();

        // Update ring visuals based on state
        UpdateRingVisuals();

        if (player == null) return;

        // If currently cooling down, do nothing except show ring progress
        if (Time.time < nextReadyTime)
            return;

        // If not charging yet, check if player is close to start charging
        if (!charging)
        {
            if (ShouldStartChargeNow())
            {
                charging = true;
                chargeStartTime = Time.time;
                if (logDebug) Debug.Log("[Grab] Charge START");
            }
            return;
        }

        // Charging continues even if player runs away
        float t = (Time.time - chargeStartTime) / Mathf.Max(0.01f, grabWindupTime);
        if (t >= 1f)
        {
            charging = false;

            // Attempt grab ONLY if player is close at the moment the charge completes
            bool canGrabNow = IsPlayerCloseRightNow();

            if (logDebug) Debug.Log("[Grab] Charge COMPLETE. canGrabNow=" + canGrabNow);

            if (canGrabNow)
                BeginGrab();

            // Cooldown starts regardless (balance)
            nextReadyTime = Time.time + grabCooldown;
        }
    }

    // Called from GrabZone2D
    public void SetPlayerInGrabZone(Collider2D col, bool inZone)
    {
        if (col != null && col.CompareTag(playerTag))
        {
            CachePlayer(col.transform);
            playerInGrabZone = inZone;
        }
    }

    private void EnsurePlayer()
    {
        if (player != null) return;

        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) CachePlayer(p.transform);
    }

    private void CachePlayer(Transform t)
    {
        player = t;
        playerRb = t.GetComponent<Rigidbody2D>();

        if (autoDisablePlayerMovement && cachedMovement == null)
        {
            cachedMovement = t.GetComponent<PlayerMovement>();
        }

        if (autoDisablePlayerAttack && cachedAttack == null)
        {
            cachedAttack = t.GetComponent<PlayerAttack>();
        }
    }

    private bool ShouldStartChargeNow()
    {
        if (requireGrabZone && !playerInGrabZone)
            return false;

        return IsPlayerWithinRadius(grabRadius);
    }

    private bool IsPlayerCloseRightNow()
    {
        if (requireGrabZone && !playerInGrabZone)
            return false;

        // Must be within maxGrabDistance at the moment of attempt
        return IsPlayerWithinRadius(maxGrabDistance);
    }

    private bool IsPlayerWithinRadius(float r)
    {
        if (player == null || warningAnchor == null) return false;
        float dist = Vector2.Distance((Vector2)warningAnchor.position, (Vector2)player.position);
        return dist <= r;
    }

    private void BeginGrab()
    {
        if (grabbing || grabRoutine != null) return;
        if (bossController != null && bossController.IsAttacking) return;

        TryBeginGrab();
    }

    /// <summary>
    /// Starts a grab immediately when the target and required references are valid.
    /// Used by BossBehaviorController so both autonomous and controller-driven grabs
    /// share one state and one cleanup path.
    /// </summary>
    public bool TryBeginGrab()
    {
        EnsurePlayer();

        if (grabbing || grabRoutine != null || grabPoint == null || player == null)
            return false;

        if (!IsPlayerCloseRightNow())
            return false;

        grabRoutine = StartCoroutine(GrabRoutine(player));
        return true;
    }

    private IEnumerator GrabRoutine(Transform grabbedPlayer)
    {
        grabbing = true;
        charging = false;
        Rigidbody2D grabbedRb = grabbedPlayer != null
            ? grabbedPlayer.GetComponent<Rigidbody2D>()
            : null;
        bool completedThrow = false;

        try
        {
            CaptureAndDisablePlayerControls(grabbedPlayer);

            oldParent = grabbedPlayer.parent;
            savedWorldScale = grabbedPlayer.lossyScale;

            if (grabbedRb != null)
            {
                oldBodyType = grabbedRb.bodyType;
                oldGravity = grabbedRb.gravityScale;
                oldSimulated = grabbedRb.simulated;

                SetVelocity(grabbedRb, Vector2.zero);
                grabbedRb.angularVelocity = 0f;
                grabbedRb.simulated = true;
                grabbedRb.bodyType = RigidbodyType2D.Kinematic;
                grabbedRb.gravityScale = 0f;
            }

            grabbedPlayer.SetParent(grabPoint, worldPositionStays: true);
            grabbedPlayer.position = grabPoint.position;
            RestoreWorldScale(grabbedPlayer, savedWorldScale);

            yield return new WaitForSeconds(Mathf.Max(0f, holdTime));

            if (grabbedPlayer == null)
                yield break;

            grabbedPlayer.SetParent(oldParent, worldPositionStays: true);
            RestoreWorldScale(grabbedPlayer, savedWorldScale);

            if (difficultyProfile == null)
                difficultyProfile = GetComponentInParent<EnemyDifficultyProfile>();

            float scaledDamage = difficultyProfile != null
                ? difficultyProfile.ScaleDamage(damage)
                : damage * EnemyDifficultyProfile.GetDefaultDamageMultiplier();

            DifficultyDebugTelemetry.RecordEnemyDamage(this, damage, scaledDamage);

            PlayerHealth health = grabbedPlayer.GetComponentInParent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(scaledDamage);
            else
                grabbedPlayer.gameObject.SendMessage(
                    "TakeDamage", scaledDamage, SendMessageOptions.DontRequireReceiver);

            if (grabbedPlayer == null)
                yield break;

            if (grabbedRb != null)
            {
                grabbedRb.simulated = true;
                grabbedRb.bodyType = forceDynamicForThrow
                    ? RigidbodyType2D.Dynamic
                    : oldBodyType;

                if (grabbedRb.bodyType == RigidbodyType2D.Dynamic)
                    grabbedRb.gravityScale = oldGravity;

                SetVelocity(grabbedRb, Vector2.zero);

                Vector2 direction =
                    ((Vector2)grabbedPlayer.position - (Vector2)transform.position).normalized;
                if (direction.sqrMagnitude < 0.001f)
                    direction = transform.right;

                if (randomAngleDegrees > 0f)
                {
                    float angle = Random.Range(-randomAngleDegrees, randomAngleDegrees);
                    direction = Quaternion.Euler(0f, 0f, angle) * direction;
                }

                grabbedRb.AddForce(direction * Mathf.Max(0f, throwForce), ForceMode2D.Impulse);
                completedThrow = true;

                PlayerMovement movement = grabbedPlayer.GetComponent<PlayerMovement>();
                if (movement != null)
                    movement.PreserveExternalVelocity(Mathf.Max(0.1f, stunAfterThrow + 0.1f));
            }

            if (stunAfterThrow > 0f)
                yield return new WaitForSeconds(stunAfterThrow);
        }
        finally
        {
            CleanupGrab(grabbedPlayer, grabbedRb, completedThrow);
        }
    }

    private void CaptureAndDisablePlayerControls(Transform grabbedPlayer)
    {
        disabledScriptStates.Clear();

        if (playerScriptsToDisable != null)
        {
            for (int i = 0; i < playerScriptsToDisable.Length; i++)
                RememberAndDisable(playerScriptsToDisable[i]);
        }

        if (grabbedPlayer != null)
        {
            if (autoDisablePlayerMovement)
                cachedMovement = grabbedPlayer.GetComponent<PlayerMovement>();
            if (autoDisablePlayerAttack)
                cachedAttack = grabbedPlayer.GetComponent<PlayerAttack>();
        }

        if (autoDisablePlayerMovement)
            RememberAndDisable(cachedMovement);
        if (autoDisablePlayerAttack)
            RememberAndDisable(cachedAttack);
    }

    private void RememberAndDisable(MonoBehaviour behaviour)
    {
        if (behaviour == null || disabledScriptStates.ContainsKey(behaviour))
            return;

        disabledScriptStates.Add(behaviour, behaviour.enabled);
        behaviour.enabled = false;

        if (logDebug)
            Debug.Log($"[Grab] DISABLED {behaviour.GetType().Name}");
    }

    private void RestorePlayerControls()
    {
        foreach (KeyValuePair<MonoBehaviour, bool> entry in disabledScriptStates)
        {
            if (entry.Key == null) continue;
            entry.Key.enabled = entry.Value;

            if (logDebug)
                Debug.Log($"[Grab] RESTORED {entry.Key.GetType().Name} to {entry.Value}");
        }

        disabledScriptStates.Clear();
    }

    private void CleanupGrab(
        Transform grabbedPlayer,
        Rigidbody2D grabbedRb,
        bool preserveThrowVelocity)
    {
        if (grabbedPlayer != null)
        {
            if (grabbedPlayer.parent == grabPoint)
                grabbedPlayer.SetParent(oldParent, worldPositionStays: true);

            RestoreWorldScale(grabbedPlayer, savedWorldScale);
        }

        if (grabbedRb != null)
        {
            Vector2 velocity = GetVelocity(grabbedRb);
            float angularVelocity = grabbedRb.angularVelocity;

            grabbedRb.simulated = oldSimulated;
            grabbedRb.bodyType = oldBodyType;
            grabbedRb.gravityScale = oldGravity;

            if (preserveThrowVelocity && oldSimulated)
            {
                SetVelocity(grabbedRb, velocity);
                grabbedRb.angularVelocity = angularVelocity;
            }
        }

        RestorePlayerControls();
        grabbing = false;
        grabRoutine = null;
    }

    private static Vector2 GetVelocity(Rigidbody2D body)
    {
#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }

    private static void SetVelocity(Rigidbody2D body, Vector2 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = velocity;
#else
        body.velocity = velocity;
#endif
    }

    // ---------- Ring Visuals ----------

    private void SpawnOrEnsureRing()
    {
        if (ring != null) return;
        if (grabWarningPrefab == null) return;

        ring = Instantiate(grabWarningPrefab, warningAnchor.position, Quaternion.identity);
        ring.transform.SetParent(warningAnchor, worldPositionStays: true);
        ring.transform.localPosition = Vector3.zero;

        // Get renderers
        ringSR = ring.GetComponentInChildren<SpriteRenderer>(true);
        ringLR = ring.GetComponentInChildren<LineRenderer>(true);

        if (disableTelegraphPulseOnGrabRing)
        {
            var pulse = ring.GetComponentInChildren<TelegraphPulse2D>(true);
            if (pulse != null) pulse.enabled = false;
        }

        if (ringSR != null) ringSR.sortingOrder = warningSortingOrder;

        if (ringLR != null)
        {
            ringLR.sortingOrder = warningSortingOrder;
            ringLR.useWorldSpace = false;

            // Ensure we draw a circle if prefab is line-based
            RedrawRingIfNeeded();

            // Avoid scaling affecting line points
            ring.transform.localScale = Vector3.one;

            ringMat = ringLR.material;
        }
        else
        {
            // Sprite ring: scale to diameter
            float d = grabRadius * 2f;
            ring.transform.localScale = new Vector3(d, d, 1f);
        }
    }

    private void UpdateRingTransform()
    {
        if (ring == null) return;

        ring.transform.localPosition = Vector3.zero;

        // If sprite ring, keep scale synced to grabRadius
        if (ringLR == null)
        {
            float d = grabRadius * 2f;
            ring.transform.localScale = new Vector3(d, d, 1f);
        }
        else
        {
            RedrawRingIfNeeded();
        }
    }

    private void UpdateRingVisuals()
    {
        if (ring == null) return;

        // cooldown progress (0..1)
        float cooldownT = 1f;
        if (Time.time < nextReadyTime)
            cooldownT = 1f - ((nextReadyTime - Time.time) / Mathf.Max(0.01f, grabCooldown));

        if (Time.time < nextReadyTime)
        {
            // COOLDOWN: color shows progress by alpha
            Color c = cooldownColor;
            c.a = Mathf.Lerp(0.25f, cooldownColor.a, cooldownT);
            ApplyRingColor(c);
            return;
        }

        if (!charging)
        {
            // READY
            ApplyRingColor(readyColor);
            return;
        }

        // CHARGING (blink near the end)
        float t = Mathf.Clamp01((Time.time - chargeStartTime) / Mathf.Max(0.01f, grabWindupTime));
        float blinkStartT = 1f - chargeBlinkStartFraction;

        if (t < blinkStartT)
        {
            // early charge: steady "soon" color, alpha ramps slightly
            Color c = chargeSoonColor;
            c.a = Mathf.Lerp(0.35f, chargeSoonColor.a, t / Mathf.Max(0.0001f, blinkStartT));
            ApplyRingColor(c);
            return;
        }

        // late charge: blink between soon and imminent
        float s = (Mathf.Sin(Time.time * Mathf.PI * 2f * chargeBlinkHz) + 1f) * 0.5f;
        Color blink = Color.Lerp(chargeSoonColor, chargeImminentColor, s);
        ApplyRingColor(blink);
    }

    private void ApplyRingColor(Color c)
    {
        if (ringSR != null) ringSR.color = c;

        if (ringLR != null)
        {
            ringLR.startColor = c;
            ringLR.endColor = c;

            if (ringMat != null)
            {
                if (ringMat.HasProperty(ColorProp)) ringMat.SetColor(ColorProp, c);
                else if (ringMat.HasProperty(BaseColorProp)) ringMat.SetColor(BaseColorProp, c);
            }
        }
    }

    private void RedrawRingIfNeeded()
    {
        if (ringLR == null)
            return;

        int segments = Mathf.Max(8, warningLineSegments);
        if (Mathf.Approximately(lastDrawnRadius, grabRadius) && lastDrawnSegments == segments)
            return;

        DrawCircleOnLineRenderer(ringLR, grabRadius, segments);
        lastDrawnRadius = grabRadius;
        lastDrawnSegments = segments;
    }

    private void OnEnable()
    {
        if (ring != null)
            ring.SetActive(true);
    }

    private void OnDisable()
    {
        charging = false;

        if (grabRoutine != null)
        {
            StopCoroutine(grabRoutine);
            grabRoutine = null;
        }

        CleanupGrab(player, playerRb, false);

        if (ring != null)
            ring.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ringMat != null)
        {
            Destroy(ringMat);
            ringMat = null;
        }
    }

    private static void DrawCircleOnLineRenderer(LineRenderer lr, float radius, int segments)
    {
        int seg = Mathf.Max(8, segments);
        lr.positionCount = seg + 1;

        for (int i = 0; i <= seg; i++)
        {
            float t = (float)i / seg;
            float ang = t * Mathf.PI * 2f;
            float x = Mathf.Cos(ang) * radius;
            float y = Mathf.Sin(ang) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    // ---------- Scale fix ----------
    private static void RestoreWorldScale(Transform t, Vector3 desiredWorldScale)
    {
        Transform parent = t.parent;
        if (parent == null)
        {
            t.localScale = desiredWorldScale;
            return;
        }

        Vector3 parentWorldScale = parent.lossyScale;

        float x = parentWorldScale.x != 0f ? desiredWorldScale.x / parentWorldScale.x : desiredWorldScale.x;
        float y = parentWorldScale.y != 0f ? desiredWorldScale.y / parentWorldScale.y : desiredWorldScale.y;
        float z = parentWorldScale.z != 0f ? desiredWorldScale.z / parentWorldScale.z : desiredWorldScale.z;

        t.localScale = new Vector3(x, y, z);
    }
}
