using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidRingSpawner : MonoBehaviour
{
    [Header("Asteroid Prefabs")]
    [SerializeField] GameObject[] asteroidPrefabs;

    [Header("Initial Ring")]
    [SerializeField, Min(5)] int initialCount = 15;

    [SerializeField] float minRadius = 8f;
    [SerializeField] float maxRadius = 14f;

    [SerializeField] float minHeight = -3f;
    [SerializeField] float maxHeight = 3f;

    [SerializeField, Range(0f, 30f)]
    float angleJitter = 8f;

    [Header("Intermittent Spawning")]
    [SerializeField] bool enableIntermittentSpawning = true;

    [SerializeField] float minSpawnInterval = 4f;
    [SerializeField] float maxSpawnInterval = 8f;

    [SerializeField, Min(1)]
    int maxAliveAsteroids = 24;

    [Header("Organization")]
    [SerializeField] Transform asteroidParent;

    readonly List<GameObject> spawnedAsteroids =
        new List<GameObject>();

    public IReadOnlyList<GameObject> SpawnedAsteroids =>
        spawnedAsteroids;

    void Start()
    {
        SpawnInitialRing();

        if (enableIntermittentSpawning)
        {
            StartCoroutine(
                IntermittentSpawnRoutine()
            );
        }
    }

    void SpawnInitialRing()
    {
        if (!ValidatePrefabs())
            return;

        initialCount =
            Mathf.Max(
                initialCount,
                asteroidPrefabs.Length
            );

        float angleStep =
            360f / initialCount;

        for (int i = 0; i < initialCount; i++)
        {
            float baseAngle =
                angleStep * i;

            float angle =
                baseAngle +
                Random.Range(
                    -angleJitter,
                    angleJitter
                );

            float radius =
                Random.Range(
                    minRadius,
                    maxRadius
                );

            float height =
                Random.Range(
                    minHeight,
                    maxHeight
                );

            GameObject prefab;

            // Guarantee that every asteroid family appears at least once.
            if (i < asteroidPrefabs.Length)
            {
                prefab =
                    asteroidPrefabs[i];
            }
            else
            {
                prefab =
                    GetRandomAsteroidPrefab();
            }

            SpawnAsteroid(
                prefab,
                angle,
                radius,
                height
            );
        }

        Debug.Log(
            $"[AsteroidSpawner] Initial ring spawned. " +
            $"Count = {initialCount}",
            gameObject
        );
    }

    IEnumerator IntermittentSpawnRoutine()
    {
        while (true)
        {
            float waitTime =
                Random.Range(
                    minSpawnInterval,
                    maxSpawnInterval
                );

            yield return
                new WaitForSeconds(waitTime);

            CleanupDestroyedAsteroids();

            if (
                spawnedAsteroids.Count >=
                maxAliveAsteroids
            )
            {
                // Debug.Log(
                //     $"[AsteroidSpawner] Spawn skipped. " +
                //     $"Alive asteroids = {spawnedAsteroids.Count}/" +
                //     $"{maxAliveAsteroids}",
                //     gameObject
                // );

                continue;
            }

            SpawnRandomAsteroid();
        }
    }

    void SpawnRandomAsteroid()
    {
        if (!ValidatePrefabs())
            return;

        GameObject prefab =
            GetRandomAsteroidPrefab();

        float angle =
            Random.Range(
                0f,
                360f
            );

        float radius =
            Random.Range(
                minRadius,
                maxRadius
            );

        float height =
            Random.Range(
                minHeight,
                maxHeight
            );

        GameObject spawned =
            SpawnAsteroid(
                prefab,
                angle,
                radius,
                height
            );

        if (spawned != null)
        {
            // Debug.Log(
            //     $"[AsteroidSpawner] INTERMITTENT SPAWN\n" +
            //     $"Asteroid = {spawned.name}\n" +
            //     $"Alive = {spawnedAsteroids.Count}/" +
            //     $"{maxAliveAsteroids}",
            //     spawned
            // );
        }
    }

    GameObject SpawnAsteroid(
        GameObject prefab,
        float angleDegrees,
        float radius,
        float height
    )
    {
        if (prefab == null)
            return null;

        float radians =
            angleDegrees *
            Mathf.Deg2Rad;

        Vector3 offset =
            new Vector3(
                Mathf.Cos(radians) * radius,
                height,
                Mathf.Sin(radians) * radius
            );

        Vector3 worldPosition =
            transform.position +
            offset;

        Quaternion randomRotation =
            Random.rotation;

        Transform parent =
            asteroidParent != null
                ? asteroidParent
                : transform;

        GameObject asteroid =
            Instantiate(
                prefab,
                worldPosition,
                randomRotation,
                parent
            );

        spawnedAsteroids.Add(
            asteroid
        );

        return asteroid;
    }

    GameObject GetRandomAsteroidPrefab()
    {
        return asteroidPrefabs[
            Random.Range(
                0,
                asteroidPrefabs.Length
            )
        ];
    }

    void CleanupDestroyedAsteroids()
    {
        for (
            int i =
                spawnedAsteroids.Count - 1;
            i >= 0;
            i--
        )
        {
            if (
                spawnedAsteroids[i] ==
                null
            )
            {
                spawnedAsteroids.RemoveAt(
                    i
                );
            }
        }
    }

    bool ValidatePrefabs()
    {
        if (
            asteroidPrefabs == null ||
            asteroidPrefabs.Length == 0
        )
        {
            Debug.LogError(
                "[AsteroidSpawner] No asteroid prefabs assigned.",
                gameObject
            );

            return false;
        }

        return true;
    }
}