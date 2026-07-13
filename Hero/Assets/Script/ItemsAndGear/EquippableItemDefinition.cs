using UnityEngine;

public abstract class EquippableItemDefinition : ItemDefinition
{
    [Header("Equipment")]
    [SerializeField] private EquipmentSlotType equipmentSlot;
    [SerializeField] private ItemStatModifiers statModifiers = new ItemStatModifiers();

    public EquipmentSlotType EquipmentSlot => equipmentSlot;
    public ItemStatModifiers StatModifiers => statModifiers;
}
