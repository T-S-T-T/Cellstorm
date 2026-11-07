using System.Collections.Generic;
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

        [Header("Boss formula (GDD V0.1.1 section 7.1)")]
        [Tooltip("Boss max HP = this x milestone number. Bosses never regenerate.")]
        [SerializeField] float hpPerMilestone = 500f;
        [Tooltip("Boss DEF = floor(this x milestone number).")]
        [SerializeField] float defensePerMilestone = 1.5f;
        [SerializeField] float bossSpeed = 5f;
        [Tooltip("Boss contact damage in HP per second, before the milestone multiplier.")]
        [SerializeField] float bossContactDps = 15f;
        [Tooltip("Boss contact and weapon damage grows by this per milestone after the first (0.2 = +20%).")]
        [SerializeField] float bossDamageGrowth = 0.2f;
        [SerializeField] int bossScorePerMilestone = 500;
        [SerializeField] Color bossColor = new Color(0.80f, 0.25f, 0.85f, 1f);
        [Tooltip("Each boss picks one of these at random. Empty = built-in Ram, Jaw and Enemy Gun.")]
        [SerializeField] WeaponSO[] bossWeapons;

        // Runtime copy of the table (so adding the support cell to an old table never edits the asset).
        readonly List<SpawnTableSO.Entry> _entries = new List<SpawnTableSO.Entry>();
        float[] _accumulators = new float[0];
        float _nextSweep;
        SwarmBody _player;
        bool _warnedFallback;

        /// <summary>Debug: spawn a regular enemy of a type next to a position.</summary>
        public Enemy DebugSpawn(EnemyTypeSO type, Vector2 position)
        {
            return Enemy.Create(BuildRegular(type, CurrentMilestone()), position);
        }

        /// <summary>Debug / tools: every enemy type of the table (including the support cell).</summary>
        public List<EnemyTypeSO> AllTypes()
        {
            EnsureTable();
            var list = new List<EnemyTypeSO>();
            foreach (var e in _entries)
                if (e != null && e.type != null) list.Add(e.type);
            return list;
        }

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

            if (_accumulators.Length != _entries.Count)
                _accumulators = new float[_entries.Count];

            int count = Enemy.CountNonBoss();

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
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
            Enemy.Create(BuildRegular(type, milestone), RandomRingPosition());
        }

        /// <summary>The spec of a regular enemy for a milestone (endless mode adds DEF).</summary>
        public EnemySpec BuildRegular(EnemyTypeSO type, int milestone)
        {
            var spec = EnemySpec.FromType(type, milestone);

            // GDD: in endless mode (milestone 7 and above) every non-boss enemy has DEF = milestone - 5 when that is higher.
            if (milestone >= 7) spec.defense = Mathf.Max(spec.defense, milestone - 5);
            return spec;
        }

        // ---------------- Used by the resource point director ----------------

        /// <summary>Types that currently spawn (rate above 0) at this milestone.</summary>
        public void TypesAt(int milestone, List<EnemyTypeSO> into)
        {
            EnsureTable();
            into.Clear();
            foreach (var e in _entries)
                if (e != null && e.type != null && e.RateAt(milestone) > 0f && e.type.behavior != EnemyBehavior.Support)
                    into.Add(e.type);
            if (into.Count == 0 && _entries.Count > 0 && _entries[0].type != null) into.Add(_entries[0].type);
        }

        /// <summary>A random normal type from the spawn row, weighted by spawn rate.</summary>
        public EnemyTypeSO PickNormalType(int milestone)
        {
            EnsureTable();
            float total = 0f;
            foreach (var e in _entries)
                if (e != null && e.type != null && e.type.behavior != EnemyBehavior.Support) total += e.RateAt(milestone);
            if (total <= 0f) return _entries.Count > 0 ? _entries[0].type : null;

            float r = Random.value * total;
            foreach (var e in _entries)
            {
                if (e == null || e.type == null || e.type.behavior == EnemyBehavior.Support) continue;
                r -= e.RateAt(milestone);
                if (r <= 0f) return e.type;
            }
            return _entries[0].type;
        }

        /// <summary>The toughest type (highest max HP) in the spawn row.</summary>
        public EnemyTypeSO ToughestType(int milestone)
        {
            var list = new List<EnemyTypeSO>();
            TypesAt(milestone, list);
            EnemyTypeSO best = null;
            foreach (var t in list)
                if (best == null || t.maxHp > best.maxHp) best = t;
            return best;
        }

        /// <summary>Spawns a guard at a position (bypasses the 50-enemy cap). Elites get hp/damage multipliers.</summary>
        public Enemy SpawnGuard(EnemyTypeSO type, int milestone, Vector2 position, Vector2 pointCenter, float hpMultiplier, float damageMultiplier)
        {
            var spec = BuildRegular(type, milestone);
            spec.maxHp *= hpMultiplier;
            spec.damageMultiplier = damageMultiplier;
            var e = Enemy.Create(spec, position);
            e.MakeGuard(pointCenter);
            return e;
        }

        public Vector2 PlayerPosition => _player != null ? (Vector2)_player.transform.position : Vector2.zero;

        public Enemy SpawnBoss(int n, WeaponSO forcedWeapon = null, Vector2? at = null)
        {
            float maxHp = n * hpPerMilestone;
            WeaponSO weapon = forcedWeapon != null ? forcedWeapon : PickBossWeapon();
            bool ranged = weapon != null && (weapon.type == WeaponType.Gun || weapon.type == WeaponType.SpreadShot);
            var spec = new EnemySpec
            {
                name = "Boss " + n,
                color = bossColor,
                maxHp = maxHp,
                defense = Mathf.Floor(n * defensePerMilestone),
                speed = bossSpeed,
                contactDps = bossContactDps,
                damageMultiplier = 1f + bossDamageGrowth * (n - 1),
                behavior = ranged ? EnemyBehavior.KeepDistance : EnemyBehavior.Chase,
                preferredRange = 6f,
                weapon = weapon,
                scoreValue = bossScorePerMilestone * n,
                expValue = 50 * n, // PLACEHOLDER
                isBoss = true,
                milestone = n
            };
            return Enemy.Create(spec, at ?? RandomRingPosition());
        }

        /// <summary>
        /// A random boss weapon from Ram, Jaw, Gun, Whip, Spread Shot, Spore Cloud and Mine (GDD V0.2 section 7.2).
        /// Scene assets that predate V0.2 only list three, so the missing types come from the built-in defaults.
        /// </summary>
        WeaponSO PickBossWeapon()
        {
            var pool = new List<WeaponSO>();
            if (bossWeapons != null)
                foreach (var w in bossWeapons)
                    if (w != null) pool.Add(w);
            foreach (var def in WeaponDefaults.BossPool())
            {
                bool have = false;
                foreach (var w in pool)
                    if (w.type == def.type) { have = true; break; }
                if (!have) pool.Add(def);
            }
            return pool[Random.Range(0, pool.Count)];
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
                if (e == null || e.IsBoss || e.IsGuard) continue;     // guards never despawn by distance
                if (((Vector2)e.transform.position - p).sqrMagnitude > limitSqr) e.Despawn();
            }
        }

        int CurrentMilestone()
        {
            return RunDirector.Instance != null ? RunDirector.Instance.Milestone : 0;
        }

        void EnsureTable()
        {
            if (_entries.Count > 0) return;

            if (table == null || table.entries == null || table.entries.Length == 0)
            {
                if (!_warnedFallback)
                {
                    _warnedFallback = true;
                    Debug.LogWarning("EnemySpawner: no Spawn Table assigned, using built-in GDD defaults. " +
                                     "Run Cellstorm > Data > Regenerate data to create editable assets.");
                }
                table = EnemyDefaults.CreateTable(EnemyDefaults.CreateTypes());
            }

            bool hasSupport = false;
            foreach (var e in table.entries)
            {
                if (e == null || e.type == null) continue;
                _entries.Add(e);
                if (e.type.behavior == EnemyBehavior.Support) hasSupport = true;
            }

            // Tables made before V0.2 have no support cell: add it with the GDD rates (until the data is regenerated).
            if (!hasSupport)
            {
                _entries.Add(new SpawnTableSO.Entry { type = EnemyDefaults.CreateSupport(), ratesPerSecond = EnemyDefaults.SupportRates });
            }
        }

        /// <summary>Debug text: current spawn rates per second.</summary>
        public string DescribeRates()
        {
            EnsureTable();
            int milestone = CurrentMilestone();
            var sb = new StringBuilder();
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e == null || e.type == null) continue;
                if (sb.Length > 0) sb.Append("   ");
                sb.Append(e.type.displayName).Append(' ').Append(e.RateAt(milestone).ToString("0.0#"));
            }
            return sb.ToString();
        }
    }
}
