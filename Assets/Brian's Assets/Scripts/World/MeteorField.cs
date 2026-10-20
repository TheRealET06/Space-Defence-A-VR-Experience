using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    [DefaultExecutionOrder(50)]
    public class MeteorField : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] TowerGrid grid;
        [SerializeField] Meteor[] meteorPrefabs;
        [SerializeField] Transform meteorParent;

        [Header("Launch")]
        [SerializeField] int launchCount = 8;
        [SerializeField] List<Vector3Int> reservedCells = new List<Vector3Int> { new Vector3Int(20, 1, 5), new Vector3Int(0, 1, 5) };
        [SerializeField] int reservedRadius = 1;

        [Header("Mid-game")]
        [SerializeField] Vector2 spawnInterval = new Vector2(15f, 30f);
        [SerializeField] int maxMeteors = 30;

        readonly List<Meteor> meteors = new List<Meteor>();
        float timer;

        public int Count => meteors.Count;

        void Start()
        {
            if (grid == null) grid = FindAnyObjectByType<TowerGrid>();
            if (grid == null || meteorPrefabs == null || meteorPrefabs.Length == 0)
            {
                Debug.LogError("MeteorField: missing grid or meteor prefabs.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < launchCount; i++)
                if (!SpawnRandom()) break;

            timer = NextInterval();
        }

        void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = NextInterval();
            if (meteors.Count < maxMeteors) SpawnRandom();
        }

        float NextInterval() => Random.Range(spawnInterval.x, spawnInterval.y);

        bool SpawnRandom()
        {
            var free = FreeCells();
            if (free.Count == 0) return false;

            Vector3Int cell = free[Random.Range(0, free.Count)];
            if (!grid.TryGetPoint(cell, out Vector3 pos)) return false;

            Meteor prefab = meteorPrefabs[Random.Range(0, meteorPrefabs.Length)];
            Transform parent = meteorParent != null ? meteorParent : grid.transform;
            Meteor meteor = Instantiate(prefab, pos, Random.rotation, parent);
            meteor.Place(this, cell);
            grid.SetOccupied(cell, true);
            meteors.Add(meteor);
            return true;
        }

        List<Vector3Int> FreeCells()
        {
            var free = new List<Vector3Int>();
            for (int x = 0; x < grid.Columns; x++)
                for (int y = 0; y < grid.Layers; y++)
                    for (int z = 0; z < grid.Rows; z++)
                    {
                        var c = new Vector3Int(x, y, z);
                        if (!grid.IsOccupied(c) && !IsReserved(c)) free.Add(c);
                    }
            return free;
        }

        bool IsReserved(Vector3Int c)
        {
            foreach (var r in reservedCells)
                if (Mathf.Abs(c.x - r.x) <= reservedRadius &&
                    Mathf.Abs(c.y - r.y) <= reservedRadius &&
                    Mathf.Abs(c.z - r.z) <= reservedRadius)
                    return true;
            return false;
        }

        public void Release(Meteor meteor)
        {
            if (!meteors.Remove(meteor)) return;
            grid.SetOccupied(meteor.Cell, false);
        }
    }
}
