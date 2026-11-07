using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Key press: a dash window (0.3 s) at 5 x the moving speed. Every hostile swarm touched during the window takes
    /// the Ram damage once (30 by default, reduced by its DEF); hostile weapon cells in the way are damaged too.
    /// Ram costs no HP, ignores Speed for its timing and does not count as the deployed HP-cost weapon.
    /// </summary>
    public class RamAbility : Ability
    {
        readonly HashSet<GameObject> _hit = new HashSet<GameObject>();
        readonly List<Collider2D> _buf = new List<Collider2D>();
        float _timer;
        float _window = 0.3f;

        public RamAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        public override float Progress
        {
            get
            {
                if (Phase == AbilityPhase.Executing) return 1f - Mathf.Clamp01(_timer / Mathf.Max(0.01f, _window));
                return Phase == AbilityPhase.Cooldown ? CooldownProgress : 0f;
            }
        }

        public override void Tick(float dt, AbilityInput input)
        {
            switch (Phase)
            {
                case AbilityPhase.Ready:
                    if (input.pressed) BeginDash();
                    break;

                case AbilityPhase.Executing:
                    _timer -= dt;
                    Strike();
                    if (_timer <= 0f)
                    {
                        Owner.Host.EndDash();
                        StartCooldown(CooldownBase);   // Ram ignores Speed (Quick Ram and Quick Hands-style cards still apply)
                    }
                    break;

                case AbilityPhase.Cooldown:
                    TickCooldown(dt);
                    break;
            }
        }

        void BeginDash()
        {
            float speed = SpeedMath.MoveSpeed(Owner.Stats.Speed) * Mathf.Max(0.1f, P(Param.DashSpeed, Data.dashMultiplier));
            _hit.Clear();
            _window = Mathf.Max(0.05f, P(Param.DashWindow, Data.executeTime));
            _timer = _window;
            Phase = AbilityPhase.Executing;
            Owner.Host.BeginDash(Owner.Host.AimDirection, speed, _timer);
            Strike();
        }

        void Strike()
        {
            Vector2 center = Owner.Position;
            float reach = Owner.Radius + 0.08f;
            float damage = DamageNow;
            var status = StatusFor(default, center);

            var filter = ContactFilter2D.noFilter;
            _buf.Clear();
            Physics2D.OverlapCircle(center, reach, filter, _buf);

            for (int i = 0; i < _buf.Count; i++)
            {
                var col = _buf[i];
                if (col == null) continue;
                var rb = col.attachedRigidbody;
                var go = rb != null ? rb.gameObject : col.gameObject;
                if (!go.TryGetComponent<SwarmCombat>(out var target)) continue;
                if (target.Faction == Owner.Faction || target.Stats.IsDead) continue;
                if (!_hit.Add(go)) continue;       // each enemy is hit once per dash

                Vector2 point = col.ClosestPoint(center);
                target.Stats.TakeDamage(new DamageInfo(damage, DamageType.Ram, Owner.Faction, point, status));
                target.ReactToHit(point);
            }

            // Weapon cells have no colliders; query them directly.
            WeaponCellGroup.DamageCircle(center, reach, damage, Owner.Faction);
        }

        public override void Cancel()
        {
            if (Phase == AbilityPhase.Executing && Owner.Host != null) Owner.Host.EndDash();
            base.Cancel();
        }
    }
}
