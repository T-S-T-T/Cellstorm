using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Shared flow for Jaw and Gun (GDD V0.1.1 section 4.5): hold the key to deploy the weapon cells into a shape
    /// (Shaping), wait primed once complete, release to execute, then the cells fly back and the weapon cools down.
    ///
    /// - Needs Hp >= cost (x% of MAX HP) to start and no other HP-cost weapon deployed (Ram does not count).
    /// - Every tick, Hp below the cost counts as releasing the key.
    /// - The cells hold the cost; cells that die cost real HP and set the deploy progress back by 1/N.
    /// - Deploy time and cooldown scale with Speed (SpeedMath).
    /// </summary>
    public abstract class ShapedAbility : Ability
    {
        WeaponCellGroup _group;
        float _progress;
        float _executeTimer;
        float _deployCost;
        bool _forceRelease;
        StatModifier _executeMod;

        protected float Closure { get; private set; }

        public WeaponCellGroup Group => _group;
        public int CellCount => _group != null ? _group.Count : 0;
        public float Defense => Owner.Stats.Defense + P(Param.CellDef, 0f);   // Tough Cells
        public Faction Faction => Owner.Faction;

        public override bool HoldsHp =>
            Phase == AbilityPhase.Shaping || Phase == AbilityPhase.Primed || Phase == AbilityPhase.Executing;

        protected ShapedAbility(AbilityController owner, WeaponSO data) : base(owner, data)
        {
            _group = new WeaponCellGroup(this, owner);
        }

        public override float TurnPenalty => HoldsHp ? Data.turnRateWhileActive : 1f;

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

        /// <summary>Seconds to deploy the weapon, scaled by Speed.</summary>
        public float ShapeTime => SpeedMath.Scaled(P(Param.ShapeTime, Data.shapeTime), Owner.Stats.Speed);
        /// <summary>HP the weapon costs right now: x% of max HP.</summary>
        public float CostNow => Owner.HpCostFraction(Data) * Owner.Stats.MaxHp;

        /// <summary>Seconds the execute phase lasts (a weapon can lengthen it: Second Bite).</summary>
        protected virtual float ExecuteTime => Data.executeTime;
        /// <summary>Seconds over which the shape closes or sweeps (Closure goes 0 to 1).</summary>
        protected virtual float ClosureTime => ExecuteTime;
        /// <summary>How far the shape reaches beyond the body edge (after cards). Sets the cell speed.</summary>
        protected virtual float ReachUnits => Data.coneRange;
        /// <summary>Seconds since the execute phase began.</summary>
        protected float ExecuteElapsed => ExecuteTime - _executeTimer;

        /// <summary>World position of weapon cell i of n for the current aim.</summary>
        protected abstract Vector2 FormationPoint(int index, int count, Vector2 center, float radius, Vector2 aim);
        protected abstract void OnExecuteStart();
        protected virtual void OnExecuteTick(float dt) { }

        public Vector2 GetFormationPoint(int index, int count)
        {
            return FormationPoint(index, count, Owner.Position, Owner.Radius, Owner.Host.AimDirection);
        }

        public override void Tick(float dt, AbilityInput input)
        {
            switch (Phase)
            {
                case AbilityPhase.Ready:
                    if (input.held && Owner.CanDeploy(this) && Owner.Stats.Hp >= CostNow && CostNow > 0f) BeginShaping();
                    break;

                case AbilityPhase.Shaping:
                    if (!StillHeld(input))
                    {
                        // Released before the shape was complete (or the body can no longer pay): cancel.
                        _group.Release();
                        StartCooldown(Data.cancelCooldown);
                        break;
                    }
                    _progress = Mathf.Min(1f, _progress + dt / Mathf.Max(0.05f, ShapeTime));
                    if (_progress >= 1f) Phase = AbilityPhase.Primed;
                    break;

                case AbilityPhase.Primed:
                    if (!StillHeld(input)) BeginExecute();
                    break;

                case AbilityPhase.Executing:
                    _executeTimer -= dt;
                    Closure = ClosureTime > 0f ? Mathf.Clamp01(ExecuteElapsed / ClosureTime) : 1f;
                    OnExecuteTick(dt);
                    if (_executeTimer <= 0f) EndExecute();
                    break;

                case AbilityPhase.Cooldown:
                    TickCooldown(dt);
                    break;
            }

            UpdateCells(dt);
        }

        bool StillHeld(AbilityInput input)
        {
            if (_forceRelease) return false;
            if (!input.held) return false;
            return Owner.Stats.Hp >= _deployCost;     // below the cost behaves as if the key was released
        }

        void BeginShaping()
        {
            _deployCost = CostNow;
            _progress = 0f;
            Closure = 0f;
            _forceRelease = false;
            Owner.PurgeReturningCells();

            _group.Begin(Owner.WeaponCellCount, _deployCost, Owner.WeaponCellRadius);
            Phase = AbilityPhase.Shaping;
        }

        void BeginExecute()
        {
            Phase = AbilityPhase.Executing;
            _executeTimer = ExecuteTime;
            Closure = 0f;
            OnExecuteStart();
            if (ExecuteTime <= 0f) EndExecute();
        }

        void EndExecute()
        {
            RemoveExecuteModifier();
            _group.Release();
            StartCooldown(SpeedMath.Scaled(CooldownBase, Owner.Stats.Speed));
        }

        /// <summary>A weapon cell died: the deploy progress drops by one cell's share.</summary>
        public void NotifyCellLost()
        {
            if (Phase != AbilityPhase.Shaping && Phase != AbilityPhase.Primed) return;
            _progress = Mathf.Max(0f, _progress - 1f / Mathf.Max(1, _group.Count));
            if (Phase == AbilityPhase.Primed) Phase = AbilityPhase.Shaping;
        }

        /// <summary>The body could not pay for a replacement cell: release the weapon.</summary>
        public void NotifyCannotPay()
        {
            _forceRelease = true;
        }

        /// <summary>A stun releases the weapon: the cells return, nothing fires.</summary>
        public override void OnStunned()
        {
            if (Phase == AbilityPhase.Shaping || Phase == AbilityPhase.Primed)
            {
                _group.Release();
                Closure = 0f;
                StartCooldown(Data.cancelCooldown);
            }
        }

        public void PurgeReturning()
        {
            _group.PurgeReturning();
        }

        public override void Cancel()
        {
            RemoveExecuteModifier();
            _group.ClearImmediate();
            Closure = 0f;
            _forceRelease = false;
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

        void UpdateCells(float dt)
        {
            if (_group.Count == 0) return;

            float ease = _progress * (2f - _progress);
            if (Phase == AbilityPhase.Primed || Phase == AbilityPhase.Executing) ease = 1f;

            // Cells never move slower than 1.5 x the swarm's moving speed (GDD V0.1.1 section 4.3).
            float reach = Owner.Radius + ReachUnits + 1f;
            float cellSpeed = Mathf.Max(reach / Mathf.Max(0.05f, ShapeTime), 1.5f * SpeedMath.MoveSpeed(Owner.Stats.Speed));
            _group.UpdateCells(dt, ease, Phase == AbilityPhase.Primed, cellSpeed);
        }

        protected static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        /// <summary>Deterministic 0..1 value per cell index, for stable thickness inside a shape.</summary>
        protected static float Hash01(int i)
        {
            float x = Mathf.Sin(i * 12.9898f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }
    }
}
