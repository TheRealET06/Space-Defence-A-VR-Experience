using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Brian
{
    public class GridPoints : MonoBehaviour
    {
        [SerializeField] TowerGrid grid;
        [SerializeField] Material material;
        [SerializeField] float size = 0.06f;
        [SerializeField] float thickness = 0.005f;
        [SerializeField] float refreshInterval = 0.2f;

        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> indices = new List<int>();
        Mesh mesh;
        bool[] shown;
        float timer;

        static readonly int[] BoxIndices =
        {
            0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 1, 5, 0, 5, 4,
            2, 3, 7, 2, 7, 6, 1, 2, 6, 1, 6, 5, 0, 4, 7, 0, 7, 3
        };

        void Start()
        {
            if (grid == null) grid = GetComponent<TowerGrid>();
            if (grid == null || material == null)
            {
                enabled = false;
                return;
            }

            var view = new GameObject("Grid Points");
            view.transform.SetParent(grid.transform, false);
            mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
            view.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = view.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            shown = new bool[grid.Columns * grid.Layers * grid.Rows];
            Refresh(true);
        }

        void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = refreshInterval;
            Refresh(false);
        }

        void Refresh(bool force)
        {
            bool changed = force;
            int i = 0;
            for (int x = 0; x < grid.Columns; x++)
                for (int y = 0; y < grid.Layers; y++)
                    for (int z = 0; z < grid.Rows; z++, i++)
                    {
                        bool free = !grid.IsOccupied(new Vector3Int(x, y, z));
                        if (shown[i] != free)
                        {
                            shown[i] = free;
                            changed = true;
                        }
                    }
            if (changed) Rebuild();
        }

        void Rebuild()
        {
            vertices.Clear();
            indices.Clear();
            int i = 0;
            for (int x = 0; x < grid.Columns; x++)
                for (int y = 0; y < grid.Layers; y++)
                    for (int z = 0; z < grid.Rows; z++, i++)
                    {
                        if (!shown[i]) continue;
                        if (!grid.TryGetPoint(new Vector3Int(x, y, z), out Vector3 world)) continue;
                        Vector3 c = grid.transform.InverseTransformPoint(world);
                        AddBox(c, new Vector3(size, thickness, thickness));
                        AddBox(c, new Vector3(thickness, size, thickness));
                        AddBox(c, new Vector3(thickness, thickness, size));
                    }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        void AddBox(Vector3 c, Vector3 s)
        {
            Vector3 h = s * 0.5f;
            int b = vertices.Count;
            vertices.Add(c + new Vector3(-h.x, -h.y, -h.z));
            vertices.Add(c + new Vector3(h.x, -h.y, -h.z));
            vertices.Add(c + new Vector3(h.x, h.y, -h.z));
            vertices.Add(c + new Vector3(-h.x, h.y, -h.z));
            vertices.Add(c + new Vector3(-h.x, -h.y, h.z));
            vertices.Add(c + new Vector3(h.x, -h.y, h.z));
            vertices.Add(c + new Vector3(h.x, h.y, h.z));
            vertices.Add(c + new Vector3(-h.x, h.y, h.z));
            foreach (int k in BoxIndices) indices.Add(b + k);
        }
    }
}
