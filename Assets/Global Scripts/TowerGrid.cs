using System.Collections.Generic;
using UnityEngine;

/// 3D grid of tower placement points (X = columns, Y = layers, Z = rows),
/// centered on this transform horizontally, with layer 0 at the transform's height.
/// Move/rotate/scale this object to position the grid in your XR play space.
public class TowerGrid : MonoBehaviour
{
    public static TowerGrid Instance { get; private set; }   // NEW

    [Header("Grid")]
    [SerializeField] int columns = 10;        // X
    [SerializeField] int layers = 3;          // Y
    [SerializeField] int rows = 10;           // Z
    [SerializeField] float cellSize = 0.2f;   // spacing on X and Z, in meters
    [SerializeField] float layerSpacing = 0.2f; // spacing on Y, in meters

    [Header("Visuals (optional)")]
    [SerializeField] GameObject pointMarkerPrefab;
    [SerializeField] bool drawGizmos = true;

    Vector3[,,] points;
    bool[,,] occupied;
    GameObject[,,] markers;

    public int Columns => columns;
    public int Layers => layers;
    public int Rows => rows;
    readonly Dictionary<Vector3Int, TowerNode> nodes = new Dictionary<Vector3Int, TowerNode>();
    public void RegisterNode(Vector3Int cell, TowerNode node) => nodes[cell] = node;

    void Awake()            // CHANGED: was "void Awake() => Build();"
    {
        Instance = this;
        Build();
    }

    public void Build()
    {
        points = new Vector3[columns, layers, rows];
        occupied = new bool[columns, layers, rows];
        markers = new GameObject[columns, layers, rows];

        for (int x = 0; x < columns; x++)
            for (int y = 0; y < layers; y++)
                for (int z = 0; z < rows; z++)
                {
                    points[x, y, z] = GridToWorld(x, y, z);

                    if (pointMarkerPrefab != null)
                    {
                        markers[x, y, z] = Instantiate(pointMarkerPrefab, points[x, y, z],
                                                       transform.rotation, transform);
                    }
                }
    }

    Vector3 GridToWorld(int x, int y, int z)
    {
        float offsetX = (columns - 1) * cellSize * 0.5f;
        float offsetZ = (rows - 1) * cellSize * 0.5f;
        Vector3 local = new Vector3(x * cellSize - offsetX, y * layerSpacing, z * cellSize - offsetZ);
        return transform.TransformPoint(local);
    }

    /// Snap a world position to the nearest cell. False if outside the grid or occupied.
    public bool TryGetNearestFreePoint(Vector3 worldPos, out Vector3Int cell, out Vector3 snapped)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        float offsetX = (columns - 1) * cellSize * 0.5f;
        float offsetZ = (rows - 1) * cellSize * 0.5f;

        int x = Mathf.RoundToInt((local.x + offsetX) / cellSize);
        int y = Mathf.RoundToInt(local.y / layerSpacing);
        int z = Mathf.RoundToInt((local.z + offsetZ) / cellSize);

        cell = new Vector3Int(x, y, z);
        snapped = default;

        if (x < 0 || x >= columns || y < 0 || y >= layers || z < 0 || z >= rows) return false;
        if (occupied[x, y, z]) return false;

        snapped = points[x, y, z];
        return true;
    }

    /// Finds the first free point along a ray (nearest to the ray origin) that lies
    /// within pickRadius of the ray. Good for aiming a controller at floating points.
    public bool TryGetFreePointAlongRay(Ray ray, float pickRadius, float maxDistance,
                                        out Vector3Int cell, out Vector3 snapped)
    {
        cell = default;
        snapped = default;
        float bestT = float.MaxValue;
        bool found = false;

        for (int x = 0; x < columns; x++)
            for (int y = 0; y < layers; y++)
                for (int z = 0; z < rows; z++)
                {
                    if (occupied[x, y, z]) continue;

                    Vector3 p = points[x, y, z];
                    float t = Vector3.Dot(p - ray.origin, ray.direction);
                    if (t < 0f || t > maxDistance || t >= bestT) continue;

                    float dist = Vector3.Distance(p, ray.origin + ray.direction * t);
                    if (dist > pickRadius) continue;

                    bestT = t;
                    cell = new Vector3Int(x, y, z);
                    snapped = p;
                    found = true;
                }
        return found;
    }

    public void SetOccupied(Vector3Int cell, bool value)
    {
        // NEW: ignore invalid cells (e.g. a tower whose gridCell was never set)
        if (cell.x < 0 || cell.x >= columns || cell.y < 0 || cell.y >= layers || cell.z < 0 || cell.z >= rows) return;

        occupied[cell.x, cell.y, cell.z] = value;
        if (markers[cell.x, cell.y, cell.z] != null)
            markers[cell.x, cell.y, cell.z].SetActive(!value);
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.cyan;
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < layers; y++)
                for (int z = 0; z < rows; z++)
                    Gizmos.DrawWireSphere(GridToWorld(x, y, z), cellSize * 0.1f);
    }
    public bool IsInside(Vector3Int c) =>
    c.x >= 0 && c.x < columns && c.y >= 0 && c.y < layers && c.z >= 0 && c.z < rows;

    public bool IsOccupied(Vector3Int c) => occupied[c.x, c.y, c.z];

    public bool TryGetPoint(Vector3Int c, out Vector3 world)
    {
        world = default;
        if (!IsInside(c)) return false;
        world = points[c.x, c.y, c.z];
        return true;
    }

    /// Nearest node of the given type along the ray (within pickRadius) that doesn't already have a tower.
    public bool TryGetFreeNodeAlongRay(Ray ray, float pickRadius, float maxDistance,
                                       NodeType requiredType, out TowerNode best)
    {
        best = null;
        float bestT = float.MaxValue;

        foreach (var n in nodes.Values)
        {
            if (n == null || n.HasTower || n.Type != requiredType) continue;

            Vector3 p = n.SnapPoint;
            float t = Vector3.Dot(p - ray.origin, ray.direction);
            if (t < 0f || t > maxDistance || t >= bestT) continue;

            if (Vector3.Distance(p, ray.origin + ray.direction * t) > pickRadius) continue;

            bestT = t;
            best = n;
        }
        return best != null;
    }
}
