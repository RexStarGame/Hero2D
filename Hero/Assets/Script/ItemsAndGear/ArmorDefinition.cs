using UnityEngine;
public abstract class ArmorDefinition : EquippableItemDefinition
{
    [Header("Armor")] [Min(0f)] [SerializeField] private float armorRating;
    public float ArmorRating => armorRating;
}
