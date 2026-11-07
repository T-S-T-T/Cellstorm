using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Cells grow into a long tentacle cocked back at one side of the arc. On release it sweeps a 120 degree arc in 0.2 s,
    /// hitting everything in the arc once (80 damage by default) and pulling it toward the swarm (1.5 units).
    /// Cards: Long Lash (reach), Wide Arc, Barbed Lash (pull), Wide Reach, Heavy Hits ...
    /// </summary>
    public class WhipAbility : ShapedAbility
    {
        readonly List<Collider2D> _hits = new List<Collider2D>();
        readonly HashSet<GameObject> _lashed = new HashSet<GameObject>();

        public WhipAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        float Arc => Mathf.Clamp(P(Param.ArcAngle, Data.whipArc), 10f, 340f);
        float Range => Mathf.Max(0.5f, P(Param.Reach, Data.whipRange) * P(Param.Size, 1f));
        float Pull => Mathf.Max(0f, P(Param.PullDistance, Data.pullDistance));

        protected override float ReachUnits => Range;

        protected override Vector2 FormationPoint(int index, int count, Vector2 center, float radius, Vector2 aim)
        {
            float t = (index + 1f) / count;
            float half = Arc * 0.5f;
            Vector2 dir = Rotate(aim, -half + Arc * Closure);          // cocked at one side, sweeps across
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float wave = Mathf.Sin(t * 7f + Time.time * 9f) * 0.10f * (1f - Closure) * t;
            return center + dir * (radius + Range * t) + perp * wave;
        }

        protected override void OnExecuteStart()
        {
            _lashed.Clear();
            Lash();
        }

        void Lash()
        {
            Vector2 center = Owner.Position;
            Vector2 aim = Owner.Host.AimDirection;
            float reach = Owner.Radius + Range;
            float halfAngle = Arc * 0.5f;
            float damage = DamageNow;
            float pull = Pull;

            var filter = ContactFilter2D.noFilter;
            filter.useTriggers = true;
            _hits.Clear();
            Physics2D.OverlapCircle(center, reach, filter, _hits);

            for (int i = 0; i < _hits.Count; i++)
            {
                var col = _hits[i];
                if (col == null) continue;

                Vector2 closest = col.ClosestPoint(center);
                Vector2 to = closest - center;
                if (to.sqrMagnitude > 0.0001f && Vector2.Angle(aim, to) > halfAngle) continue;

                var rb = col.attachedRigidbody;
                var go = rb != null ? rb.gameObject : col.gameObject;

                if (go.TryGetComponent<Bullet>(out var bullet))
                {
                    if (bullet.Faction != Owner.Faction) bullet.Kill();
                    continue;
                }

                if (!go.TryGetComponent<SwarmCombat>(out var target)) continue;
                if (target.Faction == Owner.Faction || target.Stats.IsDead) continue;
                if (!_lashed.Add(go)) continue;                    // once per lash

                // Pull toward the swarm, but never past its edge.
                float targetR = col is CircleCollider2D cc ? cc.radius : 0.3f;
                float gap = Mathf.Max(0f, ((Vector2)go.transform.position - center).magnitude - Owner.Radius - targetR);
                var basePayload = new StatusPayload { knockDistance = -Mathf.Min(pull, gap) };
                var status = StatusFor(basePayload, center);

                target.Stats.TakeDamage(new DamageInfo(damage, DamageType.Bite, Owner.Faction, closest, status));
                target.ReactToHit(closest);
            }

            WeaponCellGroup.DamageCone(center, aim, halfAngle, reach, damage, Owner.Faction);
        }
    }
}
