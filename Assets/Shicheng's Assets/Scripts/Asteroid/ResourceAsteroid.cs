using UnityEngine;

public class ResourceAsteroid : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] AsteroidFamily family;
    [SerializeField] bool isChunk = false;

    [Header("Yield")]
    [Range(1, 64)]
    [SerializeField] int barrelYield = 4;

    [Header("Mining")]
    [SerializeField] float maxIntegrity = 100f;

    [Header("Shatter")]
    [SerializeField] GameObject shatterResultPrefab;

    float currentIntegrity;

    public AsteroidFamily Family => family;
    public bool IsChunk => isChunk;

    public int BarrelYield => barrelYield;

    public float Integrity => currentIntegrity;
    public float MaxIntegrity => maxIntegrity;

    public int PriorityValue => barrelYield;

    void Awake()
    {
        currentIntegrity = maxIntegrity;
    }

    public ResourceAsteroid ApplyMiningHit(float damage)
    {
        currentIntegrity =
            Mathf.Max(
                0f,
                currentIntegrity - damage
            );

        Debug.Log(
            $"[Asteroid] HIT\n" +
            $"Object = {name}\n" +
            $"Integrity = {currentIntegrity:F1}/{maxIntegrity:F1}",
            gameObject
        );

        if (currentIntegrity > 0f)
            return null;

        if (isChunk)
        {
            Debug.Log(
                $"[Asteroid] Chunk depleted: {name}",
                gameObject
            );

            Destroy(gameObject);
            return null;
        }

        return Shatter();
    }

    ResourceAsteroid Shatter()
    {
        ResourceAsteroid spawnedChunk = null;

        if (shatterResultPrefab != null)
        {
            GameObject chunkObject =
                Instantiate(
                    shatterResultPrefab,
                    transform.position,
                    transform.rotation,
                    transform.parent
                );

            spawnedChunk =
                chunkObject.GetComponent<ResourceAsteroid>();

            if (spawnedChunk == null)
            {
                Debug.LogError(
                    $"[Asteroid] Shatter prefab {shatterResultPrefab.name} " +
                    $"does not contain ResourceAsteroid.",
                    chunkObject
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"[Asteroid] {name} has no shatter result prefab.",
                gameObject
            );
        }

        Debug.Log(
            $"[Asteroid] SHATTERED\n" +
            $"Original = {name}\n" +
            $"Chunk = {(spawnedChunk != null ? spawnedChunk.name : "None")}",
            gameObject
        );

        Destroy(gameObject);

        return spawnedChunk;
    }

    public void SetCaptured(bool captured)
    {
        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = !captured;
        }

        Rigidbody[] rigidbodies =
            GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody body in rigidbodies)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = captured;
        }
    }
}