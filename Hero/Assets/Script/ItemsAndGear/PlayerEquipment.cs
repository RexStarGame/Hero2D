using System;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    public const int RingSlotCount = 2;

    [SerializeField]
    private RingDefinition[] equippedRings =
        new RingDefinition[RingSlotCount];

    public event Action EquipmentChanged;

    public RingDefinition GetRing(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return null;

        return equippedRings[slotIndex];
    }

    public RingDefinition EquipRing(
        RingDefinition newRing,
        int slotIndex,
        int playerLevel)
    {
        if (newRing == null || !IsValidSlot(slotIndex))
            return newRing;

        if (playerLevel < newRing.RequiredLevel)
        {
            Debug.Log($"Requires level {newRing.RequiredLevel}.");
            return newRing;
        }

        RingDefinition previouslyEquipped = equippedRings[slotIndex];
        equippedRings[slotIndex] = newRing;

        EquipmentChanged?.Invoke();

        // Return this ring to the inventory when swapping.
        return previouslyEquipped;
    }

    public RingDefinition UnequipRing(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return null;

        RingDefinition removedRing = equippedRings[slotIndex];

        if (removedRing == null)
            return null;

        equippedRings[slotIndex] = null;
        EquipmentChanged?.Invoke();

        return removedRing;
    }

    public float GetHealthBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in equippedRings)
            if (ring != null)
                total += ring.HealthBonus;

        return total;
    }

    public float GetDamageBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in equippedRings)
            if (ring != null)
                total += ring.DamageBonus;

        return total;
    }

    public float GetLifeStealBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in equippedRings)
            if (ring != null)
                total += ring.LifeStealBonus;

        return total;
    }

    public float GetRegenerationBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in equippedRings)
            if (ring != null)
                total += ring.RegenerationBonus;

        return total;
    }

    public float GetCriticalChanceBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in equippedRings)
            if (ring != null)
                total += ring.CriticalChanceBonus;

        return total;
    }

    public float GetDefenseBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in equippedRings)
            if (ring != null)
                total += ring.DefenseBonus;

        return total;
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < RingSlotCount;
    }
}