using System;
using UnityEngine;

namespace Brian
{
    public class Home : MonoBehaviour
    {
        public static Home Instance { get; private set; }
        public static event Action<Home> Spawned;

        [SerializeField] int maxHealth = 20;

        public int MaxHealth => maxHealth;
        public int Health { get; private set; }
        public float Fraction => maxHealth > 0 ? (float)Health / maxHealth : 0f;

        public event Action<int, int> HealthChanged;

        void Awake()
        {
            Instance = this;
            Health = maxHealth;
            Spawned?.Invoke(this);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Damage(int amount)
        {
            if (Health <= 0 || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            HealthChanged?.Invoke(Health, maxHealth);
            if (Health == 0 && GameManager.Instance != null) GameManager.Instance.Lose();
        }
    }
}
