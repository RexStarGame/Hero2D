using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum InventoryFilter { All, Equipment, Rings, Unique }

public class InventoryPanelUI : MonoBehaviour, IDropHandler
{
    public static InventoryPanelUI Instance { get; private set; }

    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Transform gridRoot;
    [SerializeField] private InventoryItemSlotUI slotPrefab;
    [SerializeField] private InventoryFilter filter;

    [Header("Grid Layout")]
    [Min(1)] [SerializeField] private int columns = 8;
    [Min(1)] [SerializeField] private int visibleRows = 6;
    [SerializeField] private Vector2 spacing = Vector2.one;

    private readonly List<InventoryItemSlotUI> spawned = new List<InventoryItemSlotUI>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Instance = this;
        if (inventory != null) inventory.InventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.InventoryChanged -= Refresh;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool IsScreenPointInside(Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform panel = transform as RectTransform;
        return panel != null &&
            RectTransformUtility.RectangleContainsScreenPoint(panel, screenPosition, eventCamera);
    }

    public void OnDrop(PointerEventData eventData)
    {
        EquipmentSlotUI source = ItemDragContext.EquipmentSource;
        if (source == null || inventory == null) return;

        if (inventory.UnequipToInventory(source.SlotType, source.SlotNumber))
            ItemDragContext.MarkDropHandled();
    }

    public void SetFilter(int value)
    {
        filter = (InventoryFilter)value;
        Refresh();
    }

    public void Refresh()
    {
        foreach (InventoryItemSlotUI slot in spawned)
            if (slot != null) Destroy(slot.gameObject);

        spawned.Clear();

        if (inventory == null || gridRoot == null || slotPrefab == null)
            return;

        int displayIndex = 0;

        for (int i = 0; i < inventory.Items.Count; i++)
        {
            ItemDefinition item = inventory.Items[i];
            if (!Matches(item)) continue;

            InventoryItemSlotUI slot = Instantiate(slotPrefab, gridRoot);
            slot.Bind(inventory, i);
            PositionSlot(slot, displayIndex);
            spawned.Add(slot);
            displayIndex++;
        }
    }

    private void PositionSlot(InventoryItemSlotUI slot, int displayIndex)
    {
        RectTransform gridRect = gridRoot as RectTransform;
        RectTransform slotRect = slot.transform as RectTransform;

        if (gridRect == null || slotRect == null)
            return;

        int safeColumns = Mathf.Max(1, columns);
        int safeRows = Mathf.Max(1, visibleRows);

        float cellWidth = Mathf.Max(
            1f,
            (gridRect.rect.width - spacing.x * (safeColumns - 1)) / safeColumns);

        float cellHeight = Mathf.Max(
            1f,
            (gridRect.rect.height - spacing.y * (safeRows - 1)) / safeRows);

        int column = displayIndex % safeColumns;
        int row = displayIndex / safeColumns;

        slotRect.anchorMin = new Vector2(0f, 1f);
        slotRect.anchorMax = new Vector2(0f, 1f);
        slotRect.pivot = new Vector2(0f, 1f);
        slotRect.sizeDelta = new Vector2(cellWidth, cellHeight);
        slotRect.anchoredPosition = new Vector2(
            column * (cellWidth + spacing.x),
            -row * (cellHeight + spacing.y));
        slotRect.localScale = Vector3.one;
    }

    private bool Matches(ItemDefinition item)
    {
        switch (filter)
        {
            case InventoryFilter.Equipment:
                return item is EquippableItemDefinition;
            case InventoryFilter.Rings:
                return item is RingDefinition;
            case InventoryFilter.Unique:
                return item != null && item.Rarity == ItemRarity.Unique;
            default:
                return true;
        }
    }
}
