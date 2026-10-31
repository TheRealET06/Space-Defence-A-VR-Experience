using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Brian
{
    public class AsteroidField : MonoBehaviour
    {
        [Serializable]
        public class Band
        {
            public ResourceAsteroid prefab;
            public float minDistance;
            public float maxDistance = 1.25f;
            public int startCount = 2;
            public int cap = 4;
        }

        public struct Face
        {
            public Vector3 origin;
            public Vector3 normal;
        }

        [SerializeField] Band[] bands;
        [SerializeField] float spawnInterval = 2f;
        [SerializeField] float bandMargin = 0.1f;

        TowerGrid grid;
        Face face;
        bool ready;
        List<ResourceAsteroid>[] alive;
        readonly Dictionary<ResourceAsteroid, Vector3Int> cells = new Dictionary<ResourceAsteroid, Vector3Int>();
        readonly List<Vector3Int> candidates = new List<Vector3Int>();
        readonly List<ResourceAsteroid> gone = new List<ResourceAsteroid>();
        float[] timers;
        float colliderSweep;

        public static bool TryGetFace(TowerGrid grid, out Face face)
        {
            face = default;
            if (grid == null) return false;
            if (!grid.TryGetPoint(Vector3Int.zero, out Vector3 corner)) return false;
            if (!grid.TryGetPoint(new Vector3Int(grid.Columns - 1, 0, 0), out Vector3 far)) return false;
            face.origin = corner;
            face.normal = (far - corner).normalized;
            return true;
        }

        void Start()
        {
            grid = FindAnyObjectByType<TowerGrid>();
            if (bands == null || bands.Length == 0 || !TryGetFace(grid, out face))
            {
                Debug.LogError("AsteroidField: needs a TowerGrid in the scene and at least one band.", this);
                enabled = false;
                return;
            }
            alive = new List<ResourceAsteroid>[bands.Length];
            timers = new float[bands.Length];
            for (int i = 0; i < bands.Length; i++) alive[i] = new List<ResourceAsteroid>();
        }

        void Update()
        {
            if (!ready)
            {
                ready = true;
                for (int i = 0; i < bands.Length; i++)
                {
                    for (int n = 0; n < Mathf.Min(bands[i].startCount, bands[i].cap); n++) Spawn(i);
                    timers[i] = spawnInterval;
                }
                return;
            }

            gone.Clear();
            foreach (var pair in cells)
                if (pair.Key == null) gone.Add(pair.Key);
            foreach (var a in gone)
            {
                grid.SetOccupied(cells[a], false);
                cells.Remove(a);
            }
            for (int i = 0; i < bands.Length; i++) alive[i].RemoveAll(a => a == null);

            colliderSweep -= Time.deltaTime;
            if (colliderSweep <= 0f)
            {
                colliderSweep = 0.1f;
                foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            }

            var wave = WaveDirector.Instance;
            if (wave == null || !wave.WaveRunning || wave.TimeLeft <= 0f) return;

            for (int i = 0; i < bands.Length; i++)
            {
                if (alive[i].Count >= bands[i].cap)
                {
                    timers[i] = spawnInterval;
                    continue;
                }
                timers[i] -= Time.deltaTime;
                if (timers[i] > 0f) continue;
                timers[i] = spawnInterval;
                Spawn(i);
            }
        }

        void Spawn(int index)
        {
            var band = bands[index];
            if (band.prefab == null) return;

            candidates.Clear();
            for (int x = 0; x < grid.Columns; x++)
                for (int y = 0; y < grid.Layers; y++)
                    for (int z = 0; z < grid.Rows; z++)
                    {
                        var c = new Vector3Int(x, y, z);
                        if (grid.IsOccupied(c) || !grid.TryGetPoint(c, out Vector3 p)) continue;
                        float depth = Vector3.Dot(p - face.origin, face.normal);
                        if (depth >= band.minDistance + bandMargin && depth <= band.maxDistance - bandMargin) candidates.Add(c);
                    }
            if (candidates.Count == 0) return;

            var cell = candidates[Random.Range(0, candidates.Count)];
            grid.TryGetPoint(cell, out Vector3 point);
            var asteroid = Instantiate(band.prefab, point, Random.rotation, transform);
            foreach (var c in asteroid.GetComponentsInChildren<Collider>()) c.enabled = false;
            grid.SetOccupied(cell, true);
            cells[asteroid] = cell;
            alive[index].Add(asteroid);
        }
    }
}
