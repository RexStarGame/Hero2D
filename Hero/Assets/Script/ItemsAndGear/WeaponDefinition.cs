using UnityEngine;
using UnityEngine.Serialization;

public enum WeaponType { Sword, Axe, Dagger, Staff, Bow, Other }

[CreateAssetMenu(fileName = "New Weapon", menuName = "Hero2D/Items/Weapon")]
public class WeaponDefinition : EquippableItemDefinition, ISerializationCallbackReceiver
{
    [Header("Weapon")]
    [SerializeField] private WeaponType weaponType;
    [FormerlySerializedAs("baseDamage")]
    [Min(0f)] [SerializeField] private float minimumBaseDamage;
    [Min(0f)] [SerializeField] private float maximumBaseDamage;
    [Min(0f)] [SerializeField] private float attackRange = 1f;
    public WeaponType WeaponType => weaponType;
    public float MinimumBaseDamage => minimumBaseDamage;
    public float MaximumBaseDamage => Mathf.Max(minimumBaseDamage, maximumBaseDamage);
    public float BaseDamage => MaximumBaseDamage;
    public float AttackRange => attackRange;

    public void OnBeforeSerialize()
    {
        if (maximumBaseDamage < minimumBaseDamage) maximumBaseDamage = minimumBaseDamage;
    }

    public void OnAfterDeserialize()
    {
        if (maximumBaseDamage < minimumBaseDamage) maximumBaseDamage = minimumBaseDamage;
    }
}
