using UnityEngine;

public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary, Unique }
public enum EquipmentSlotType { None, Weapon, Helmet, Chest, Gloves, Boots, Necklace, Ring }

public abstract class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;
    [TextArea] [SerializeField] private string description;
    [SerializeField] private ItemRarity rarity;
    [Min(1)] [SerializeField] private int requiredLevel = 1;
    [Min(0)] [SerializeField] private int goldValue;
    [Header("Visuals")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject worldPrefab;

    public string ItemID => itemID;
    public string ItemName => itemName;
    public string Description => description;
    public ItemRarity Rarity => rarity;
    public int RequiredLevel => requiredLevel;
    public int GoldValue => goldValue;
    public Sprite Icon => icon;
    public GameObject WorldPrefab => worldPrefab;
}
