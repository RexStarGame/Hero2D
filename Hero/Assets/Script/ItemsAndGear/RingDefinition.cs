using UnityEngine;

[CreateAssetMenu(fileName = "New Ring", menuName = "Hero2D/Items/Ring")]
public class RingDefinition : EquippableItemDefinition
{
    // Ring-specific effects can be added here later. Shared bonuses live in
    // EquippableItemDefinition so every equipment category is calculated alike.
    public float HealthBonus => StatModifiers.MaxHealth;
    public float MinimumDamageBonus => StatModifiers.MinimumDamage;
    public float MaximumDamageBonus => StatModifiers.MaximumDamage;
    public float DamageBonus => MaximumDamageBonus;
    public float DefenseBonus => StatModifiers.Defense;
    public float RegenerationBonus => StatModifiers.Regeneration;
    public float LifeStealBonus => StatModifiers.LifeSteal;
    public float CriticalChanceBonus => StatModifiers.CriticalChance;
    public float AttackSpeedBonus => StatModifiers.AttackSpeed;
    public string RingName => ItemName;
    public Sprite Icon => base.Icon;
}
