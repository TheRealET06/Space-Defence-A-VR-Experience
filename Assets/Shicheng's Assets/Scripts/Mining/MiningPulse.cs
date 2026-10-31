using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MiningPulse : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float speed = 15f;
    [SerializeField] float lifetime = 4f;

    ResourceAsteroid target;
    MiningTowerController owner;

    Rigidbody rb;

    float damage;
    float age;

    public float SpeedMultiplier { get; set; } = 1f;
    bool resolved;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;
    }

    public void Initialize(
        ResourceAsteroid newTarget,
        MiningTowerController newOwner,
        float newDamage
    )
    {
        target = newTarget;
        owner = newOwner;
        damage = newDamage;

        Debug.Log(
            $"[MiningPulse] Fired at " +
            $"{(target != null ? target.name : "None")}",
            gameObject
        );
    }

    void Update()
    {
        age += Time.deltaTime;

        if (age >= lifetime)
        {
            FailPulse(
                "Projectile lifetime expired."
            );
        }
    }

    void FixedUpdate()
    {
        if (resolved)
            return;

        if (target == null)
        {
            FailPulse(
                "Target was lost before impact."
            );

            return;
        }

        Vector3 direction =
            target.transform.position -
            transform.position;

        float step =
            speed * SpeedMultiplier * Time.fixedDeltaTime;

        if (direction.magnitude <= step + 0.1f)
        {
            Hit(target);
            return;
        }

        Vector3 normalizedDirection =
            direction.normalized;

        rb.linearVelocity =
            normalizedDirection * speed * SpeedMultiplier;

        Quaternion desiredRotation =
            Quaternion.LookRotation(
                normalizedDirection,
                Vector3.up
            );

        rb.MoveRotation(
            desiredRotation
        );
    }

    void OnCollisionEnter(Collision collision)
    {
        if (resolved)
            return;

        ResourceAsteroid asteroid =
            collision.collider
                .GetComponentInParent<ResourceAsteroid>();

        if (asteroid == null)
            return;

        if (asteroid != target)
        {
            Debug.LogWarning(
                $"[MiningPulse] Hit wrong asteroid: {asteroid.name}",
                gameObject
            );

            return;
        }

        Hit(asteroid);
    }

    void Hit(ResourceAsteroid asteroid)
    {
        resolved = true;

        Debug.Log(
            $"[MiningPulse] COLLISION\n" +
            $"Projectile = {name}\n" +
            $"Asteroid = {asteroid.name}",
            gameObject
        );

        ResourceAsteroid spawnedChunk =
            asteroid.ApplyMiningHit(
                damage
            );

        if (owner != null)
        {
            owner.OnPulseHit(
                spawnedChunk
            );
        }

        Destroy(gameObject);
    }

    void FailPulse(string reason)
    {
        if (resolved)
            return;

        resolved = true;

        Debug.LogWarning(
            $"[MiningPulse] FAILED\n" +
            $"Reason = {reason}",
            gameObject
        );

        if (owner != null)
        {
            owner.OnPulseFailed(
                reason
            );
        }

        Destroy(gameObject);
    }
}