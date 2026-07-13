using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int capacity = 100;
    [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();
    [SerializeField] private PlayerEquipment equipment;
    [SerializeField] private PlayerXP playerXP;
    public event Action InventoryChanged;
    public IReadOnlyList<ItemDefinition> Items => items;
    public int Capacity => capacity;

    private void Awake()
    {
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();
        if (playerXP == null) playerXP = GetComponent<PlayerXP>();
    }

    public ItemDefinition GetItem(int index) => index >= 0 && index < items.Count ? items[index] : null;
    public bool Add(ItemDefinition item)
    {
        if (item == null || items.Count >= capacity) return false;
        items.Add(item); InventoryChanged?.Invoke(); return true;
    }
    public bool Remove(ItemDefinition item)
    {
        bool removed = items.Remove(item);
        if (removed) InventoryChanged?.Invoke();
        return removed;
    }

    public bool EquipFromInventory(int index, EquipmentSlotType type, int slotNumber = 0)
    {
        EquippableItemDefinition item = GetItem(index) as EquippableItemDefinition;
        if (item == null || equipment == null || playerXP == null) return false;
        if (!equipment.TryEquip(item, type, slotNumber, playerXP.level, out EquippableItemDefinition replaced)) return false;
        items.RemoveAt(index);
        if (replaced != null) items.Add(replaced);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool QuickEquip(int index)
    {
        EquippableItemDefinition item = GetItem(index) as EquippableItemDefinition;
        if (item == null) return false;
        int number = 0;
        if (item.EquipmentSlot == EquipmentSlotType.Ring)
            number = equipment.GetItem(EquipmentSlotType.Ring, 0) == null ? 0 :
                (equipment.GetItem(EquipmentSlotType.Ring, 1) == null ? 1 : 0);
        return EquipFromInventory(index, item.EquipmentSlot, number);
    }

    public bool UnequipToInventory(EquipmentSlotType type, int slotNumber = 0)
    {
        if (items.Count >= capacity || equipment == null) return false;
        EquippableItemDefinition item = equipment.Unequip(type, slotNumber);
        if (item == null) return false;
        items.Add(item); InventoryChanged?.Invoke(); return true;
    }

    public void ReplaceContents(IEnumerable<ItemDefinition> newItems)
    {
        items.Clear();
        if (newItems != null) items.AddRange(newItems);
        InventoryChanged?.Invoke();
    }
}
