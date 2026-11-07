using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Cells form a V-shaped open mouth toward the aim direction (90 degrees, 2 units beyond the swarm's edge).
    /// On release the mouth snaps shut for 0.1 s and hits everything in the cone once: swarms take the weapon's damage
    /// (reduced by their DEF), hostile weapon cells are damaged and enemy bullets are destroyed.
    /// V0.2 cards: Wide Jaw (angle), Long Fangs (reach), Wide Reach (size), Second Bite (a second, weaker bite).
    /// </summary>
    public class JawAbility : ShapedAbility
    {
        const float SecondBiteDelay = 0.3f;
        const float SecondBiteScale = 0.5f;

        readonly List<Collider2D> _hits = new List<Collider2D>();
        readonly HashSet<GameObject> _bitten = new HashSet<GameObject>();
        bool _secondDone;

        public JawAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        float ConeAngle => Mathf.Clamp(P(Param.ConeAngle, Data.coneAngle), 10f, 340f);
        float Range => Mathf.Max(0.3f, P(Param.Reach, Data.coneRange) * P(Param.Size, 1f));
        bool HasSecondBite => Owner.Mods.Flag(Param.SecondBite, Data);

        protected override float ReachUnits => Range;
        protected override float ExecuteTime => Data.executeTime + (HasSecondBite ? SecondBiteDelay : 0f);
        protected override float ClosureTime => Data.executeTime;

        protected override Vector2 FormationPoint(int index, int count, Vector2 center, float radius, Vector2 aim)
        {
            // Two arms; arm cells are spread along the arm with a little thickness.
            int arms = Mathf.Max(1, (count + 1) / 2);
            int rank = index / 2;
            float side = (index % 2 == 0) ? 1f : -1f;
            float t = (rank + 1f) / arms;

            float half = ConeAngle * 0.5f * (1f - Closure);
            if (count == 1) half = 0f;

            Vector2 dir = Rotate(aim, side * half);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float thick = (Hash01(index) - 0.5f) * 0.14f;
            return center + dir * (radius + Range * t) + perp * thick;
        }

        protected override void OnExecuteStart()
        {
            _bitten.Clear();
            _secondDone = false;
            Bite(1f);
        }

        protected override void OnExecuteTick(float dt)
        {
            float elapsed = ExecuteElapsed;
            if (!_secondDone && HasSecondBite && elapsed >= SecondBiteDelay)
            {
                _secondDone = true;
                _bitten.Clear();
                Bite(SecondBiteScale);
            }
            else if (elapsed < Data.executeTime)
            {
                Bite(1f);       // cells still closing may reach targets the first frame missed (each is hit once)
            }
        }

        void Bite(float scale)
        {
            Vector2 center = Owner.Position;
            Vector2 aim = Owner.Host.AimDirection;
            float reach = Owner.Radius + Range;
            float halfAngle = ConeAngle * 0.5f;
            float damage = DamageNow * scale;
            var status = StatusFor(default, center);

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

                if (go.TryGetComponent<SwarmCombat>(out var target))
                {
                    if (target.Faction == Owner.Faction || target.Stats.IsDead) continue;
                    if (!_bitten.Add(go)) continue; // each target is bitten once per bite

                    target.Stats.TakeDamage(new DamageInfo(damage, DamageType.Bite, Owner.Faction, closest, status));
                    target.ReactToHit(closest);
                }
            }

            // Weapon cells have no colliders: query them with the same cone.
            WeaponCellGroup.DamageCone(center, aim, halfAngle, reach, damage, Owner.Faction);
        }
    }
}
