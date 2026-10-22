using System;
using System.Collections.Generic;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>
    /// Stats and health for any swarm (player, enemies, bosses). Cells are the health.
    /// Base values come from an optional StatBlockSO (loadouts, enemy types) or the inline fields.
    /// Temporary effects go through AddModifier; permanent upgrades through AddToBase.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SwarmStats : MonoBehaviour
    {
        [SerializeField] StatBlockSO statBlock;

        [Header("Base stats (used when no Stat Block is assigned)")]
        [SerializeField, Min(1)] float baseMaxCells = 5f;
        [SerializeField, Min(0)] float baseCellRegen = 1f;
        [SerializeField, Min(0)] float baseDefense = 2f;
        [SerializeField, Min(0)] float baseSpeed = 2f;

        readonly List<StatModifier> _mods = new List<StatModifier>();
        int _cells;
        float _regenAcc;
        float _damageAcc;
        bool _initialized;

        /// <summary>Raised whenever the current cell count changes (new count).</summary>
        public event Action<int> CellsChanged;
        /// <summary>Raised when cells are lost (amount actually lost).</summary>
        public event Action<int> Damaged;
        public event Action Died;

        public bool IsDead { get; private set; }

        /// <summary>Test helper (god mode): ignores all damage while true.</summary>
        public bool Invulnerable { get; set; }

        public int Cells { get { EnsureInitialized(); return _cells; } }
        public int MaxCells => Mathf.Max(1, Mathf.RoundToInt(Get(StatType.MaxCells)));
        public float CellRegen => Get(StatType.CellRegen);
        public float Defense => Get(StatType.Defense);
        public float Speed => Get(StatType.Speed);

        public float BaseDefense { get { EnsureInitialized(); return baseDefense; } }
        public int ModifierCount => _mods.Count;

        void Awake()
        {
            EnsureInitialized();
        }

        void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            if (statBlock != null)
            {
                baseMaxCells = statBlock.maxCells;
                baseCellRegen = statBlock.cellRegen;
                baseDefense = statBlock.defense;
                baseSpeed = statBlock.speed;
            }
            _cells = Mathf.Max(1, Mathf.RoundToInt(baseMaxCells));
        }

        /// <summary>Set base stats from code (used for enemies created at runtime).</summary>
        public void Configure(int maxCells, float regen, float defense, float speed, int startCells = -1)
        {
            EnsureInitialized();
            baseMaxCells = Mathf.Max(1, maxCells);
            baseCellRegen = Mathf.Max(0f, regen);
            baseDefense = Mathf.Max(0f, defense);
            baseSpeed = Mathf.Max(0f, speed);
            IsDead = false;

            int start = startCells >= 0 ? startCells : maxCells;
            SetCellsInternal(Mathf.Clamp(start, 1, MaxCells));
        }

        // ---------------- Stat calculation ----------------

        public float Get(StatType stat)
        {
            EnsureInitialized();

            float add = 0f;
            float mult = 1f;
            for (int i = 0; i < _mods.Count; i++)
            {
                var m = _mods[i];
                if (m.Stat != stat) continue;
                if (m.Op == ModifierOp.Add) add += m.Value;
                else mult *= m.Value;
            }
            return Mathf.Max(0f, (BaseOf(stat) + add) * mult);
        }

        float BaseOf(StatType stat)
        {
            switch (stat)
            {
                case StatType.MaxCells: return baseMaxCells;
                case StatType.CellRegen: return baseCellRegen;
                case StatType.Defense: return baseDefense;
                default: return baseSpeed;
            }
        }

        /// <summary>Permanent change to a base stat (level-up upgrades).</summary>
        public void AddToBase(StatType stat, float amount)
        {
            EnsureInitialized();
            switch (stat)
            {
                case StatType.MaxCells: baseMaxCells = Mathf.Max(1f, baseMaxCells + amount); break;
                case StatType.CellRegen: baseCellRegen = Mathf.Max(0f, baseCellRegen + amount); break;
                case StatType.Defense: baseDefense = Mathf.Max(0f, baseDefense + amount); break;
                default: baseSpeed = Mathf.Max(0f, baseSpeed + amount); break;
            }
        }

        // ---------------- Modifiers ----------------

        /// <param name="duration">Seconds, or a negative value for permanent.</param>
        public StatModifier AddModifier(StatType stat, ModifierOp op, float value, float duration = -1f, object source = null)
        {
            var m = new StatModifier(stat, op, value, duration, source);
            _mods.Add(m);
            return m;
        }

        public void RemoveModifier(StatModifier m)
        {
            if (m != null) _mods.Remove(m);
        }

        public void RemoveModifiersFrom(object source)
        {
            for (int i = _mods.Count - 1; i >= 0; i--)
                if (_mods[i].Source == source) _mods.RemoveAt(i);
        }

        // ---------------- Health ----------------

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || IsDead) return;

            for (int i = _mods.Count - 1; i >= 0; i--)
            {
                var m = _mods[i];
                if (!m.Timed) continue;
                m.Remaining -= dt;
                if (m.Remaining <= 0f) _mods.RemoveAt(i);
            }

            int max = MaxCells;
            if (_cells > max) SetCellsInternal(max);

            float regen = CellRegen;
            if (_cells < max && regen > 0f)
            {
                _regenAcc += regen * dt;
                int whole = (int)_regenAcc;
                if (whole > 0)
                {
                    _regenAcc -= whole;
                    Heal(whole);
                }
            }
            else
            {
                _regenAcc = 0f;
            }
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            SetCellsInternal(Mathf.Min(MaxCells, Cells + amount));
        }

        /// <summary>Remove cells immediately. Returns the amount actually removed.</summary>
        public int TakeDamage(int amount)
        {
            if (IsDead || Invulnerable || amount <= 0) return 0;

            int dealt = Mathf.Min(amount, _cells);
            SetCellsInternal(_cells - dealt);
            Damaged?.Invoke(dealt);

            if (_cells <= 0) Die();
            return dealt;
        }

        /// <summary>
        /// Continuous damage (contact). Fractions are carried over between calls so a rate of
        /// 5 cells/s removes 5 cells per second regardless of frame rate.
        /// </summary>
        public void ApplyDamageOverTime(float cellsPerSecond, float dt)
        {
            if (IsDead || cellsPerSecond <= 0f || dt <= 0f) return;

            _damageAcc += cellsPerSecond * dt;
            int whole = (int)_damageAcc;
            if (whole > 0)
            {
                _damageAcc -= whole;
                TakeDamage(whole);
            }
        }

        /// <summary>Clamped to 0..MaxCells. Setting 0 kills the swarm.</summary>
        public void SetCells(int value)
        {
            if (IsDead) return;
            value = Mathf.Clamp(value, 0, MaxCells);
            SetCellsInternal(value);
            if (_cells <= 0) Die();
        }

        /// <summary>Test helper: sets base max cells and current cells together.</summary>
        public void DebugSetMaxAndCells(int value)
        {
            EnsureInitialized();
            baseMaxCells = Mathf.Max(1, value);
            SetCells(Mathf.Max(1, value));
        }

        void SetCellsInternal(int value)
        {
            if (value == _cells) return;
            _cells = value;
            CellsChanged?.Invoke(_cells);
        }

        void Die()
        {
            if (IsDead) return;
            IsDead = true;
            Died?.Invoke();
        }
    }
}
