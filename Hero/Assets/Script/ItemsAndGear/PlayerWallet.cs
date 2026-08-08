using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [Min(0)] [SerializeField] private int startingGold;

    public int Gold { get; private set; }
    public event Action GoldChanged;
    public event Action<int> GoldAdded;

    private void Awake()
    {
        Gold = Mathf.Max(0, startingGold);
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

    public void RestoreGold(int amount)
    {
        Gold = Mathf.Max(0, amount);
        GoldChanged?.Invoke();
    }
}
