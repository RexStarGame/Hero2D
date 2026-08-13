using UnityEngine;
[CreateAssetMenu(fileName = "New Unique Equipment", menuName = "Hero2D/Items/Unique Equipment")]
public class UniqueEquipmentDefinition : EquippableItemDefinition
{
    [TextArea] [SerializeField] private string specialEffectDescription;
    public string SpecialEffectDescription => specialEffectDescription;
}
