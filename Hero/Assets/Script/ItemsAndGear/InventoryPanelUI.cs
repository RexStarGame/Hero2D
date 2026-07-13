using System.Collections.Generic;
using UnityEngine;

public enum InventoryFilter { All, Equipment, Rings, Unique }

public class InventoryPanelUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Transform gridRoot;
    [SerializeField] private InventoryItemSlotUI slotPrefab;
    [SerializeField] private InventoryFilter filter;
    private readonly List<InventoryItemSlotUI> spawned = new List<InventoryItemSlotUI>();

    private void OnEnable() { if (inventory != null) inventory.InventoryChanged += Refresh; Refresh(); }
    private void OnDisable() { if (inventory != null) inventory.InventoryChanged -= Refresh; }
    public void SetFilter(int value) { filter = (InventoryFilter)value; Refresh(); }
    public void Refresh()
    {
        foreach (InventoryItemSlotUI slot in spawned) if (slot != null) Destroy(slot.gameObject);
        spawned.Clear();
        if (inventory == null || gridRoot == null || slotPrefab == null) return;
        for (int i = 0; i < inventory.Items.Count; i++)
        {
            ItemDefinition item = inventory.Items[i];
            if (!Matches(item)) continue;
            InventoryItemSlotUI slot = Instantiate(slotPrefab, gridRoot);
            slot.Bind(inventory, i); spawned.Add(slot);
        }
    }
    private bool Matches(ItemDefinition item)
    {
        switch (filter)
        {
            case InventoryFilter.Equipment: return item is EquippableItemDefinition;
            case InventoryFilter.Rings: return item is RingDefinition;
            case InventoryFilter.Unique: return item != null && item.Rarity == ItemRarity.Unique;
            default: return true;
        }
    }
}
