using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class DamageUpgrade : MonoBehaviour
{
    [Header("Damage Stats")]
    [Tooltip("Default player damage at level 0.")]
    [SerializeField] private int damage = 10;              // <-- DEFAULT DAMAGE (what you want)
    [SerializeField] private int damageLevel = 0;
    [SerializeField] private int maxDamageLevel = 10;
    [SerializeField] private int damagePerLevel = 5;

    [Header("Upgrade Cost")]
    [SerializeField] private int cost = 1;
    [SerializeField] private PlayerXP playerXP;
    [SerializeField] private PlayerEquipment equipment;

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;

    [Header("Events (optional)")]
    public UnityEvent<int> onDamageChanged; // sends new Damage value

    // ✅ This is the REAL damage used by the player
    public int BaseAndAbilityDamage => damage + (damageLevel * damagePerLevel);
    public int EquipmentDamageBonus
    {
        get
        {
            // This component currently lives under an upgrade-menu UI object,
            // which can be inactive at startup. Resolve gameplay references on
            // demand so equipped-item damage never depends on opening that UI.
            ResolveReferences();
            return equipment == null ? 0 : Mathf.RoundToInt(equipment.GetDamageBonus());
        }
    }
    public int Damage => BaseAndAbilityDamage + EquipmentDamageBonus;

    public int DamageLevel => damageLevel;

    private void Awake()
    {
        ResolveReferences();

        UpdateLevelText();
        onDamageChanged?.Invoke(Damage);
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (equipment != null)
        {
            equipment.EquipmentChanged -= HandleEquipmentChanged;
            equipment.EquipmentChanged += HandleEquipmentChanged;
        }
    }

    private void OnDisable()
    {
        if (equipment != null)
            equipment.EquipmentChanged -= HandleEquipmentChanged;
    }

    private void ResolveReferences()
    {
        if (playerXP == null)
        {
#if UNITY_2023_1_OR_NEWER
            playerXP = FindAnyObjectByType<PlayerXP>();
#else
            playerXP = FindObjectOfType<PlayerXP>();
#endif
        }

        if (equipment != null) return;

        // DamageUpgrade currently lives on a UI object, while equipment lives
        // on Player. PlayerXP is already a reliable reference to that Player.
        if (playerXP != null)
            equipment = playerXP.GetComponent<PlayerEquipment>();

        if (equipment != null) return;

#if UNITY_2023_1_OR_NEWER
        equipment = FindAnyObjectByType<PlayerEquipment>();
#else
        equipment = FindObjectOfType<PlayerEquipment>();
#endif
    }

    private void HandleEquipmentChanged()
    {
        UpdateLevelText();
        onDamageChanged?.Invoke(Damage);
    }

    // Called by UI Button
    public void BuyDamage()
    {
        if (playerXP == null)
        {
            Debug.LogError("[DamageUpgrade] PlayerXP not found. Assign it in Inspector.");
            return;
        }

        if (damageLevel >= maxDamageLevel)
        {
            Debug.Log("[DamageUpgrade] Damage is maxed out!");
            return;
        }

        if (playerXP.abilityPoints < cost)
        {
            Debug.Log("[DamageUpgrade] Not enough ability points!");
            return;
        }

        playerXP.abilityPoints -= cost;
        damageLevel++;

        Debug.Log($"[DamageUpgrade] Level={damageLevel} Damage={Damage} (object: {gameObject.name})");

        UpdateLevelText();
        onDamageChanged?.Invoke(Damage);
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
            levelText.text = "+"; // ✅ LIVE damage display
    }
}
