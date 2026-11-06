using System;
using System.Collections.Generic;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>
    /// Stats and health for any swarm (player, enemies, bosses). HP is a float: Hp (current), MaxHp, HpRegen,
    /// plus DEF and Speed. Base values come from an optional StatBlockSO (loadouts) or the inline fields.
    /// Temporary effects go through AddModifier; permanent upgrades through AddToBase.
    /// Damage goes through TakeDamage(DamageInfo): DEF is applied once, here (GDD V0.1.1 section 4.3).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SwarmStats : MonoBehaviour, IDamageable
    {
        [SerializeField] StatBlockSO statBlock;

        [Header("Base stats (used when no Stat Block is assigned)")]
        [SerializeField, Min(1)] float baseMaxHp = 100f;
        [SerializeField, Min(0)] float baseHpRegen = 1f;
        [SerializeField, Min(0)] float baseDefense = 2f;
        [SerializeField, Min(0)] float baseSpeed = 2f;

        readonly List<StatModifier> _mods = new List<StatModifier>();
        float _hp;
        float _heldHp;
        bool _initialized;

        /// <summary>Raised whenever the current HP changes (new HP).</summary>
        public event Action<float> HpChanged;
        /// <summary>Raised when HP is lost (amount actually lost, after DEF).</summary>
        public event Action<float> Damaged;
        public event Action Died;

        public bool IsDead { get; private set; }

        /// <summary>Test helper (god mode): ignores all damage while true.</summary>
        public bool Invulnerable { get; set; }

        public float Hp { get { EnsureInitialized(); return _hp; } }
        public float MaxHp => Mathf.Max(1f, Get(StatType.MaxHp));
        public float HpRegen => Get(StatType.HpRegen);
        public float Defense => Get(StatType.Defense);
        public float Speed => Get(StatType.Speed);

        /// <summary>HP currently held by deployed weapon cells (V0.1.1 Phase 4). Always 0 until then.</summary>
        public float HeldHp => _heldHp;
        /// <summary>HP of the body itself: Hp minus what the weapon cells hold.</summary>
        public float BodyHp => Mathf.Max(0f, Hp - _heldHp);

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
                baseMaxHp = statBlock.maxHp;
                baseHpRegen = statBlock.hpRegen;
                baseDefense = statBlock.defense;
                baseSpeed = statBlock.speed;
            }
            _hp = Mathf.Max(1f, baseMaxHp);
        }

        /// <summary>Set base stats from code (used for enemies created at runtime and by the loadout picker).</summary>
        public void Configure(float maxHp, float regen, float defense, float speed, float startHp = -1f)
        {
            EnsureInitialized();
            baseMaxHp = Mathf.Max(1f, maxHp);
            baseHpRegen = Mathf.Max(0f, regen);
            baseDefense = Mathf.Max(0f, defense);
            baseSpeed = Mathf.Max(0f, speed);
            IsDead = false;
            _heldHp = 0f;

            float start = startHp >= 0f ? startHp : MaxHp;
            SetHpInternal(Mathf.Clamp(start, 0.01f, MaxHp));
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
                case StatType.MaxHp: return baseMaxHp;
                case StatType.HpRegen: return baseHpRegen;
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
                case StatType.MaxHp: baseMaxHp = Mathf.Max(1f, baseMaxHp + amount); break;
                case StatType.HpRegen: baseHpRegen = Mathf.Max(0f, baseHpRegen + amount); break;
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

            // Max HP can drop (Dense Cells): HP never stays above it.
            float max = MaxHp;
            if (_hp > max) SetHpInternal(max);
        }

        /// <summary>HP regen is applied every physics tick so the bar moves smoothly. Never above max HP.</summary>
        void FixedUpdate()
        {
            if (IsDead) return;
            float regen = HpRegen;
            if (regen <= 0f) return;

            float max = MaxHp;
            if (_hp >= max) return;
            SetHpInternal(Mathf.Min(max, _hp + regen * Time.fixedDeltaTime));
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            SetHpInternal(Mathf.Min(MaxHp, Hp + amount));
        }

        /// <summary>The one damage entry point: DEF reduces the hit once, here. Returns the HP actually lost.</summary>
        public float TakeDamage(DamageInfo info)
        {
            if (IsDead || Invulnerable || info.amount <= 0f) return 0f;

            float final = info.type == DamageType.True ? info.amount : DefCurve.Apply(info.amount, Defense);
            return LoseHp(final);
        }

        /// <summary>Shortcut for a raw hit that DEF reduces (contact, explosions, test damage).</summary>
        public float TakeDamage(float rawAmount, DamageType type = DamageType.Contact)
        {
            return TakeDamage(new DamageInfo(rawAmount, type, Faction.Enemy, transform.position));
        }

        /// <summary>HP loss that ignores DEF (cost of a lost weapon cell, debug kill).</summary>
        public float TakeTrueDamage(float amount)
        {
            if (IsDead || Invulnerable || amount <= 0f) return 0f;
            return LoseHp(amount);
        }

        float LoseHp(float amount)
        {
            float dealt = Mathf.Min(amount, _hp);
            SetHpInternal(_hp - dealt);
            Damaged?.Invoke(dealt);

            if (_hp <= 0.0001f) Die();
            return dealt;
        }

        /// <summary>Clamped to 0..MaxHp. Setting 0 kills the swarm.</summary>
        public void SetHp(float value)
        {
            if (IsDead) return;
            value = Mathf.Clamp(value, 0f, MaxHp);
            SetHpInternal(value);
            if (_hp <= 0.0001f) Die();
        }

        /// <summary>Test helper: sets base max HP and current HP together.</summary>
        public void DebugSetMaxAndHp(float value)
        {
            EnsureInitialized();
            baseMaxHp = Mathf.Max(1f, value);
            SetHp(baseMaxHp);
        }

        /// <summary>Weapon cells (Phase 4) set this. Clamped so 0 &lt;= HeldHp &lt;= Hp.</summary>
        public void SetHeldHp(float value)
        {
            _heldHp = Mathf.Clamp(value, 0f, Hp);
        }

        /// <summary>Debug check of the GDD invariant 0 &lt;= HeldHp &lt;= Hp &lt;= MaxHp.</summary>
        public bool InvariantsHold()
        {
            const float eps = 0.001f;
            return _heldHp >= -eps && _heldHp <= Hp + eps && Hp <= MaxHp + eps;
        }

        void SetHpInternal(float value)
        {
            if (Mathf.Approximately(value, _hp)) return;
            _hp = value;
            if (_heldHp > _hp) _heldHp = _hp;
            HpChanged?.Invoke(_hp);
        }

        void Die()
        {
            if (IsDead) return;
            IsDead = true;
            Died?.Invoke();
        }
    }
}
