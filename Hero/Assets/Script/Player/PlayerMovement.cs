using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private PlayerEquipment equipment;

    [Header("Position Save")]
    [Tooltip("How often the player's position is checked for an autosave.")]
    [Min(0.5f)]
    [SerializeField] private float positionAutosaveInterval = 3f;

    [Tooltip("Minimum distance the player must move from the last saved point before a periodic autosave writes again.")]
    [Min(0f)]
    [SerializeField] private float positionSaveDistanceThreshold = 0.05f;

    public float EquipmentMovementSpeedBonus => equipment == null ? 0f : Mathf.Max(-0.9f, equipment.GetMovementSpeedBonus());
    public float EffectiveMoveSpeed => moveSpeed * (1f + EquipmentMovementSpeedBonus);

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    private SpriteRenderer sr;
    private float preserveExternalVelocityUntil;
    private float positionAutosaveTimer;
    private Vector2 lastSavedPosition;
    private bool hasLastSavedPosition;

    /// <summary>
    /// Prevents player input from immediately overwriting an externally applied
    /// Rigidbody velocity, for example after a boss throw or knockback.
    /// </summary>
    public void PreserveExternalVelocity(float duration)
    {
        preserveExternalVelocityUntil = Mathf.Max(
            preserveExternalVelocityUntil,
            Time.time + Mathf.Max(0f, duration));
        movement = Vector2.zero;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        MinimapTarget2D.Ensure(gameObject, MinimapTargetKind.Player);
        RestoreSavedPosition();
    }

    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }


    void Update()
    {
        if (MenuLock.IsGameplayInputBlocked)
        {
            movement = Vector2.zero;
            if (animator != null)
                animator.SetBool("IsMoving", false);
            return;
        }

        // Read input
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // --- FLIP LOGIC (LEFT / RIGHT) ---
        if (movement.x > 0.01f)
        {
            sr.flipX = false; // looking right
        }
        else if (movement.x < -0.01f)
        {
            sr.flipX = true; // looking left
        }

        if (movement.sqrMagnitude > 0)
        {
            movement = movement.normalized;
        }

        // --- WALK / IDLE ANIMATION ---
        bool isMoving = movement.sqrMagnitude > 0.01f;

        // Don't allow walk animation while attacking
        if (!animator.GetBool("IsAttackingBool"))
        {
            animator.SetBool("IsMoving", isMoving);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }


        // --- UP / DOWN DIRECTION ---
        if (movement.y > 0.01f)
        {
            animator.SetBool("IsFacingUp", true);
        }
        else if (movement.y < -0.01f)
        {
            animator.SetBool("IsFacingUp", false);
        }
    }

    private void LateUpdate()
    {
        if (rb == null)
            return;

        positionAutosaveTimer += Time.unscaledDeltaTime;
        if (positionAutosaveTimer < Mathf.Max(0.5f, positionAutosaveInterval))
            return;

        positionAutosaveTimer = 0f;
        SavePositionIfChanged();
    }


    void FixedUpdate()
    {
        if (Time.time < preserveExternalVelocityUntil)
            return;

        if (MenuLock.IsGameplayInputBlocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = movement * EffectiveMoveSpeed;
    }

    private void RestoreSavedPosition()
    {
        if (rb == null)
            return;

        if (PlayerProgressSave.TryRestorePlayerPosition(out Vector2 savedPosition))
        {
            rb.position = savedPosition;
            rb.linearVelocity = Vector2.zero;
            movement = Vector2.zero;
        }

        lastSavedPosition = rb.position;
        hasLastSavedPosition = true;
    }

    private void SavePositionIfChanged()
    {
        if (rb == null)
            return;

        Vector2 currentPosition = rb.position;
        float threshold = Mathf.Max(0f, positionSaveDistanceThreshold);
        float thresholdSquared = threshold * threshold;

        if (hasLastSavedPosition &&
            (currentPosition - lastSavedPosition).sqrMagnitude <= thresholdSquared)
        {
            return;
        }

        SavePosition(currentPosition);
    }

    private void SavePositionNow()
    {
        if (rb != null)
            SavePosition(rb.position);
    }

    private void SavePosition(Vector2 position)
    {
        PlayerProgressSave.SavePlayerPosition(position);
        lastSavedPosition = position;
        hasLastSavedPosition = true;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SavePositionNow();
    }

    private void OnApplicationQuit()
    {
        SavePositionNow();
    }

    private void OnValidate()
    {
        positionAutosaveInterval = Mathf.Max(0.5f, positionAutosaveInterval);
        positionSaveDistanceThreshold = Mathf.Max(0f, positionSaveDistanceThreshold);
    }
}
