using System.Text;
using Cellstorm.Core;
using Cellstorm.Enemies;
using Cellstorm.Progression;
using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.Meta
{
    /// <summary>
    /// Records the run when it ends: death, or leaving the run (new run, quit to menu, closing the game).
    /// Winning does not end the run because you may continue in endless mode.
    /// </summary>
    public class RunRecorder : MonoBehaviour
    {
        public static RunRecorder Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        RunRecord _snapshot = new RunRecord();
        bool _finalized;
        bool _won;
        int _enemies;
        int _bosses;
        float _nextCapture;

        public RunSummary Summary { get; private set; }
        public bool Finalized => _finalized;

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {
            Enemy.Killed += OnEnemyKilled;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnEnemyKilled;
        }

        void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnStateChanged;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnStateChanged;
            if (Instance == this) Instance = null;
            if (!_finalized) FinalizeRun(useSnapshot: true);
        }

        void OnEnemyKilled(Enemy e)
        {
            _enemies++;
            if (e.IsBoss) _bosses++;
        }

        void OnStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Won) _won = true;
            if (current == GameState.Dead) FinalizeRun();
        }

        void Update()
        {
            if (_finalized || Time.unscaledTime < _nextCapture) return;
            _nextCapture = Time.unscaledTime + 0.5f;
            _snapshot = Capture();
        }

        /// <summary>Current run as a record, without saving it.</summary>
        public RunSummary Preview()
        {
            return new RunSummary { record = _finalized ? Summary.record : Capture(), saved = false };
        }

        public RunSummary FinalizeRun(bool useSnapshot = false)
        {
            if (_finalized) return Summary;
            _finalized = true;

            var record = useSnapshot ? _snapshot : Capture();
            record.won = _won;
            var newIds = SaveService.RecordRun(record);

            Summary = new RunSummary { record = record, saved = true };
            foreach (var id in newIds) Summary.unlockedNames.Add(UnlockCatalog.NameFor(id));
            return Summary;
        }

        RunRecord Capture()
        {
            var run = RunDirector.Instance;
            var build = PlayerBuild.Instance;

            var r = new RunRecord
            {
                score = run != null ? run.Score : _snapshot.score,
                runTime = run != null ? run.Elapsed : _snapshot.runTime,
                milestone = run != null ? run.Milestone : _snapshot.milestone,
                endless = run != null && run.Endless,
                won = _won,
                loadout = RunSetup.SelectedLoadout,
                enemiesDefeated = _enemies,
                bossesDefeated = _bosses,
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            if (build != null && build.Stats != null)
            {
                var s = build.Stats;
                r.maxCells = s.MaxCells;
                r.regen = s.CellRegen;
                r.defense = s.Defense;
                r.speed = s.Speed;
                r.weapons = DescribeAbilities(build.Abilities);
            }
            else
            {
                r.maxCells = _snapshot.maxCells;
                r.regen = _snapshot.regen;
                r.defense = _snapshot.defense;
                r.speed = _snapshot.speed;
                r.weapons = _snapshot.weapons;
            }
            return r;
        }

        static string DescribeAbilities(AbilityController ac)
        {
            if (ac == null) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < AbilityController.SlotCount; i++)
            {
                var a = ac.GetSlot(i);
                if (a == null) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(a.Data.displayName);
            }
            return sb.ToString();
        }
    }
}
