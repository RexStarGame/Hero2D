using UnityEngine;

public class WorldItemPickup : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemDefinition item;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool useItemIcon = true;

    [Header("Pickup")]
    [SerializeField] private bool destroyAfterPickup = true;
    [Min(0f)] [SerializeField] private float pickupDelay = 1.25f;

    [Header("Ground Lifetime")]
    [Tooltip("Seconds of active gameplay before this ground item despawns. Set to 0 to disable.")]
    [Min(0f)] [SerializeField] private float despawnAfterSeconds = 20f;

    private float pickupAvailableAt;
    private float despawnAt;
    private PlayerInventory dropOwner;
    private bool waitingForDropOwnerExit;
    private bool collected;

    public ItemDefinition Item => item;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplyItemIcon();

        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider == null)
        {
            Debug.LogWarning($"[WorldItemPickup] {name} needs a Collider2D.", this);
        }
        else if (!pickupCollider.isTrigger)
        {
            Debug.LogWarning($"[WorldItemPickup] Collider2D on {name} should have Is Trigger enabled.", this);
        }
    }

    private void OnEnable()
    {
        ResetGroundTimers();
        dropOwner = null;
        waitingForDropOwnerExit = false;
        collected = false;
    }

    private void Update()
    {
        if (despawnAfterSeconds > 0f && Time.time >= despawnAt)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryPickupFromCollider(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryPickupFromCollider(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!waitingForDropOwnerExit || dropOwner == null)
            return;

        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == dropOwner)
        {
            waitingForDropOwnerExit = false;
            dropOwner = null;
        }
    }

    private void TryPickupFromCollider(Collider2D other)
    {
        if (Time.time < pickupAvailableAt)
            return;

        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null)
            return;

        if (waitingForDropOwnerExit && inventory == dropOwner)
            return;

        TryPickup(inventory);
    }

    public bool TryPickup(PlayerInventory inventory)
    {
        if (collected || inventory == null || item == null || Time.time < pickupAvailableAt)
            return false;

        if (waitingForDropOwnerExit && inventory == dropOwner)
            return false;

        collected = true;

        if (!inventory.Add(item))
        {
            collected = false;
            return false;
        }

        if (destroyAfterPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);

        return true;
    }

    public void Initialize(ItemDefinition itemDefinition)
    {
        Initialize(itemDefinition, null);
    }

    public void Initialize(ItemDefinition itemDefinition, PlayerInventory ownerWhoDroppedIt)
    {
        item = itemDefinition;
        dropOwner = ownerWhoDroppedIt;
        waitingForDropOwnerExit = dropOwner != null;
        ResetGroundTimers();
        ApplyItemIcon();
    }

    private void ResetGroundTimers()
    {
        pickupAvailableAt = Time.time + Mathf.Max(0f, pickupDelay);
        despawnAt = despawnAfterSeconds > 0f
            ? Time.time + despawnAfterSeconds
            : float.PositiveInfinity;
    }

    private void ApplyItemIcon()
    {
        if (useItemIcon && spriteRenderer != null && item != null && item.Icon != null)
            spriteRenderer.sprite = item.Icon;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplyItemIcon();
    }

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider != null)
            pickupCollider.isTrigger = true;
    }
#endif
}
