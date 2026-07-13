using System;
using System.Collections.Generic;
using UnityEngine;

public class RingInventory : MonoBehaviour
{
    [SerializeField] private List<RingDefinition> rings = new List<RingDefinition>();
    [SerializeField] private PlayerEquipment equipment;
    [SerializeField] private PlayerXP playerXP;

    public event Action InventoryChanged;
    public int Count => rings.Count;

    private void Awake()
    {
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();
        if (playerXP == null) playerXP = GetComponent<PlayerXP>();
    }

    public RingDefinition GetRing(int index) => index >= 0 && index < rings.Count ? rings[index] : null;

    public void AddRing(RingDefinition ring)
    {
        if (ring == null) return;
        rings.Add(ring);
        InventoryChanged?.Invoke();
    }

    public bool EquipFromInventory(int inventoryIndex, int equipmentSlot)
    {
        RingDefinition ring = GetRing(inventoryIndex);
        if (ring == null || equipment == null || playerXP == null) return false;
        if (playerXP.level < ring.RequiredLevel) return false;

        rings.RemoveAt(inventoryIndex);
        RingDefinition replaced = equipment.EquipRing(ring, equipmentSlot, playerXP.level);
        if (replaced != null) rings.Add(replaced);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool UnequipToInventory(int equipmentSlot)
    {
        if (equipment == null) return false;
        RingDefinition ring = equipment.UnequipRing(equipmentSlot);
        if (ring == null) return false;
        rings.Add(ring);
        InventoryChanged?.Invoke();
        return true;
    }
}
