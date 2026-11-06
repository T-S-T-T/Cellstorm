using UnityEngine;

namespace Cellstorm.Combat
{
    public enum DamageType
    {
        Contact,
        Ram,
        Bite,
        Bullet,
        Explosion,
        /// <summary>Not reduced by DEF and not from a hostile source (debug, cost of a broken weapon cell).</summary>
        True
    }

    /// <summary>One hit. The receiver applies its own DEF (see DefCurve), so the sender passes the raw amount.</summary>
    public struct DamageInfo
    {
        public float amount;
        public DamageType type;
        public Faction sourceFaction;
        public Vector2 hitPoint;

        public DamageInfo(float amount, DamageType type, Faction sourceFaction, Vector2 hitPoint)
        {
            this.amount = amount;
            this.type = type;
            this.sourceFaction = sourceFaction;
            this.hitPoint = hitPoint;
        }
    }

    /// <summary>Anything that can lose HP: swarm bodies now, weapon cells in V0.1.1 Phase 4.</summary>
    public interface IDamageable
    {
        /// <summary>Applies the hit (reduced by the receiver's DEF). Returns the HP actually lost.</summary>
        float TakeDamage(DamageInfo info);
    }
}
