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
        True,
        /// <summary>V0.2: one tick of Toxic. Reduced by DEF like any other damage.</summary>
        Toxic
    }

    /// <summary>
    /// Status effects a hit carries (GDD V0.2 section 4.4). Everything is 0 when the hit has no effect.
    /// Slow is a fraction (0.2 = 20% slower). knockDistance &gt; 0 shoves away from sourcePos, &lt; 0 pulls toward it.
    /// </summary>
    public struct StatusPayload
    {
        public float toxicDps, toxicDuration;
        public float slowPct, slowDuration;
        public float stunChance, stunDuration;
        public float knockDistance;
        public Vector2 sourcePos;

        public bool HasAny =>
            toxicDps > 0f || slowPct > 0f || stunChance > 0f || Mathf.Abs(knockDistance) > 0.0001f;
    }

    /// <summary>One hit. The receiver applies its own DEF (see DefCurve), so the sender passes the raw amount.</summary>
    public struct DamageInfo
    {
        public float amount;
        public DamageType type;
        public Faction sourceFaction;
        public Vector2 hitPoint;
        public StatusPayload status;
        /// <summary>True for hazard damage: a kill caused only by hazards gives no score and no EXP.</summary>
        public bool hazard;

        public DamageInfo(float amount, DamageType type, Faction sourceFaction, Vector2 hitPoint,
            StatusPayload status = default, bool hazard = false)
        {
            this.amount = amount;
            this.type = type;
            this.sourceFaction = sourceFaction;
            this.hitPoint = hitPoint;
            this.status = status;
            this.hazard = hazard;
        }
    }

    /// <summary>Anything that can lose HP: swarm bodies, minions and (through WeaponCellGroup) weapon cells.</summary>
    public interface IDamageable
    {
        /// <summary>Applies the hit (reduced by the receiver's DEF). Returns the HP actually lost.</summary>
        float TakeDamage(DamageInfo info);
    }
}
