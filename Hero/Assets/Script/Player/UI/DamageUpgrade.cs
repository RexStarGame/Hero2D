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

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;

    [Header("Events (optional)")]
    public UnityEvent<int> onDamageChanged; // sends new Damage value

    // ✅ This is the REAL damage used by the player
    public int Damage => damage + (damageLevel * damagePerLevel);

    public int DamageLevel => damageLevel;

    private void Awake()
    {
        if (playerXP == null)
        {
#if UNITY_2023_1_OR_NEWER
            playerXP = FindAnyObjectByType<PlayerXP>();
#else
            playerXP = FindObjectOfType<PlayerXP>();
#endif
        }

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
