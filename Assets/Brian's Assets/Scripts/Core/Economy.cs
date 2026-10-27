using System;
using UnityEngine;

namespace Brian
{
    public class Economy : MonoBehaviour
    {
        public static Economy Instance { get; private set; }

        [SerializeField] int startingMoney = 100;

        public int Money { get; private set; }

        public event Action<int> MoneyChanged;

        void Awake()
        {
            Instance = this;
            Money = startingMoney;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

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
    }
}
