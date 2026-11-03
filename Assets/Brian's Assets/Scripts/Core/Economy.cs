using System;
using UnityEngine;

namespace Brian
{
    public class Economy : MonoBehaviour
    {
        public static Economy Instance { get; private set; }

        [SerializeField] int startingMoney = 100;
        [SerializeField] int startingGems = 0;
        [SerializeField] int startingGas = 0;

        public int Money { get; private set; }
        public int Gems { get; private set; }
        public int Gas { get; private set; }

        public event Action<int> MoneyChanged;
        public event Action<int> GemsChanged;
        public event Action<int> GasChanged;

        void Awake()
        {
            Instance = this;
            Money = startingMoney;
            Gems = startingGems;
            Gas = startingGas;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---- Money ----

        public bool CanAfford(int cost) => Money >= cost;

        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost)) return false;
            Money -= cost;
            MoneyChanged?.Invoke(Money);
            return true;
        }

        public void Earn(int amount)
        {
            if (amount <= 0) return;
            Money += amount;
            MoneyChanged?.Invoke(Money);
        }

        // ---- Gems (separate from money) ----

        public bool CanAffordGems(int cost) => Gems >= cost;

        public bool TrySpendGems(int cost)
        {
            if (!CanAffordGems(cost)) return false;
            Gems -= cost;
            GemsChanged?.Invoke(Gems);
            return true;
        }

        public void EarnGems(int amount)
        {
            if (amount <= 0) return;
            Gems += amount;
            GemsChanged?.Invoke(Gems);
        }

        // ---- Gas (separate from money and gems) ----

        public bool CanAffordGas(int cost) => Gas >= cost;

        public bool TrySpendGas(int cost)
        {
            if (!CanAffordGas(cost)) return false;
            Gas -= cost;
            GasChanged?.Invoke(Gas);
            return true;
        }

        public void EarnGas(int amount)
        {
            if (amount <= 0) return;
            Gas += amount;
            GasChanged?.Invoke(Gas);
        }
    }
}