using UnityEngine;
using UnityEngine.Events;

public class Enemy : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    private float health;

    [Header("Targeting")]
    [SerializeField] private Transform aimPoint;
    public float danger = 10f;

    [Header("Events")]
    public UnityEvent onDeath;          // fires once on death (award money, effects, etc.)

    public bool IsDead = false;
    public Vector3 AimPoint => aimPoint != null ? aimPoint.position : transform.position + Vector3.up;

    private float sustained_dmg = 0f;   // damage per second from damage-over-time effects

    void Awake() => health = maxHealth;

    void Update()
    {
        if (IsDead) return;

        if (sustained_dmg != 0f)
            TakeDamage(sustained_dmg * Time.deltaTime);
    }

    /// <summary>Instant damage.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        health -= amount;
        if (health <= 0f) Die();
    }

    /// <summary>Alias kept for the laser tower (original Enemystats API).</summary>
    public void Hurt(float dmg) => TakeDamage(dmg);

    /// <summary>Adds damage-per-second that is applied every frame (original Enemystats API).</summary>
    public void ticiking_hurt(float dmg) { sustained_dmg += dmg; }

    void Die()
    {
        if (IsDead) return;             // guard against double death / double rewards
        IsDead = true;

        // TODO: play effect, etc. (award money can be hooked up via myEvent)
        onDeath.Invoke();
        Destroy(gameObject);
    }
}