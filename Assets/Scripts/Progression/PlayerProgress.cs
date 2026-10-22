using System.Collections.Generic;
using Cellstorm.Core;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Progression
{
    /// <summary>
    /// EXP, level-ups, rerolls and the 3-option selection. Level 1 needs 10 EXP, every further level needs
    /// 1.1x the previous one, with no level cap. The level-up screen (LevelUpScreen) reads and drives this.
    /// </summary>
    public class PlayerProgress : MonoBehaviour
    {
        public static PlayerProgress Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        [SerializeField] UpgradePoolSO pool;
        [SerializeField, Min(1)] int optionCount = 3;
        [SerializeField] float firstLevelExp = 10f;
        [SerializeField] float levelGrowth = 1.1f;

        readonly List<UpgradeSO> _options = new List<UpgradeSO>();
        readonly List<UpgradeSO> _lastOptions = new List<UpgradeSO>();
        UpgradeSO[] _fallbackPool;
        PlayerBuild _build;

        public int LevelsGained { get; private set; }
        public int Exp { get; private set; }
        public int Rerolls { get; private set; }
        public int PendingLevelUps { get; private set; }
        public IReadOnlyList<UpgradeSO> Options => _options;

        public int ExpToNext => Mathf.CeilToInt(firstLevelExp * Mathf.Pow(levelGrowth, LevelsGained));

        UpgradeSO[] Pool
        {
            get
            {
                if (pool != null && pool.upgrades != null && pool.upgrades.Length > 0) return pool.upgrades;
                if (_fallbackPool == null)
                {
                    Debug.LogWarning("PlayerProgress: no Upgrade Pool assigned, using built-in GDD defaults. " +
                                     "Run Cellstorm > Phase 5 > Setup Game Scene to create editable assets.");
                    _fallbackPool = UpgradeDefaults.CreateAll(WeaponDefaults.Jaw, WeaponDefaults.Gun, WeaponDefaults.Hardening);
                }
                return _fallbackPool;
            }
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;
            Exp += amount;
            while (Exp >= ExpToNext)
            {
                Exp -= ExpToNext;
                LevelsGained++;
                PendingLevelUps++;
            }
        }

        public void AddRerolls(int amount)
        {
            Rerolls += Mathf.Max(0, amount);
        }

        public UpgradeSO FindUpgrade(string id)
        {
            foreach (var u in Pool)
                if (u != null && u.id == id) return u;
            return null;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying || PendingLevelUps <= 0) return;

            if (_build == null) _build = FindFirstObjectByType<PlayerBuild>();
            if (_build == null || _build.Stats.IsDead) return;

            _lastOptions.Clear();
            RollOptions();

            if (_options.Count == 0)
            {
                PendingLevelUps--; // nothing eligible: skip this level-up instead of getting stuck
                return;
            }
            gm.SetState(GameState.LevelUp);
        }

        void RollOptions()
        {
            var eligible = new List<UpgradeSO>();
            foreach (var u in Pool)
                if (_build.IsEligible(u)) eligible.Add(u);

            // On rerolls, avoid showing the same options again when there are enough alternatives.
            if (_lastOptions.Count > 0 && eligible.Count - _lastOptions.Count >= optionCount)
                eligible.RemoveAll(u => _lastOptions.Contains(u));

            _options.Clear();
            int picks = Mathf.Min(optionCount, eligible.Count);
            for (int i = 0; i < picks; i++)
            {
                int index = Random.Range(0, eligible.Count);
                _options.Add(eligible[index]);
                eligible.RemoveAt(index);
            }
        }

        /// <summary>Spend one reroll to get a new set of options.</summary>
        public bool Reroll()
        {
            if (Rerolls <= 0 || PendingLevelUps <= 0) return false;
            Rerolls--;
            _lastOptions.Clear();
            _lastOptions.AddRange(_options);
            RollOptions();
            return true;
        }

        /// <summary>Take an option. For weapons and skills with no free slot, pass the slot to replace.</summary>
        public bool Choose(UpgradeSO u, int replaceSlot = -1)
        {
            if (u == null || !_options.Contains(u) || _build == null) return false;
            if (!_build.Apply(u, replaceSlot)) return false;

            PendingLevelUps--;
            _lastOptions.Clear();

            var gm = GameManager.Instance;
            if (PendingLevelUps > 0)
            {
                RollOptions();
                if (_options.Count == 0)
                {
                    PendingLevelUps = 0;
                    if (gm != null) gm.SetState(GameState.Playing);
                }
            }
            else
            {
                _options.Clear();
                if (gm != null) gm.SetState(GameState.Playing);
            }
            return true;
        }
    }
}
