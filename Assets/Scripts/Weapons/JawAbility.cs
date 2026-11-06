using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Cells form a V-shaped open mouth toward the aim direction. On release the mouth snaps shut for
    /// executeTime (0.1s) and hits everything in the cone once: enemy swarms take the weapon's damage (reduced by
    /// their DEF), enemy bullets are destroyed and enemy weapon cells are broken.
    /// </summary>
    public class JawAbility : ShapedAbility
    {
        readonly List<Collider2D> _hits = new List<Collider2D>();
        readonly HashSet<GameObject> _bitten = new HashSet<GameObject>();

        public JawAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        protected override Vector2 FormationPoint(int index, int count, Vector2 center, float radius, Vector2 aim)
        {
            int arms = Mathf.Max(1, (count + 1) / 2);
            int rank = index / 2;
            float side = (index % 2 == 0) ? 1f : -1f;
            float t = (rank + 1f) / arms;

            float half = Data.coneAngle * 0.5f * (1f - Closure);
            if (count == 1) half = 0f;

            Vector2 dir = Rotate(aim, side * half);
            return center + dir * (radius + Data.coneRange * t);
        }

        protected override void OnExecuteStart()
        {
            _bitten.Clear();
            Bite();
        }

        protected override void OnExecuteTick(float dt)
        {
            Bite();
        }

        void Bite()
        {
            Vector2 center = Owner.Position;
            Vector2 aim = Owner.Host.AimDirection;
            float reach = Owner.Radius + Data.coneRange;
            float halfAngle = Data.coneAngle * 0.5f;

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

                if (go.TryGetComponent<WeaponCell>(out var cell))
                {
                    if (cell.Faction != Owner.Faction && _bitten.Add(go)) cell.Break();
                    continue;
                }

                if (go.TryGetComponent<SwarmCombat>(out var target))
                {
                    if (target.Faction == Owner.Faction || target.Stats.IsDead) continue;
                    if (!_bitten.Add(go)) continue; // each target is bitten once per bite

                    target.Stats.TakeDamage(new DamageInfo(
                        Data.damage * Owner.DamageMultiplier, DamageType.Bite, Owner.Faction, closest));
                    target.ReactToHit(closest);
                }
            }
        }
    }
}
