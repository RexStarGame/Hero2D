using System.Text;
using TMPro;
using UnityEngine;

public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Vector2 pointerOffset = new Vector2(18f, -18f);
    [SerializeField] private PlayerEquipment equipment;
    private readonly StringBuilder text = new StringBuilder(384);

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(ItemDefinition item, Vector2 screenPosition, bool equipped)
    {
        if (item == null)
        {
            Hide();
            return;
        }
        titleText.text = item.ItemName;
        titleText.color = RarityColor(item.Rarity);
        text.Clear();
        text.Append(item.Rarity).Append(" · ").Append(ItemTypeName(item));
        text.Append("\nRequired level ").Append(item.RequiredLevel);
        if (!string.IsNullOrWhiteSpace(item.Description)) text.Append("\n\n").Append(item.Description);
        if (item is EquippableItemDefinition gear)
        {
            text.Append("\n\n<color=#C7D2FE>STAT BONUSES</color>");
            AppendStats(gear.StatModifiers);
            text.Append("\n\nFits: ").Append(gear.EquipmentSlot);
            if (!equipped && equipment != null)
            {
                EquippableItemDefinition current = equipment.GetItem(gear.EquipmentSlot, 0);
                if (current != null && current != gear)
                {
                    text.Append("\n\n<color=#C7D2FE>COMPARED WITH ").Append(current.ItemName.ToUpperInvariant()).Append("</color>");
                    AppendComparison(gear, current);
                }
            }
        }
        if (item is WeaponDefinition weapon)
            AppendDamageRange("Base damage", weapon.MinimumBaseDamage, weapon.MaximumBaseDamage, false);
        if (item is ArmorDefinition armor && armor.ArmorRating > 0f)
            text.Append("\nArmor: <color=#22C55E>+").Append(armor.ArmorRating.ToString("0.##")).Append("</color>");
        if (item is UniqueEquipmentDefinition unique && !string.IsNullOrWhiteSpace(unique.SpecialEffectDescription))
            text.Append("\n\n<color=#FFD166>Unique: ").Append(unique.SpecialEffectDescription).Append("</color>");
        text.Append(equipped ? "\n\n<color=#60A5FA>Equipped</color> · Right-click to unequip" : "\n\nRight-click to equip · Drag to a highlighted slot");
        detailsText.text = text.ToString();
        panel.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        Move(screenPosition);
    }

    public void Move(Vector2 screenPosition)
    {
        if (!panel.gameObject.activeSelf) return;
        panel.position = screenPosition + pointerOffset;
        Vector3[] corners = new Vector3[4];
        panel.GetWorldCorners(corners);
        Vector2 correction = Vector2.zero;
        if (corners[2].x > Screen.width) correction.x -= corners[2].x - Screen.width;
        if (corners[0].x < 0f) correction.x -= corners[0].x;
        if (corners[2].y > Screen.height) correction.y -= corners[2].y - Screen.height;
        if (corners[0].y < 0f) correction.y -= corners[0].y;
        panel.position += (Vector3)correction;
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Hide()
    {
        if (panel != null) panel.gameObject.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void AppendStats(ItemStatModifiers s)
    {
        Line("Max Health", s.MaxHealth);
        AppendDamageRange("Damage", s.MinimumDamage, s.MaximumDamage, true);
        Line("Defense", s.Defense);
        Line("Regeneration", s.Regeneration, "/s");
        Line("Life Steal", s.LifeSteal * 100f, "%");
        Line("Critical Chance", s.CriticalChance * 100f, "%");
        Line("Attack Speed", s.AttackSpeed * 100f, "%");
        Line("Movement Speed", s.MovementSpeed * 100f, "%");
        Line("Kill XP", s.ExperienceGain * 100f, "%");
        Line("Kill Gold", s.GoldGain * 100f, "%");
    }

    private void Line(string label, float value, string suffix = "")
    {
        if (Mathf.Approximately(value, 0f)) return;
        string color = value > 0f ? "#22C55E" : "#EF4444";
        string sign = value > 0f ? "+" : "";
        text.Append("\n").Append(label).Append(": <color=").Append(color).Append(">")
            .Append(sign).Append(value.ToString("0.##")).Append(suffix).Append("</color>");
    }

    private void AppendComparison(EquippableItemDefinition nextItem, EquippableItemDefinition oldItem)
    {
        ItemStatModifiers next = nextItem.StatModifiers;
        ItemStatModifiers old = oldItem.StatModifiers;
        Compare("Max Health", next.MaxHealth - old.MaxHealth);
        CompareRange("Damage", next.MinimumDamage - old.MinimumDamage,
            next.MaximumDamage - old.MaximumDamage);
        Compare("Defense", next.Defense - old.Defense);
        Compare("Regeneration", next.Regeneration - old.Regeneration, "/s");
        Compare("Life Steal", (next.LifeSteal - old.LifeSteal) * 100f, "%");
        Compare("Critical Chance", (next.CriticalChance - old.CriticalChance) * 100f, "%");
        Compare("Attack Speed", (next.AttackSpeed - old.AttackSpeed) * 100f, "%");
        Compare("Movement Speed", (next.MovementSpeed - old.MovementSpeed) * 100f, "%");
        Compare("Kill XP", (next.ExperienceGain - old.ExperienceGain) * 100f, "%");
        Compare("Kill Gold", (next.GoldGain - old.GoldGain) * 100f, "%");

        float nextMinBase = nextItem is WeaponDefinition nextWeapon ? nextWeapon.MinimumBaseDamage : 0f;
        float nextMaxBase = nextItem is WeaponDefinition nextWeaponMax ? nextWeaponMax.MaximumBaseDamage : 0f;
        float oldMinBase = oldItem is WeaponDefinition oldWeapon ? oldWeapon.MinimumBaseDamage : 0f;
        float oldMaxBase = oldItem is WeaponDefinition oldWeaponMax ? oldWeaponMax.MaximumBaseDamage : 0f;
        CompareRange("Base damage", nextMinBase - oldMinBase, nextMaxBase - oldMaxBase);
    }

    private void AppendDamageRange(string label, float minimum, float maximum, bool signed)
    {
        maximum = Mathf.Max(minimum, maximum);
        if (Mathf.Approximately(minimum, 0f) && Mathf.Approximately(maximum, 0f)) return;
        string color = maximum > 0f ? "#22C55E" : "#EF4444";
        string minSign = signed && minimum > 0f ? "+" : string.Empty;
        string maxSign = signed && maximum > 0f ? "+" : string.Empty;
        text.Append("\n").Append(label).Append(": <color=").Append(color).Append(">")
            .Append(minSign).Append(minimum.ToString("0.##")).Append("–")
            .Append(maxSign).Append(maximum.ToString("0.##")).Append("</color>");
    }

    private void CompareRange(string label, float minimumDifference, float maximumDifference)
    {
        if (Mathf.Approximately(minimumDifference, 0f) && Mathf.Approximately(maximumDifference, 0f)) return;
        bool better = minimumDifference >= 0f && maximumDifference >= 0f;
        bool worse = minimumDifference <= 0f && maximumDifference <= 0f;
        string color = better ? "#22C55E" : worse ? "#EF4444" : "#F59E0B";
        string marker = better ? " ▲" : worse ? " ▼" : " ◆";
        text.Append("\n").Append(label).Append(": <color=").Append(color).Append(">")
            .Append(FormatSigned(minimumDifference)).Append("–")
            .Append(FormatSigned(maximumDifference)).Append(marker).Append("</color>");
    }

    private static string FormatSigned(float value)
        => (value > 0f ? "+" : string.Empty) + value.ToString("0.##");

    private void Compare(string label, float difference, string suffix = "")
    {
        if (Mathf.Approximately(difference, 0f)) return;
        bool better = difference > 0f;
        text.Append("\n").Append(label).Append(": <color=").Append(better ? "#22C55E" : "#EF4444").Append(">")
            .Append(better ? "+" : "").Append(difference.ToString("0.##")).Append(suffix)
            .Append(better ? " ▲" : " ▼").Append("</color>");
    }

    private static string ItemTypeName(ItemDefinition item) => item.GetType().Name.Replace("Definition", "");
    private static Color RarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon: return new Color(0.2f, 0.8f, 0.3f);
            case ItemRarity.Rare: return new Color(0.25f, 0.55f, 1f);
            case ItemRarity.Epic: return new Color(0.7f, 0.3f, 1f);
            case ItemRarity.Legendary: return new Color(1f, 0.55f, 0.1f);
            case ItemRarity.Unique: return new Color(1f, 0.82f, 0.25f);
            default: return Color.white;
        }
    }
}
