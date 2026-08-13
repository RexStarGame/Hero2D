using System;
using UnityEngine;

[Serializable]
public class ItemStatModifiers
{
    [SerializeField] private float maxHealth;
    [SerializeField] private float damage;
    [SerializeField] private float defense;
    [SerializeField] private float regeneration;
    [Tooltip("Decimal: 0.04 means +4%.")] [SerializeField] private float lifeSteal;
    [Tooltip("Decimal: 0.02 means +2%.")] [SerializeField] private float criticalChance;
    [Tooltip("Decimal: 0.10 means +10%.")] [SerializeField] private float attackSpeed;
    [Tooltip("Decimal: 0.10 means +10%.")] [SerializeField] private float movementSpeed;
    [Tooltip("Decimal: 0.10 means +10% XP from kills while this item is equipped.")]
    [Min(0f)] [SerializeField] private float experienceGain;

    public float MaxHealth => maxHealth;
    public float Damage => damage;
    public float Defense => defense;
    public float Regeneration => regeneration;
    public float LifeSteal => lifeSteal;
    public float CriticalChance => criticalChance;
    public float AttackSpeed => attackSpeed;
    public float MovementSpeed => movementSpeed;
    public float ExperienceGain => experienceGain;

    public static ItemStatModifiers operator +(ItemStatModifiers a, ItemStatModifiers b)
    {
        return new ItemStatModifiers
        {
            maxHealth = a.maxHealth + b.maxHealth,
            damage = a.damage + b.damage,
            defense = a.defense + b.defense,
            regeneration = a.regeneration + b.regeneration,
            lifeSteal = a.lifeSteal + b.lifeSteal,
            criticalChance = a.criticalChance + b.criticalChance,
            attackSpeed = a.attackSpeed + b.attackSpeed,
            movementSpeed = a.movementSpeed + b.movementSpeed,
            experienceGain = a.experienceGain + b.experienceGain
        };
    }
}
