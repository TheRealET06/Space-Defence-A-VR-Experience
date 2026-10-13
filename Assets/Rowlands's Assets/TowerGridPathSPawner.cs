using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// Spawns a winding path of objects between two cells on a TowerGrid.
///  - Random waypoints force the path to swing back and forth (doubling back).
///  - A straight-line penalty makes it turn often (zig-zag).
///  - Per-cell random cost adds general wobble.
/// The path never crosses itself or enters occupied cells.
/// Runs after TowerGridSpawner so its objects are treated as obstacles.
[DefaultExecutionOrder(100)]
public class TowerGridPathSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TowerGrid grid;
    [SerializeField] GameObject pathPrefab;
    [SerializeField] GameObject startPrefab;   // optional, falls back to pathPrefab
    [SerializeField] GameObject endPrefab;     // optional, falls back to pathPrefab
    [SerializeField] Transform spawnParent;    // optional; defaults to the grid

    [Header("Endpoints")]
    [SerializeField] bool randomEndpoints = false;
    [SerializeField] int minEndpointDistance = 8;   // Manhattan distance in cells (random only)
    [SerializeField] Vector3Int startCell = new Vector3Int(0, 0, 0);
    [SerializeField] Vector3Int endCell = new Vector3Int(9, 0, 9);

    [Header("Doubling back")]
    [Tooltip("Random cells the path must visit in order before reaching the end. More = more back-and-forth.")]
    [SerializeField, Range(0, 10)] int waypointCount = 3;
    [Tooltip("Minimum Manhattan distance between consecutive waypoints. Higher = longer sweeps.")]
    [SerializeField, Range(1, 20)] int waypointMinSeparation = 5;

    [Header("Zig-zag")]
    [Tooltip("Extra cost for continuing straight. Higher = turns more often.")]
    [SerializeField, Range(0f, 3f)] float straightPenalty = 1.5f;
    [Tooltip("Random cost per cell. Adds general wobble.")]
    [SerializeField, Range(0f, 5f)] float wander = 1f;

    [Header("Movement")]
    [SerializeField] bool allowVerticalMoves = true;   // move between layers
    [SerializeField] bool orientAlongPath = true;
    [SerializeField] int maxAttempts = 40;             // retries with new waypoints before falling back

    [Header("Seed")]
    [SerializeField] bool useFixedSeed = false;
    [SerializeField] int seed = 12345;

    public List<Vector3Int> Path { get; private set; } = new List<Vector3Int>();
    readonly List<GameObject> spawned = new List<GameObject>();

    int cols, lays, rows, total;
    Vector3Int[] dirs;
    float[] noise;
    bool[] usedMask;

    const int DirSlots = 7;   // 0 = no previous direction, 1..6 = direction index + 1

    void Start()
    {
        if (grid == null) grid = FindAnyObjectByType<TowerGrid>();
        if (grid == null)
        {
            Debug.LogError("TowerGridPathSpawner: no TowerGrid assigned or found.", this);
            return;
        }
        if (useFixedSeed) Random.InitState(seed);

        cols = grid.Columns; lays = grid.Layers; rows = grid.Rows;
        total = cols * lays * rows;

        var d = new List<Vector3Int>
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1),
        };
        if (allowVerticalMoves)
        {
            d.Add(new Vector3Int(0, 1, 0));
            d.Add(new Vector3Int(0, -1, 0));
        }
        dirs = d.ToArray();

        List<Vector3Int> path = randomEndpoints ? FindRandomPath() : BuildPath(startCell, endCell);
        if (path == null)
        {
            Debug.LogWarning("TowerGridPathSpawner: no path found (endpoints blocked or fully walled off).", this);
            return;
        }

        Path = path;
        SpawnPath(path);
    }

    // ---------- Path building ----------

    List<Vector3Int> FindRandomPath()
    {
        var free = new List<Vector3Int>();
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < lays; y++)
                for (int z = 0; z < rows; z++)
                {
                    var c = new Vector3Int(x, y, z);
                    if (!grid.IsOccupied(c)) free.Add(c);
                }
        if (free.Count < 2) return null;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3Int a = free[Random.Range(0, free.Count)];
            Vector3Int b = free[Random.Range(0, free.Count)];
            if (Manhattan(a, b) < minEndpointDistance) continue;

            var path = BuildPath(a, b);
            if (path != null) return path;
        }
        return null;
    }

    /// Tries several random waypoint layouts; the final attempt uses no waypoints as a fallback.
    List<Vector3Int> BuildPath(Vector3Int start, Vector3Int end)
    {
        if (!grid.IsInside(start) || !grid.IsInside(end)) return null;
        if (grid.IsOccupied(start) || grid.IsOccupied(end)) return null;

        int attempts = Mathf.Max(1, maxAttempts);
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            int wp = attempt < attempts - 1 ? waypointCount : 0;
            var path = TryBuild(start, end, wp);
            if (path != null) return path;
        }
        return null;
    }

    List<Vector3Int> TryBuild(Vector3Int start, Vector3Int end, int waypoints)
    {
        noise = new float[total];
        for (int i = 0; i < total; i++) noise[i] = Random.value * wander;
        usedMask = new bool[total];

        // Pick waypoints: random free cells, spaced from the previous point.
        var points = new List<Vector3Int> { start };
        for (int i = 0; i < waypoints; i++)
        {
            for (int tries = 0; tries < 30; tries++)
            {
                var c = new Vector3Int(
                    Random.Range(0, cols),
                    allowVerticalMoves ? Random.Range(0, lays) : start.y,
                    Random.Range(0, rows));

                if (grid.IsOccupied(c) || c == start || c == end || points.Contains(c)) continue;
                if (Manhattan(c, points[points.Count - 1]) < waypointMinSeparation) continue;

                points.Add(c);
                break;
            }
        }
        points.Add(end);

        // Chain A* segments, blocking everything already used so the path can't cross itself.
        var full = new List<Vector3Int> { start };
        usedMask[Idx(start)] = true;
        int lastDir = -1;

        for (int i = 1; i < points.Count; i++)
        {
            var seg = FindSegment(points[i - 1], points[i], lastDir);
            if (seg == null) return null;

            for (int j = 1; j < seg.Count; j++)
            {
                full.Add(seg[j]);
                usedMask[Idx(seg[j])] = true;
            }
            if (seg.Count >= 2)
                lastDir = DirIndex(seg[seg.Count - 1] - seg[seg.Count - 2]);
        }
        return full;
    }

    /// A* over (cell, incoming direction) states so straight moves can be penalized.
    List<Vector3Int> FindSegment(Vector3Int a, Vector3Int goal, int lastDir)
    {
        int goalCell = Idx(goal);
        if (usedMask[goalCell]) return null;   // an earlier segment already crossed this waypoint

        int S = total * DirSlots;
        var g = new float[S];
        var f = new float[S];
        var cameFrom = new int[S];
        var closed = new bool[S];
        for (int i = 0; i < S; i++) { g[i] = float.MaxValue; cameFrom[i] = -1; }

        int startS = Idx(a) * DirSlots + (lastDir + 1);
        g[startS] = 0f;
        f[startS] = Manhattan(a, goal);

        var open = new List<int> { startS };

        while (open.Count > 0)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
                if (f[open[i]] < f[open[best]]) best = i;

            int cur = open[best];
            open.RemoveAt(best);

            int cell = cur / DirSlots;
            int slot = cur % DirSlots;

            if (cell == goalCell) return Reconstruct(cur, cameFrom);

            closed[cur] = true;
            Vector3Int cc = FromIdx(cell);

            for (int d = 0; d < dirs.Length; d++)
            {
                Vector3Int n = cc + dirs[d];
                if (!grid.IsInside(n) || grid.IsOccupied(n)) continue;

                int nCell = Idx(n);
                if (usedMask[nCell]) continue;

                int ns = nCell * DirSlots + (d + 1);
                if (closed[ns]) continue;

                float cost = 1f + noise[nCell] + (slot == d + 1 ? straightPenalty : 0f);
                float tentative = g[cur] + cost;
                if (tentative >= g[ns]) continue;

                cameFrom[ns] = cur;
                g[ns] = tentative;
                f[ns] = tentative + Manhattan(n, goal);   // admissible: every step costs >= 1
                if (!open.Contains(ns)) open.Add(ns);
            }
        }
        return null;
    }

    List<Vector3Int> Reconstruct(int endState, int[] cameFrom)
    {
        var cells = new List<Vector3Int>();
        for (int s = endState; s != -1; s = cameFrom[s]) cells.Add(FromIdx(s / DirSlots));
        cells.Reverse();

        // Loop-erase: a high straight penalty can make the search revisit a cell.
        var result = new List<Vector3Int>();
        var seen = new Dictionary<Vector3Int, int>();
        foreach (var c in cells)
        {
            if (seen.TryGetValue(c, out int idx))
            {
                for (int k = result.Count - 1; k > idx; k--)
                {
                    seen.Remove(result[k]);
                    result.RemoveAt(k);
                }
            }
            else
            {
                seen[c] = result.Count;
                result.Add(c);
            }
        }
        return result;
    }

    // ---------- Helpers ----------

    int Idx(Vector3Int c) => c.x + cols * (c.y + lays * c.z);

    Vector3Int FromIdx(int i) => new Vector3Int(i % cols, (i / cols) % lays, i / (cols * lays));

    int DirIndex(Vector3Int delta)
    {
        for (int d = 0; d < dirs.Length; d++)
            if (dirs[d] == delta) return d;
        return -1;
    }

    static int Manhattan(Vector3Int a, Vector3Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);

    // ---------- Spawning ----------

    void SpawnPath(List<Vector3Int> path)
    {
        Transform parent = spawnParent != null ? spawnParent : grid.transform;

        for (int i = 0; i < path.Count; i++)
        {
            GameObject prefab = pathPrefab;
            if (i == 0 && startPrefab != null) prefab = startPrefab;
            else if (i == path.Count - 1 && endPrefab != null) prefab = endPrefab;
            if (prefab == null) continue;

            if (!grid.TryGetPoint(path[i], out Vector3 pos)) continue;

            Quaternion rot = grid.transform.rotation;
            if (orientAlongPath && path.Count >= 2)
            {
                int a = Mathf.Min(i, path.Count - 2);
                if (grid.TryGetPoint(path[a], out Vector3 pa) && grid.TryGetPoint(path[a + 1], out Vector3 pb))
                {
                    Vector3 dir = pb - pa;
                    if (dir.sqrMagnitude > 0.0001f)
                        rot = Quaternion.LookRotation(dir.normalized, grid.transform.up);
                }
            }

            spawned.Add(Instantiate(prefab, pos, rot, parent));
            grid.SetOccupied(path[i], true);
        }
    }
}