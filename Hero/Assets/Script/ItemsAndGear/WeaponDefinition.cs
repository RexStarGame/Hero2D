using UnityEngine;

public enum WeaponType { Sword, Axe, Dagger, Staff, Bow, Other }

[CreateAssetMenu(fileName = "New Weapon", menuName = "Hero2D/Items/Weapon")]
public class WeaponDefinition : EquippableItemDefinition
{
    [Header("Weapon")]
    [SerializeField] private WeaponType weaponType;
    [Min(0f)] [SerializeField] private float baseDamage;
    [Min(0f)] [SerializeField] private float attackRange = 1f;
    public WeaponType WeaponType => weaponType;
    public float BaseDamage => baseDamage;
    public float AttackRange => attackRange;
}
