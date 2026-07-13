using UnityEngine;

[CreateAssetMenu(
    fileName = "New Ring",
    menuName = "Hero2D/Items/Ring")]
public class RingDefinition : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField] private int ringID;
    [SerializeField] private string ringName;

    [TextArea]
    [SerializeField] private string ringDescription;

    [Header("Item Information")]
    [SerializeField] private int requiredLevel = 1;
    [SerializeField] private RingRarity rarity;
    [SerializeField] private int value;

    [Header("Stat Bonuses")]
    [SerializeField] private float healthBonus;
    [SerializeField] private float damageBonus;
    [SerializeField] private float defenseBonus;
    [SerializeField] private float regenerationBonus;
    [Tooltip("Decimal value: 0.04 means +4% life steal.")]
    [SerializeField] private float lifeStealBonus;
    [Tooltip("Decimal value: 0.02 means +2% critical chance.")]
    [SerializeField] private float criticalChanceBonus;
    [Tooltip("Decimal value: 0.10 means +10% attack speed.")]
    [SerializeField] private float attackSpeedBonus;

    [Header("Visuals")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject prefab;

    public int RingID => ringID;
    public string RingName => ringName;
    public string RingDescription => ringDescription;
    public int RequiredLevel => requiredLevel;
    public RingRarity Rarity => rarity;
    public int Value => value;

    public float HealthBonus => healthBonus;
    public float DamageBonus => damageBonus;
    public float DefenseBonus => defenseBonus;
    public float RegenerationBonus => regenerationBonus;
    public float LifeStealBonus => lifeStealBonus;
    public float CriticalChanceBonus => criticalChanceBonus;
    public float AttackSpeedBonus => attackSpeedBonus;

    public Sprite Icon => icon;
    public GameObject Prefab => prefab;
}

public enum RingRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Unique
}
