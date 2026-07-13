using UnityEngine;

[System.Serializable]
public class Rings
{
    [Header("Basic information")]
    [SerializeField] private int ringID;
    [SerializeField] private string ringName;
    [TextArea]
    [SerializeField] private string ringDescription;

    [Header("Item information")]
    [SerializeField] private int requiredLevel;
    [SerializeField] private RingRarity rarity;
    [SerializeField] private int value;

    [Header("Stat bonuses")]
    [SerializeField] private int healthBonus;
    [SerializeField] private int damageBonus;
    [SerializeField] private int defenseBonus;

    [Header("Visuals")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject prefab;
}

public enum RingRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}