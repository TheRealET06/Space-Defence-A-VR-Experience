using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    [SerializeField] int startingMoney = 200;
    public int Money { get; private set; }
    public event Action<int> OnMoneyChanged;

    void Awake() => Money = startingMoney;

    public bool CanAfford(int cost) => Money >= cost;

    public bool TrySpend(int cost)
    {
        if (!CanAfford(cost)) return false;
        Money -= cost;
        OnMoneyChanged?.Invoke(Money);
        return true;
    }

    public void AddMoney(int amount)
    {
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
    }
}