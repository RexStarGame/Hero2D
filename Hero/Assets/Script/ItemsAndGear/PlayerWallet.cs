using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [Min(0)] [SerializeField] private int startingGold;
    [SerializeField] private PlayerEquipment equipment;

    private float fractionalKillGold;

    public int Gold { get; private set; }
    public event Action GoldChanged;
    public event Action<int> GoldAdded;

    public float EquipmentKillGoldBonus
    {
        get
        {
            AutoFindEquipment();
            return equipment != null ? Mathf.Max(0f, equipment.GetGoldGainBonus()) : 0f;
        }
    }

    private void Awake()
    {
        Gold = Mathf.Max(0, startingGold);
        AutoFindEquipment();
    }

    public bool CanAfford(int amount)
        => amount >= 0 && Gold >= amount;

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount))
            return false;

        Gold -= amount;
        GoldChanged?.Invoke();
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        Gold += amount;
        GoldChanged?.Invoke();
        GoldAdded?.Invoke(amount);
    }

    public int AddKillGold(int baseAmount)
    {
        if (baseAmount <= 0)
            return 0;

        float total = baseAmount * (1f + EquipmentKillGoldBonus) + fractionalKillGold;
        int awardedGold = Mathf.FloorToInt(total);
        fractionalKillGold = total - awardedGold;

        AddGold(awardedGold);
        return awardedGold;
    }

    public void RestoreGold(int amount)
    {
        Gold = Mathf.Max(0, amount);
        GoldChanged?.Invoke();
    }

    private void AutoFindEquipment()
    {
        if (equipment != null)
            return;

        equipment = GetComponent<PlayerEquipment>();
        if (equipment == null)
            equipment = GetComponentInParent<PlayerEquipment>();

#if UNITY_2023_1_OR_NEWER
        if (equipment == null)
            equipment = FindAnyObjectByType<PlayerEquipment>();
#else
        if (equipment == null)
            equipment = FindObjectOfType<PlayerEquipment>();
#endif
    }
}
