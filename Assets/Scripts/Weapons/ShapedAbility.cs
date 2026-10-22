using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Shared flow for Jaw and Gun: hold the key to move cells out of the body into a weapon shape
    /// (Shaping), wait primed once complete, release to execute, then return the cells and cool down.
    /// Releasing early cancels. Weapon cells are physical: a hostile swarm / bullet / bite with higher DEF
    /// destroys them, which costs real cells and sets the shape progress back.
    /// </summary>
    public abstract class ShapedAbility : Ability
    {
        public const int MaxVisualCells = 24;

        readonly List<WeaponCell> _cells = new List<WeaponCell>();
        float _progress;
        float _executeTimer;
        float _returnTimer;
        bool _returning;
        int _weight = 1;
        StatModifier _executeMod;

        protected float Closure { get; private set; }

        public int CellCount => _cells.Count;
        public float Defense => Owner.Stats.Defense;
        public Faction Faction => Owner.Faction;

        protected ShapedAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        public override float TurnPenalty
        {
            get
            {
                bool active = Phase == AbilityPhase.Shaping || Phase == AbilityPhase.Primed || Phase == AbilityPhase.Executing;
                return active ? Data.turnRateWhileActive : 1f;
            }
        }

        public override float Progress
        {
            get
            {
                switch (Phase)
                {
                    case AbilityPhase.Shaping: return _progress;
                    case AbilityPhase.Primed: return 1f;
                    case AbilityPhase.Executing: return Closure;
                    case AbilityPhase.Cooldown: return CooldownProgress;
                    default: return 0f;
                }
            }
        }

        /// <summary>World position of weapon cell i of n for the current aim.</summary>
        protected abstract Vector2 FormationPoint(int index, int count, Vector2 center, float radius, Vector2 aim);
        protected abstract void OnExecuteStart();
        protected virtual void OnExecuteTick(float dt) { }

        public override void Tick(float dt, AbilityInput input)
        {
            switch (Phase)
            {
                case AbilityPhase.Ready:
                    if (input.held && Owner.Stats.Cells >= 2) BeginShaping();
                    break;

                case AbilityPhase.Shaping:
                    if (!input.held)
                    {
                        // Released before the shape was complete: cancel.
                        ReturnCells();
                        StartCooldown(Data.cancelCooldown);
                        break;
                    }
                    _progress = Mathf.Min(1f, _progress + dt / Mathf.Max(0.01f, Data.shapeTime));
                    if (_progress >= 1f) Phase = AbilityPhase.Primed;
                    break;

                case AbilityPhase.Primed:
                    if (!input.held) BeginExecute();
                    break;

                case AbilityPhase.Executing:
                    _executeTimer -= dt;
                    Closure = Data.executeTime > 0f ? Mathf.Clamp01(1f - _executeTimer / Data.executeTime) : 1f;
                    OnExecuteTick(dt);
                    if (_executeTimer <= 0f) EndExecute();
                    break;

                case AbilityPhase.Cooldown:
                    TickCooldown(dt);
                    break;
            }

            UpdateCells(dt);
        }

        void BeginShaping()
        {
            // GDD: uses 10% of the cells. Rounded up, at least 1.
            int logical = Mathf.Max(1, Mathf.CeilToInt(Owner.Stats.Cells * Data.cellShare));
            int visual = Mathf.Min(logical, MaxVisualCells);
            _weight = Mathf.Max(1, Mathf.RoundToInt((float)logical / visual));

            ClearCells();
            _progress = 0f;
            Closure = 0f;
            _returning = false;

            Vector2 center = Owner.Position;
            for (int i = 0; i < visual; i++)
                _cells.Add(WeaponCell.Create(this, center, Owner.CellStyle, Owner.WeaponCellRadius));

            Phase = AbilityPhase.Shaping;
        }

        void BeginExecute()
        {
            Phase = AbilityPhase.Executing;
            _executeTimer = Data.executeTime;
            Closure = 0f;
            OnExecuteStart();
            if (Data.executeTime <= 0f) EndExecute();
        }

        void EndExecute()
        {
            RemoveExecuteModifier();
            ReturnCells();
            StartCooldown(Data.cooldown);
        }

        /// <summary>A hostile swarm, bullet or bite destroyed a weapon cell.</summary>
        public void NotifyCellBroken(WeaponCell cell)
        {
            if (_returning || (Phase != AbilityPhase.Shaping && Phase != AbilityPhase.Primed)) return;

            // The cell is really lost, and the shape has to be rebuilt.
            Owner.Stats.TakeDamage(_weight);
            _progress = Mathf.Max(0f, _progress - 1f / Mathf.Max(1, _cells.Count));
            if (Phase == AbilityPhase.Primed) Phase = AbilityPhase.Shaping;
            if (cell != null) cell.ResetToBody(Owner.Position);
        }

        public override void Cancel()
        {
            RemoveExecuteModifier();
            ClearCells();
            Closure = 0f;
            base.Cancel();
        }

        protected void AddExecuteModifier(StatType stat, ModifierOp op, float value)
        {
            RemoveExecuteModifier();
            _executeMod = Owner.Stats.AddModifier(stat, op, value, -1f, this);
        }

        void RemoveExecuteModifier()
        {
            if (_executeMod != null)
            {
                Owner.Stats.RemoveModifier(_executeMod);
                _executeMod = null;
            }
        }

        void ReturnCells()
        {
            _returning = true;
            _returnTimer = 0.3f;
        }

        void ClearCells()
        {
            for (int i = 0; i < _cells.Count; i++)
                if (_cells[i] != null) Object.Destroy(_cells[i].gameObject);
            _cells.Clear();
            _returning = false;
        }

        void UpdateCells(float dt)
        {
            if (_cells.Count == 0) return;

            Vector2 center = Owner.Position;
            float radius = Owner.Radius;
            Vector2 aim = Owner.Host.AimDirection;
            float ease = _progress * (2f - _progress);
            float follow = 1f - Mathf.Exp(-14f * dt);
            bool primed = Phase == AbilityPhase.Primed;

            for (int i = 0; i < _cells.Count; i++)
            {
                var c = _cells[i];
                if (c == null) continue;

                Vector2 target;
                if (_returning)
                {
                    target = center;
                }
                else
                {
                    Vector2 formation = FormationPoint(i, _cells.Count, center, radius, aim);
                    target = Vector2.Lerp(center, formation, ease);
                }
                c.MoveToward(target, follow, primed);
            }

            if (_returning)
            {
                _returnTimer -= dt;
                if (_returnTimer <= 0f) ClearCells();
            }
        }

        protected static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
