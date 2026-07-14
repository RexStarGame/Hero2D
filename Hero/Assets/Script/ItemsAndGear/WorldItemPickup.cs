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

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory != null)
            TryPickup(inventory);
    }

    public bool TryPickup(PlayerInventory inventory)
    {
        if (inventory == null || item == null)
            return false;

        if (!inventory.Add(item))
            return false;

        if (destroyAfterPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);

        return true;
    }

    public void Initialize(ItemDefinition itemDefinition)
    {
        item = itemDefinition;
        ApplyItemIcon();
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
