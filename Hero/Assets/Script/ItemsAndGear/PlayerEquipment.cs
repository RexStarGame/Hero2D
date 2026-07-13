using System;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    public const int RingSlotCount = 2;

    [SerializeField] private RingDefinition ringSlot1;
    [SerializeField] private RingDefinition ringSlot2;

    public event Action EquipmentChanged;

    public RingDefinition GetRing(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return null;

        return slotIndex == 0 ? ringSlot1 : ringSlot2;
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

        RingDefinition previouslyEquipped = GetRing(slotIndex);
        SetRing(slotIndex, newRing);

        EquipmentChanged?.Invoke();

        // Return this ring to the inventory when swapping.
        return previouslyEquipped;
    }

    public RingDefinition UnequipRing(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return null;

        RingDefinition removedRing = GetRing(slotIndex);

        if (removedRing == null)
            return null;

        SetRing(slotIndex, null);
        EquipmentChanged?.Invoke();

        return removedRing;
    }

    public float GetHealthBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in GetEquippedRings())
            if (ring != null)
                total += ring.HealthBonus;

        return total;
    }

    public float GetDamageBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in GetEquippedRings())
            if (ring != null)
                total += ring.DamageBonus;

        return total;
    }

    public float GetLifeStealBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in GetEquippedRings())
            if (ring != null)
                total += ring.LifeStealBonus;

        return total;
    }

    public float GetRegenerationBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in GetEquippedRings())
            if (ring != null)
                total += ring.RegenerationBonus;

        return total;
    }

    public float GetCriticalChanceBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in GetEquippedRings())
            if (ring != null)
                total += ring.CriticalChanceBonus;

        return total;
    }

    public float GetDefenseBonus()
    {
        float total = 0f;

        foreach (RingDefinition ring in GetEquippedRings())
            if (ring != null)
                total += ring.DefenseBonus;

        return total;
    }

    public float GetAttackSpeedBonus()
    {
        float total = 0f;
        foreach (RingDefinition ring in GetEquippedRings())
            if (ring != null)
                total += ring.AttackSpeedBonus;
        return total;
    }

    public void SwapRingSlots()
    {
        RingDefinition oldSlot1 = ringSlot1;
        ringSlot1 = ringSlot2;
        ringSlot2 = oldSlot1;
        EquipmentChanged?.Invoke();
    }

    private RingDefinition[] GetEquippedRings()
    {
        return new[] { ringSlot1, ringSlot2 };
    }

    private void SetRing(int slotIndex, RingDefinition ring)
    {
        if (slotIndex == 0) ringSlot1 = ring;
        else ringSlot2 = ring;
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < RingSlotCount;
    }
}
