using System.Collections.Generic;
using UnityEngine;
/*
namespace Brian
{
    [DefaultExecutionOrder(200)]
    public class WaveDirector : MonoBehaviour
    {
        public static WaveDirector Instance { get; private set; }

        struct Entry
        {
            public float time;
            public EnemyData data;
            public float healthMultiplier;
        }

        [Header("References")]
        [SerializeField] TowerGrid grid;
        [SerializeField] TowerGridPathSpawner pathSource;
        [SerializeField] EnemyData basicEnemy;
        [SerializeField] EnemyData hardEnemy;
        [SerializeField] Transform enemyParent;
        [SerializeField] float heightOffset = 0.05f;

        [Header("Schedule")]
        [SerializeField] int seed = 1234;
        [SerializeField] float startDelay = 10f;
        [SerializeField] float duration = 300f;
        [SerializeField] float startRate = 0.12f;
        [SerializeField] float endRate = 1.3f;
        [SerializeField, Range(0f, 1f)] float startHardRatio = 0f;
        [SerializeField, Range(0f, 1f)] float endHardRatio = 0.7f;
        [SerializeField, Range(0f, 1f)] float intervalJitter = 0.3f;
        [SerializeField] float endHealthMultiplier = 2.5f;

        readonly List<Entry> schedule = new List<Entry>();
        List<Vector3> waypoints;
        float clock;
        int nextIndex;

        public int TotalCount => schedule.Count;
        public int SpawnedCount => nextIndex;
        public int RemainingCount => TotalCount - SpawnedCount + Enemy.Active.Count;
        public bool AllSpawned => nextIndex >= schedule.Count;
        public float TimeUntilStart => Mathf.Max(0f, startDelay - clock);
        public float Progress => duration > 0f ? Mathf.Clamp01((clock - startDelay) / duration) : 1f;
        public EnemyData BasicEnemy => basicEnemy;
        public EnemyData HardEnemy => hardEnemy;

        void Awake()
        {
            Instance = this;
            BuildSchedule();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (grid == null) grid = FindAnyObjectByType<TowerGrid>();
            if (pathSource == null) pathSource = FindAnyObjectByType<TowerGridPathSpawner>();

            if (grid == null || pathSource == null || basicEnemy == null || basicEnemy.prefab == null)
            {
                Debug.LogError("WaveDirector: missing grid, path spawner, or basic enemy (with prefab).", this);
                enabled = false;
                return;
            }

            waypoints = BuildWaypoints();
            if (waypoints.Count < 2)
            {
                Debug.LogError("WaveDirector: no path to follow.", this);
                enabled = false;
            }
        }

        void BuildSchedule()
        {
            schedule.Clear();
            var rng = new System.Random(seed);
            float t = 0f;

            while (true)
            {
                float rate = Mathf.Max(0.01f, Mathf.Lerp(startRate, endRate, t / duration));
                float jitter = 1f + ((float)rng.NextDouble() * 2f - 1f) * intervalJitter;
                t += jitter / rate;
                if (t > duration) break;

                float k = t / duration;
                bool hard = hardEnemy != null && rng.NextDouble() < Mathf.Lerp(startHardRatio, endHardRatio, k);
                schedule.Add(new Entry
                {
                    time = startDelay + t,
                    data = hard ? hardEnemy : basicEnemy,
                    healthMultiplier = Mathf.Lerp(1f, endHealthMultiplier, k)
                });
            }
        }

        List<Vector3> BuildWaypoints()
        {
            var points = new List<Vector3>();
            Vector3 lift = grid.transform.up * heightOffset;
            foreach (var cell in pathSource.Path)
                if (grid.TryGetPoint(cell, out Vector3 p))
                    points.Add(p + lift);
            return points;
        }

        void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            clock += Time.deltaTime;

            while (nextIndex < schedule.Count && schedule[nextIndex].time <= clock)
            {
                Spawn(schedule[nextIndex]);
                nextIndex++;
            }

            if (AllSpawned && Enemy.Active.Count == 0 && GameManager.Instance != null)
                GameManager.Instance.Win();
        }

        void Spawn(Entry entry)
        {
            Enemy enemy = Instantiate(entry.data.prefab, waypoints[0], grid.transform.rotation, enemyParent);
            enemy.Init(entry.data, waypoints, grid.transform.up, entry.healthMultiplier);
        }
    }
}
*/
