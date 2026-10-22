using System.Text;
using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Enemies
{
    /// <summary>
    /// Spawns regular enemies from the spawn table (rates depend on the current milestone) and a boss at
    /// each milestone. Enemies appear on a ring just outside the camera view and despawn when far away.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [SerializeField] SpawnTableSO table;

        [Header("Limits")]
        [Tooltip("GDD: 50 enemies at once. Bosses bypass this.")]
        [SerializeField] int maxEnemies = 50;
        [Tooltip("Extra distance beyond the camera corner where enemies appear.")]
        [SerializeField] float spawnMargin = 2f;
        [Tooltip("Non-boss enemies farther than spawn distance x this are removed.")]
        [SerializeField] float despawnDistanceMultiplier = 2.5f;

        [Header("Boss formula (GDD section 7.1)")]
        [SerializeField] int cellsPerMilestone = 50;
        [SerializeField] float regenDivisor = 100f;
        [SerializeField] float defensePerMilestone = 1.5f;
        [SerializeField] float bossSpeed = 5f;
        [SerializeField] Color bossColor = new Color(0.80f, 0.25f, 0.85f, 1f);

        float[] _accumulators = new float[0];
        float _nextSweep;
        SwarmBody _player;
        bool _warnedFallback;

        public int NonBossCount => Enemy.CountNonBoss();
        public int MaxEnemies => maxEnemies;

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {
            RunDirector.MilestoneReached += OnMilestoneReached;
        }

        void OnDisable()
        {
            RunDirector.MilestoneReached -= OnMilestoneReached;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            if (_player == null) _player = FindAnyObjectByType<SwarmBody>();
            if (_player == null || _player.IsDead) return;

            EnsureTable();
            int milestone = CurrentMilestone();

            if (_accumulators.Length != table.entries.Length)
                _accumulators = new float[table.entries.Length];

            int count = Enemy.CountNonBoss();

            for (int i = 0; i < table.entries.Length; i++)
            {
                var entry = table.entries[i];
                if (entry == null || entry.type == null) continue;

                _accumulators[i] += entry.RateAt(milestone) * dt;

                while (_accumulators[i] >= 1f)
                {
                    if (count >= maxEnemies)
                    {
                        _accumulators[i] = Mathf.Min(_accumulators[i], 1f);
                        break;
                    }
                    SpawnRegular(entry.type, milestone);
                    _accumulators[i] -= 1f;
                    count++;
                }
            }

            if (Time.time >= _nextSweep)
            {
                _nextSweep = Time.time + 0.5f;
                DespawnFarEnemies();
            }
        }

        void OnMilestoneReached(int milestone)
        {
            if (_player == null) _player = FindAnyObjectByType<SwarmBody>();
            if (_player == null) return;
            SpawnBoss(milestone);
        }

        void SpawnRegular(EnemyTypeSO type, int milestone)
        {
            var spec = EnemySpec.FromType(type, milestone);

            // GDD: in endless mode every non-boss enemy has DEF = milestone - 5.
            spec.defense = Mathf.Max(spec.defense, milestone - 5);

            Enemy.Create(spec, RandomRingPosition());
        }

        void SpawnBoss(int n)
        {
            int maxCells = n * cellsPerMilestone;
            var spec = new EnemySpec
            {
                name = "Boss " + n,
                color = bossColor,
                maxCells = maxCells,
                regen = maxCells / regenDivisor,
                defense = Mathf.Floor(n * defensePerMilestone),
                speed = bossSpeed,
                behavior = EnemyBehavior.Chase, // Phase 4: random weapon; ranged weapon uses KeepDistance
                scoreValue = 10,
                scorePerCell = true,
                expValue = 50 * n, // PLACEHOLDER
                isBoss = true,
                milestone = n
            };
            Enemy.Create(spec, RandomRingPosition());
        }

        Vector2 RandomRingPosition()
        {
            float angle = Random.value * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return (Vector2)_player.transform.position + dir * SpawnDistance();
        }

        float SpawnDistance()
        {
            var cam = Camera.main;
            float half = cam != null
                ? cam.orthographicSize * Mathf.Sqrt(cam.aspect * cam.aspect + 1f)
                : 8f;
            return half + spawnMargin;
        }

        void DespawnFarEnemies()
        {
            float limit = SpawnDistance() * despawnDistanceMultiplier;
            float limitSqr = limit * limit;
            Vector2 p = _player.transform.position;

            for (int i = Enemy.Active.Count - 1; i >= 0; i--)
            {
                var e = Enemy.Active[i];
                if (e == null || e.IsBoss) continue;
                if (((Vector2)e.transform.position - p).sqrMagnitude > limitSqr) e.Despawn();
            }
        }

        int CurrentMilestone()
        {
            return RunDirector.Instance != null ? RunDirector.Instance.Milestone : 0;
        }

        void EnsureTable()
        {
            if (table != null && table.entries != null && table.entries.Length > 0) return;

            if (!_warnedFallback)
            {
                _warnedFallback = true;
                Debug.LogWarning("EnemySpawner: no Spawn Table assigned, using built-in GDD defaults. " +
                                 "Run Cellstorm > Phase 3 > Setup Game Scene to create editable assets.");
            }
            table = EnemyDefaults.CreateTable(EnemyDefaults.CreateTypes());
        }

        /// <summary>Debug text: current spawn rates per second.</summary>
        public string DescribeRates()
        {
            EnsureTable();
            int milestone = CurrentMilestone();
            var sb = new StringBuilder();
            for (int i = 0; i < table.entries.Length; i++)
            {
                var e = table.entries[i];
                if (e == null || e.type == null) continue;
                if (sb.Length > 0) sb.Append("   ");
                sb.Append(e.type.displayName).Append(' ').Append(e.RateAt(milestone).ToString("0.0#"));
            }
            return sb.ToString();
        }
    }
}
