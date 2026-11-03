using System;
using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    // ---------- Authoring data (shows up in the Inspector) ----------

    [Serializable]
    public class SpawnGroup
    {
        public EnemyData enemy;
        public int minCount = 5;
        public int maxCount = 5;          // random count in [min, max]
        public float startOffset = 0f;    // seconds after the wave begins
        public float interval = 1f;       // seconds between spawns in this group
        [Range(0f, 1f)] public float intervalJitter = 0.3f;
        [Tooltip("1 = normal health. Values <= 0 are treated as 1.")]
        public float healthMultiplier = 1f;
    }

    [Serializable]
    public class WaveDefinition
    {
        public string name = "Wave";
        public float delayBefore = 8f;    // countdown before this wave starts
        public float duration = 60f;
        [TextArea(2, 4)] public string description;
        public List<SpawnGroup> groups = new List<SpawnGroup>();
    }

    [Serializable]
    public class EnemyPoolEntry
    {
        public EnemyData enemy;
        public int cost = 1;              // how much of a wave's budget one enemy uses
        public float weight = 1f;         // relative chance of being picked
        public int firstWave = 1;         // 1-based wave where this enemy can first appear
    }

    public enum WavePhase { Waiting, Spawning, Clearing, Finished }   // Waiting = idle until the player starts the next wave

    /// One row of a wave preview: how many of one enemy type are coming, and at what health.
    public struct WavePreviewLine
    {
        public EnemyData enemy;
        public int count;
        public float health;     // strongest health among this wave's spawns of this type
        public bool isNew;       // true if this enemy type hasn't appeared in any earlier wave
    }

    [DefaultExecutionOrder(200)]
    public class WaveDirector : MonoBehaviour
    {
        public static WaveDirector Instance { get; private set; }

        struct Entry
        {
            public float time;            // relative to wave start
            public EnemyData data;
            public float healthMultiplier;
        }

        class Wave
        {
            public string name;
            public float delay;
            public float duration;
            public string description;
            public List<Entry> entries = new List<Entry>();
        }

        [Header("References")]
        [SerializeField] TowerGrid grid;
        [SerializeField] TowerGridPathSpawner pathSource;
        [SerializeField] Transform enemyParent;
        [SerializeField] float heightOffset = 0.05f;

        [Header("Legacy (only needed if other scripts read BasicEnemy / HardEnemy)")]
        [SerializeField] EnemyData basicEnemy;
        [SerializeField] EnemyData hardEnemy;

        [Header("Flow")]
        [SerializeField] int seed = 1234;

        [Header("Hand-authored waves (play first)")]
        [SerializeField] List<WaveDefinition> manualWaves = new List<WaveDefinition>();

        [Header("Generated waves (appended after the manual ones)")]
        [SerializeField] bool generateWaves = true;
        [SerializeField] int generatedWaveCount = 10;
        [SerializeField] List<EnemyPoolEntry> pool = new List<EnemyPoolEntry>();
        [SerializeField] float generatedWaveDelay = 10f;
        [SerializeField] float baseBudget = 8f;
        [SerializeField] float budgetPerWave = 4f;
        [SerializeField] float baseInterval = 1.2f;
        [SerializeField, Range(0.5f, 1f)] float intervalShrinkPerWave = 0.93f;
        [SerializeField] float minInterval = 0.2f;
        [SerializeField, Range(0f, 1f)] float intervalJitter = 0.4f;
        [SerializeField, Range(0f, 1f)] float burstChance = 0.1f;
        [SerializeField] int burstSize = 3;
        [SerializeField] float healthGrowthPerWave = 0.15f;

        readonly List<Wave> waves = new List<Wave>();
        List<Vector3> waypoints;

        WavePhase phase = WavePhase.Waiting;
        int waveIndex;
        int nextInWave;
        int spawnedTotal;
        int totalCount;
        float waveClock;

        // ---------- Public API (old members kept so existing UI still compiles) ----------

        public int TotalCount => totalCount;
        public int SpawnedCount => spawnedTotal;
        public int RemainingCount => totalCount - spawnedTotal + Enemy.Active.Count;
        public bool AllSpawned => spawnedTotal >= totalCount;
        public float TimeUntilStart => 0f;   // waves are player-triggered now; kept so old UI compiles
        public float Progress => totalCount > 0 ? Mathf.Clamp01((float)spawnedTotal / totalCount) : 1f;
        public EnemyData BasicEnemy => basicEnemy;
        public EnemyData HardEnemy => hardEnemy;

        // New wave-aware API for the HUD
        public int CurrentWave => waveIndex + 1;
        public int TotalWaves => waves.Count;
        public WavePhase Phase => phase;
        public float TimeUntilNextWave => 0f;

        /// True only when no wave is active (nothing spawning, no enemies alive) and the game is running.
        public bool CanStartWave =>
            phase == WavePhase.Waiting &&
            (GameManager.Instance == null || GameManager.Instance.IsPlaying);

        /// Called by the control board button. Ignored unless CanStartWave, so waves can never overlap.
        public void StartNextWave()
        {
            if (!CanStartWave) return;
            BeginWave();
        }
        public string CurrentWaveName => waveIndex < waves.Count ? waves[waveIndex].name : "";

        public bool WaveRunning => phase == WavePhase.Spawning || phase == WavePhase.Clearing;
        public float WaveDuration => waveIndex < waves.Count ? waves[waveIndex].duration : 0f;
        public float TimeLeft => WaveRunning ? Mathf.Max(0f, WaveDuration - waveClock) : 0f;

        public string GetWaveDescription(int waveNumber) =>
            waveNumber >= 1 && waveNumber <= waves.Count ? waves[waveNumber - 1].description : "";

        /// 1-based number of the next wave the player hasn't started yet (the one after the active wave
        /// while a wave is running). 0 when there are no more waves to start.
        public int UpcomingWaveNumber
        {
            get
            {
                if (phase == WavePhase.Finished) return 0;
                int idx = phase == WavePhase.Waiting ? waveIndex : waveIndex + 1;
                return idx < waves.Count ? idx + 1 : 0;
            }
        }

        public string GetWaveName(int waveNumber) =>
            waveNumber >= 1 && waveNumber <= waves.Count ? waves[waveNumber - 1].name : "";

        /// Enemy types and counts for a wave (1-based), in order of first appearance.
        public List<WavePreviewLine> GetWavePreview(int waveNumber)
        {
            var result = new List<WavePreviewLine>();
            if (waveNumber < 1 || waveNumber > waves.Count) return result;
            int idx = waveNumber - 1;

            foreach (var e in waves[idx].entries)
            {
                float mult = e.healthMultiplier > 0f ? e.healthMultiplier : 1f;
                float hp = e.data.maxHealth * mult;
                int i = result.FindIndex(l => l.enemy == e.data);
                if (i < 0)
                {
                    result.Add(new WavePreviewLine { enemy = e.data, count = 1, health = hp, isNew = !SeenBefore(e.data, idx) });
                }
                else
                {
                    var line = result[i];
                    line.count++;
                    line.health = Mathf.Max(line.health, hp);
                    result[i] = line;
                }
            }
            return result;
        }

        bool SeenBefore(EnemyData data, int beforeIndex)
        {
            for (int w = 0; w < beforeIndex; w++)
                foreach (var e in waves[w].entries)
                    if (e.data == data) return true;
            return false;
        }

        public event Action<int, int> WaveStarted;   // (waveNumber, totalWaves)
        public event Action<int> WaveEnded;          // (waveNumber) fired when a wave is fully cleared, including the final wave
        public event Action<int> ReadyForWave;       // fired when a wave is cleared and the next one (number given) can be started
        public event Action GameWon;                 // fired once when the final wave is cleared

        /// Enemies still to spawn in the active wave plus enemies alive on the field.
        public int WaveEnemiesLeft =>
            (phase == WavePhase.Spawning ? waves[waveIndex].entries.Count - nextInWave : 0) + Enemy.Active.Count;

        // ---------- Lifecycle ----------

        void Awake()
        {
            Instance = this;
            BuildAllWaves();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (grid == null) grid = FindAnyObjectByType<TowerGrid>();
            if (pathSource == null) pathSource = FindAnyObjectByType<TowerGridPathSpawner>();

            if (grid == null || pathSource == null)
            {
                Debug.LogError("WaveDirector: missing grid or path spawner.", this);
                enabled = false;
                return;
            }

            if (waves.Count == 0 || totalCount == 0)
            {
                Debug.LogError("WaveDirector: no waves to run. Add manual waves or fill the enemy pool (enemies need prefabs).", this);
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

        // ---------- Schedule building ----------

        static bool Usable(EnemyData d) => d != null && d.prefab != null;

        void BuildAllWaves()
        {
            waves.Clear();
            totalCount = 0;

            // Hand-authored waves
            for (int i = 0; i < manualWaves.Count; i++)
                waves.Add(BuildManualWave(manualWaves[i], i));

            // Generated waves
            if (generateWaves)
            {
                for (int i = 0; i < generatedWaveCount; i++)
                {
                    int waveNumber = waves.Count + 1;   // 1-based, continues after manual waves
                    waves.Add(BuildGeneratedWave(waveNumber, i));
                }
            }

            foreach (var w in waves) totalCount += w.entries.Count;
        }

        Wave BuildManualWave(WaveDefinition def, int index)
        {
            var wave = new Wave { name = string.IsNullOrEmpty(def.name) ? $"Wave {index + 1}" : def.name, delay = def.delayBefore, duration = def.duration, description = def.description };
            var rng = new System.Random(seed + index * 7919);

            foreach (var g in def.groups)
            {
                if (!Usable(g.enemy)) continue;

                int count = rng.Next(Mathf.Min(g.minCount, g.maxCount), Mathf.Max(g.minCount, g.maxCount) + 1);
                float t = g.startOffset;
                for (int i = 0; i < count; i++)
                {
                    wave.entries.Add(new Entry { time = t, data = g.enemy, healthMultiplier = g.healthMultiplier });
                    float jitter = 1f + ((float)rng.NextDouble() * 2f - 1f) * g.intervalJitter;
                    t += Mathf.Max(0.05f, g.interval * jitter);
                }
            }

            wave.entries.Sort((a, b) => a.time.CompareTo(b.time));
            return wave;
        }

        Wave BuildGeneratedWave(int waveNumber, int generatedIndex)
        {
            var wave = new Wave { name = $"Wave {waveNumber}", delay = generatedWaveDelay, duration = 60f };
            var rng = new System.Random(seed + waveNumber * 104729);   // per-wave seed: editing one wave doesn't reshuffle the others

            float budget = baseBudget + budgetPerWave * generatedIndex;
            float interval = Mathf.Max(minInterval, baseInterval * Mathf.Pow(intervalShrinkPerWave, generatedIndex));
            float healthMult = 1f + healthGrowthPerWave * generatedIndex;
            float t = 0f;

            while (true)
            {
                int n = rng.NextDouble() < burstChance ? Mathf.Max(1, burstSize) : 1;
                bool spawnedAny = false;

                for (int i = 0; i < n; i++)
                {
                    var pick = PickAffordable(waveNumber, budget, rng);
                    if (pick == null) break;
                    budget -= pick.cost;
                    wave.entries.Add(new Entry { time = t, data = pick.enemy, healthMultiplier = healthMult });
                    spawnedAny = true;
                }

                if (!spawnedAny) break;

                float jitter = 1f + ((float)rng.NextDouble() * 2f - 1f) * intervalJitter;
                t += Mathf.Max(0.05f, interval * jitter);
            }

            return wave;
        }

        EnemyPoolEntry PickAffordable(int waveNumber, float budget, System.Random rng)
        {
            var options = new List<EnemyPoolEntry>();
            float total = 0f;
            foreach (var p in pool)
            {
                if (p == null || !Usable(p.enemy) || p.weight <= 0f) continue;
                if (p.firstWave > waveNumber || p.cost > budget) continue;
                options.Add(p);
                total += p.weight;
            }
            if (options.Count == 0) return null;

            float r = (float)rng.NextDouble() * total;
            foreach (var o in options)
            {
                r -= o.weight;
                if (r <= 0f) return o;
            }
            return options[options.Count - 1];
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

        // ---------- Runtime ----------

        void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            float dt = Time.deltaTime;

            switch (phase)
            {
                case WavePhase.Waiting:
                    break;   // idle until StartNextWave() is called

                case WavePhase.Spawning:
                    waveClock += dt;
                    var entries = waves[waveIndex].entries;
                    while (nextInWave < entries.Count && entries[nextInWave].time <= waveClock)
                    {
                        Spawn(entries[nextInWave]);
                        nextInWave++;
                        spawnedTotal++;
                    }
                    if (nextInWave >= entries.Count) phase = WavePhase.Clearing;
                    break;

                case WavePhase.Clearing:
                    waveClock += dt;
                    if (waveClock < WaveDuration) break;
                    bool isLast = waveIndex >= waves.Count - 1;
                    if (isLast)
                    {
                        // Final wave: always wait until everything is dead, then win.
                        if (Enemy.Active.Count == 0)
                        {
                            phase = WavePhase.Finished;
                            WaveEnded?.Invoke(waveIndex + 1);
                            GameWon?.Invoke();   // WaveAudioDirector plays the win sound from this
                            if (GameManager.Instance != null) GameManager.Instance.Win();
                        }
                    }
                    else if (Enemy.Active.Count == 0)
                    {
                        // Wave fully spawned AND fully dead: unlock the button for the next one.
                        WaveEnded?.Invoke(waveIndex + 1);   // number of the wave that just ended
                        waveIndex++;
                        phase = WavePhase.Waiting;
                        ReadyForWave?.Invoke(waveIndex + 1);
                    }
                    break;
            }
        }

        void BeginWave()
        {
            waveClock = 0f;
            nextInWave = 0;
            phase = WavePhase.Spawning;
            WaveStarted?.Invoke(waveIndex + 1, waves.Count);
        }

        void Spawn(Entry entry)
        {
            // Inspector-created list elements can come through as 0; a 0 multiplier means 0 HP, so treat it as 1.
            float mult = entry.healthMultiplier > 0f ? entry.healthMultiplier : 1f;
            Enemy enemy = Instantiate(entry.data.prefab, waypoints[0], grid.transform.rotation, enemyParent);
            enemy.Init(entry.data, waypoints, grid.transform.up, mult);
        }
    }
}