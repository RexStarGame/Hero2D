using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int capacity = 100;
    [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();
    [SerializeField] private PlayerEquipment equipment;
    [SerializeField] private PlayerXP playerXP;

    [Header("World Drop")]
    [SerializeField] private Vector2 dropOffset = new Vector2(0f, -1f);

    public event Action InventoryChanged;

    public IReadOnlyList<ItemDefinition> Items => items;
    public int Capacity => capacity;

    private void Awake()
    {
        if (equipment == null)
            equipment = GetComponent<PlayerEquipment>();

        if (playerXP == null)
            playerXP = GetComponent<PlayerXP>();
    }

    public ItemDefinition GetItem(int index)
        => index >= 0 && index < items.Count ? items[index] : null;

    public bool Add(ItemDefinition item)
    {
        if (item == null || items.Count >= capacity)
            return false;

        items.Add(item);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool Remove(ItemDefinition item)
    {
        bool removed = items.Remove(item);
        if (removed)
            InventoryChanged?.Invoke();

        return removed;
    }

    public bool EquipFromInventory(int index, EquipmentSlotType type, int slotNumber = 0)
    {
        EquippableItemDefinition item = GetItem(index) as EquippableItemDefinition;
        if (item == null || equipment == null || playerXP == null)
            return false;

        if (!equipment.TryEquip(
                item,
                type,
                slotNumber,
                playerXP.level,
                out EquippableItemDefinition replaced))
            return false;

        items.RemoveAt(index);

        if (replaced != null)
            items.Add(replaced);

        InventoryChanged?.Invoke();
        return true;
    }

    public bool QuickEquip(int index)
    {
        EquippableItemDefinition item = GetItem(index) as EquippableItemDefinition;
        if (item == null || equipment == null)
            return false;

        int number = 0;

        if (item.EquipmentSlot == EquipmentSlotType.Ring)
        {
            number = equipment.GetItem(EquipmentSlotType.Ring, 0) == null
                ? 0
                : equipment.GetItem(EquipmentSlotType.Ring, 1) == null
                    ? 1
                    : 0;
        }

        return EquipFromInventory(index, item.EquipmentSlot, number);
    }

    public bool UnequipToInventory(EquipmentSlotType type, int slotNumber = 0)
    {
        if (items.Count >= capacity || equipment == null)
            return false;

        EquippableItemDefinition item = equipment.Unequip(type, slotNumber);
        if (item == null)
            return false;

        items.Add(item);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool DropItemToWorld(int index)
    {
        ItemDefinition item = GetItem(index);
        if (!CanSpawnWorldItem(item))
            return false;

        SpawnWorldItem(item);
        items.RemoveAt(index);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool DropEquippedItemToWorld(EquipmentSlotType type, int slotNumber = 0)
    {
        if (equipment == null)
            return false;

        EquippableItemDefinition item = equipment.GetItem(type, slotNumber);
        if (!CanSpawnWorldItem(item))
            return false;

        EquippableItemDefinition removed = equipment.Unequip(type, slotNumber);
        if (removed == null)
            return false;

        SpawnWorldItem(removed);
        return true;
    }

    private static bool CanSpawnWorldItem(ItemDefinition item)
    {
        if (item == null)
            return false;

        if (item.WorldPrefab == null)
        {
            Debug.LogWarning($"[PlayerInventory] {item.ItemName} has no World Prefab and cannot be dropped.");
            return false;
        }

        return true;
    }

    private void SpawnWorldItem(ItemDefinition item)
    {
        Vector3 position = transform.position + (Vector3)dropOffset;
        GameObject spawnedItem = Instantiate(item.WorldPrefab, position, Quaternion.identity);

        WorldItemPickup pickup = spawnedItem.GetComponent<WorldItemPickup>();
        if (pickup == null)
            pickup = spawnedItem.GetComponentInChildren<WorldItemPickup>();

        if (pickup != null)
            pickup.Initialize(item);
        else
            Debug.LogWarning($"[PlayerInventory] {item.WorldPrefab.name} needs WorldItemPickup.", spawnedItem);
    }

    public void ReplaceContents(IEnumerable<ItemDefinition> newItems)
    {
        items.Clear();

        if (newItems != null)
            items.AddRange(newItems);

        InventoryChanged?.Invoke();
    }
}
