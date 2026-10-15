using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHealth = 100f;
    float health;

    void Awake() => health = maxHealth;

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f) Die();
    }

    void Die()
    {
        // TODO: award money, play effect, etc.
        Destroy(gameObject);
    }
}