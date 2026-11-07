using System.Collections.Generic;
using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>Shared damage helpers that are not tied to one weapon.</summary>
    public static class DamageUtil
    {
        static readonly List<Collider2D> Buf = new List<Collider2D>();
        static readonly HashSet<SwarmCombat> Seen = new HashSet<SwarmCombat>();

        /// <summary>
        /// Area damage: every swarm of the other faction whose collider touches the circle takes 'raw' damage once
        /// (reduced by its own DEF), and hostile weapon cells inside the circle are damaged too.
        /// Used by exploding enemies (20 damage, radius 1.8) and V0.2 mines.
        /// </summary>
        public static int Explode(Vector2 center, float radius, float raw, Faction attacker, StatusPayload status = default)
        {
            int hits = 0;
            Buf.Clear();
            Seen.Clear();
            Physics2D.OverlapCircle(center, radius, ContactFilter2D.noFilter, Buf);

            for (int i = 0; i < Buf.Count; i++)
            {
                var col = Buf[i];
                if (col == null) continue;
                var rb = col.attachedRigidbody;
                var go = rb != null ? rb.gameObject : col.gameObject;
                if (!go.TryGetComponent<SwarmCombat>(out var target)) continue;
                if (target.Faction == attacker || target.Stats.IsDead) continue;
                if (!Seen.Add(target)) continue;

                Vector2 point = col.ClosestPoint(center);
                target.Stats.TakeDamage(new DamageInfo(raw, DamageType.Explosion, attacker, point, status));
                target.ReactToHit(point);
                hits++;
            }

            WeaponCellGroup.DamageCircle(center, radius, raw, attacker);
            return hits;
        }
    }
}
