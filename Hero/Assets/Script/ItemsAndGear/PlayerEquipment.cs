using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Serializable]
    public class EquippedSlot
    {
        public EquipmentSlotType slotType;
        public int slotNumber;
        public EquippableItemDefinition item;
    }

    [SerializeField] private List<EquippedSlot> slots = new List<EquippedSlot>();
    public event Action EquipmentChanged;
    public event Action<string, bool> EquipmentFeedback;

    private static readonly EquipmentSlotType[] SingleSlots =
    {
        EquipmentSlotType.Weapon, EquipmentSlotType.Helmet, EquipmentSlotType.Chest,
        EquipmentSlotType.Gloves, EquipmentSlotType.Boots, EquipmentSlotType.Necklace
    };

    private void Awake() => EnsureSlots();
    private void OnValidate() => EnsureSlots();

    public EquippableItemDefinition GetItem(EquipmentSlotType type, int number = 0)
    {
        EquippedSlot slot = FindSlot(type, number);
        return slot == null ? null : slot.item;
    }

    public bool TryEquip(EquippableItemDefinition item, EquipmentSlotType targetType, int targetNumber,
        int playerLevel, out EquippableItemDefinition replaced)
    {
        replaced = null;
        if (item == null || item.EquipmentSlot != targetType)
        {
            EquipmentFeedback?.Invoke("That item does not fit this slot.", false);
            return false;
        }
        if (targetType == EquipmentSlotType.Ring && (targetNumber < 0 || targetNumber > 1)) return false;
        if (targetType != EquipmentSlotType.Ring) targetNumber = 0;
        if (playerLevel < item.RequiredLevel)
        {
            EquipmentFeedback?.Invoke($"Requires level {item.RequiredLevel}.", false);
            return false;
        }

        EquippedSlot slot = FindSlot(targetType, targetNumber);
        if (slot == null) return false;
        replaced = slot.item;
        slot.item = item;
        EquipmentChanged?.Invoke();
        EquipmentFeedback?.Invoke($"{item.ItemName} equipped", true);
        return true;
    }

    public EquippableItemDefinition Unequip(EquipmentSlotType type, int number = 0)
    {
        EquippedSlot slot = FindSlot(type, type == EquipmentSlotType.Ring ? number : 0);
        if (slot == null || slot.item == null) return null;
        EquippableItemDefinition removed = slot.item;
        slot.item = null;
        EquipmentChanged?.Invoke();
        EquipmentFeedback?.Invoke($"{removed.ItemName} unequipped", true);
        return removed;
    }

    public void SwapRings()
    {
        EquippedSlot a = FindSlot(EquipmentSlotType.Ring, 0);
        EquippedSlot b = FindSlot(EquipmentSlotType.Ring, 1);
        EquippableItemDefinition old = a.item; a.item = b.item; b.item = old;
        EquipmentChanged?.Invoke();
    }

    public ItemStatModifiers GetCombinedModifiers()
    {
        ItemStatModifiers total = new ItemStatModifiers();
        foreach (EquippedSlot slot in slots)
        {
            if (slot.item == null) continue;
            total += slot.item.StatModifiers;
            if (slot.item is ArmorDefinition armor && armor.ArmorRating > 0f)
            {
                // Armor rating is displayed by its definition; shared defense bonuses belong in modifiers.
            }
        }
        return total;
    }

    public float GetHealthBonus() => GetCombinedModifiers().MaxHealth;
    public float GetMinimumDamageBonus()
    {
        float total = GetCombinedModifiers().MinimumDamage;
        foreach (EquippedSlot slot in slots)
            if (slot.item is WeaponDefinition weapon) total += weapon.MinimumBaseDamage;
        return total;
    }
    public float GetMaximumDamageBonus()
    {
        float total = GetCombinedModifiers().MaximumDamage;
        foreach (EquippedSlot slot in slots)
            if (slot.item is WeaponDefinition weapon) total += weapon.MaximumBaseDamage;
        return total;
    }
    public float GetDamageBonus() => GetMaximumDamageBonus();
    public float GetDefenseBonus()
    {
        float total = GetCombinedModifiers().Defense;
        foreach (EquippedSlot slot in slots) if (slot.item is ArmorDefinition armor) total += armor.ArmorRating;
        return total;
    }
    public float GetRegenerationBonus() => GetCombinedModifiers().Regeneration;
    public float GetLifeStealBonus() => GetCombinedModifiers().LifeSteal;
    public float GetCriticalChanceBonus() => GetCombinedModifiers().CriticalChance;
    public float GetAttackSpeedBonus() => GetCombinedModifiers().AttackSpeed;
    public float GetMovementSpeedBonus() => GetCombinedModifiers().MovementSpeed;
    public float GetExperienceGainBonus() => GetCombinedModifiers().ExperienceGain;
    public float GetGoldGainBonus() => GetCombinedModifiers().GoldGain;
    public float GetGuardChanceBonus() => GetCombinedModifiers().GuardChance;
    public float GetGuardReductionBonus() => GetCombinedModifiers().GuardReduction;

    // Compatibility while the old ring UI is being migrated.
    public RingDefinition GetRing(int index) => GetItem(EquipmentSlotType.Ring, index) as RingDefinition;
    public RingDefinition EquipRing(RingDefinition ring, int index, int level)
    {
        return TryEquip(ring, EquipmentSlotType.Ring, index, level, out EquippableItemDefinition old)
            ? old as RingDefinition : ring;
    }
    public RingDefinition UnequipRing(int index) => Unequip(EquipmentSlotType.Ring, index) as RingDefinition;
    public void SwapRingSlots() => SwapRings();

    public IReadOnlyList<EquippedSlot> Slots => slots;

    public void RestoreSlot(EquipmentSlotType type, int number, EquippableItemDefinition item)
    {
        EnsureSlots();
        EquippedSlot slot = FindSlot(type, type == EquipmentSlotType.Ring ? number : 0);
        if (slot != null && (item == null || item.EquipmentSlot == type)) slot.item = item;
    }

    public void NotifyRestored() => EquipmentChanged?.Invoke();

    private EquippedSlot FindSlot(EquipmentSlotType type, int number)
        => slots.Find(s => s.slotType == type && s.slotNumber == number);

    private void EnsureSlots()
    {
        if (slots == null) slots = new List<EquippedSlot>();
        foreach (EquipmentSlotType type in SingleSlots) EnsureSlot(type, 0);
        EnsureSlot(EquipmentSlotType.Ring, 0);
        EnsureSlot(EquipmentSlotType.Ring, 1);
        slots.RemoveAll(s => s == null || s.slotType == EquipmentSlotType.None ||
            (s.slotType == EquipmentSlotType.Ring && (s.slotNumber < 0 || s.slotNumber > 1)) ||
            (s.slotType != EquipmentSlotType.Ring && s.slotNumber != 0));
    }

    private void EnsureSlot(EquipmentSlotType type, int number)
    {
        if (FindSlot(type, number) == null)
            slots.Add(new EquippedSlot { slotType = type, slotNumber = number });
    }
}
