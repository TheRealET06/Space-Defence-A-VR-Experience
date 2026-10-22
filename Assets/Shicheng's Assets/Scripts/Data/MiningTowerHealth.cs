using System;
using UnityEngine;

public class MiningTowerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float currentHealth = 100f;

    [Header("Shield")]
    [SerializeField] float maxShield;
    [SerializeField] float currentShield;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    public float MaxShield => maxShield;
    public float CurrentShield => currentShield;

    public event Action OnHealthChanged;

    void Awake()
    {
        currentHealth =
            Mathf.Clamp(currentHealth, 0f, maxHealth);

        currentShield =
            Mathf.Clamp(currentShield, 0f, maxShield);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
            return;

        float remainingDamage = amount;

        if (currentShield > 0f)
        {
            float absorbed =
                Mathf.Min(currentShield, remainingDamage);

            currentShield -= absorbed;
            remainingDamage -= absorbed;
        }

        if (remainingDamage > 0f)
        {
            currentHealth =
                Mathf.Max(
                    0f,
                    currentHealth - remainingDamage
                );
        }

        OnHealthChanged?.Invoke();
    }

    public void Repair(float amount)
    {
        if (amount <= 0f)
            return;

        currentHealth =
            Mathf.Min(
                maxHealth,
                currentHealth + amount
            );

        OnHealthChanged?.Invoke();
    }

    public void AddShieldCapacity(float amount)
    {
        if (amount <= 0f)
            return;

        maxShield += amount;
        currentShield = maxShield;

        OnHealthChanged?.Invoke();
    }
}