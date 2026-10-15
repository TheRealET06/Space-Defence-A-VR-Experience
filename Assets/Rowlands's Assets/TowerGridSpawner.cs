using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// Spawns objects on a TowerGrid at game start: first at predefined cells,
/// then at random free cells. Marks each used cell as occupied on the grid.
public class TowerGridSpawner : MonoBehaviour
{
    [Serializable]
    public struct PredefinedSpawn
    {
        public Vector3Int cell;      // x = column, y = layer, z = row
        public GameObject prefab;    // leave empty to use the default prefab
    }

    [Header("References")]
    [SerializeField] TowerGrid grid;
    [SerializeField] GameObject defaultPrefab;
    [SerializeField] Transform spawnParent;          // optional; defaults to the grid

    [Header("Predefined spawns")]
    [SerializeField] List<PredefinedSpawn> predefinedSpawns = new List<PredefinedSpawn>();

    [Header("Random spawns")]
    [SerializeField] int randomCount = 5;
    [SerializeField] GameObject[] randomPrefabs;     // empty = use default prefab
    [SerializeField] bool limitRandomToLayer = false;
    [SerializeField] int randomLayer = 0;            // used if limitRandomToLayer is true

    [Header("Seed")]
    [SerializeField] bool useFixedSeed = false;
    [SerializeField] int seed = 12345;

    readonly List<GameObject> spawned = new List<GameObject>();
    public IReadOnlyList<GameObject> Spawned => spawned;

    // Start (not Awake) so TowerGrid.Awake has already built its points.
    void Start()
    {
        if (grid == null) grid = FindAnyObjectByType<TowerGrid>();
        if (grid == null)
        {
            Debug.LogError("TowerGridSpawner: no TowerGrid assigned or found.", this);
            return;
        }

        if (useFixedSeed) Random.InitState(seed);

        SpawnPredefined();
        SpawnRandom();
    }

    void SpawnPredefined()
    {
        foreach (var s in predefinedSpawns)
        {
            GameObject prefab = s.prefab != null ? s.prefab : defaultPrefab;
            if (!TrySpawn(s.cell, prefab))
                Debug.LogWarning($"TowerGridSpawner: could not spawn at {s.cell} (outside grid, occupied, or no prefab).", this);
        }
    }

    void SpawnRandom()
    {
        if (randomCount <= 0) return;

        // Collect every free cell, shuffle, and take the first N (no duplicates).
        var free = new List<Vector3Int>();
        for (int y = 0; y < grid.Layers; y++)
        {
            if (limitRandomToLayer && y != randomLayer) continue;
            for (int x = 0; x < grid.Columns; x++)
                for (int z = 0; z < grid.Rows; z++)
                {
                    var c = new Vector3Int(x, y, z);
                    if (!grid.IsOccupied(c)) free.Add(c);
                }
        }

        for (int i = free.Count - 1; i > 0; i--)   // Fisher-Yates
        {
            int j = Random.Range(0, i + 1);
            (free[i], free[j]) = (free[j], free[i]);
        }

        int count = Mathf.Min(randomCount, free.Count);
        if (count < randomCount)
            Debug.LogWarning($"TowerGridSpawner: only {free.Count} free cells; spawning {count} instead of {randomCount}.", this);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = (randomPrefabs != null && randomPrefabs.Length > 0)
                ? randomPrefabs[Random.Range(0, randomPrefabs.Length)]
                : defaultPrefab;
            TrySpawn(free[i], prefab);
        }
    }

    bool TrySpawn(Vector3Int cell, GameObject prefab)
    {
        if (prefab == null) return false;
        if (!grid.IsInside(cell) || grid.IsOccupied(cell)) return false;
        if (!grid.TryGetPoint(cell, out Vector3 pos)) return false;

        Transform parent = spawnParent != null ? spawnParent : grid.transform;
        var go = Instantiate(prefab, pos, grid.transform.rotation, parent);

        Tower tower = go.GetComponentInChildren<Tower>();
        if (tower != null)
        {
            tower.gridCell = cell;     // so Sell can free this cell
            tower.root = go;           // so Sell destroys the whole spawned object
        }

        grid.SetOccupied(cell, true);   // hides the marker too, if one exists
        spawned.Add(go);
        return true;
    }
}