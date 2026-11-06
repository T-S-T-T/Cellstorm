using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Enemies
{
    /// <summary>Everything the Enemy component needs. Built from an EnemyTypeSO or the boss formula.</summary>
    public struct EnemySpec
    {
        public string name;
        public Color color;

        /// <summary>Max HP. Enemies never regenerate. The body size is set once from this (SwarmSize.ForEnemy).</summary>
        public float maxHp;
        public float defense;
        public float speed;

        /// <summary>HP per second dealt to the player while touching, before the boss multiplier and the player's DEF.</summary>
        public float contactDps;
        /// <summary>Bosses: 1 + 0.2 x (milestone - 1), applied to contact and weapon damage. Others: 1.</summary>
        public float damageMultiplier;

        public EnemyBehavior behavior;
        public float preferredRange;
        public WeaponSO weapon;

        public float explosionTriggerRange;
        public float explosionRadius;
        public float explosionDamage;

        public int scoreValue;
        public int expValue;

        public bool isBoss;
        /// <summary>Bosses: the boss number. Others: the milestone they spawned in.</summary>
        public int milestone;

        public static EnemySpec FromType(EnemyTypeSO t, int milestone)
        {
            return new EnemySpec
            {
                name = t.displayName,
                color = t.color,
                maxHp = t.maxHp,
                defense = t.defense,
                speed = t.speed,
                contactDps = t.contactDps,
                damageMultiplier = 1f,
                behavior = t.behavior,
                preferredRange = t.preferredRange,
                weapon = t.weapon,
                explosionTriggerRange = t.explosionTriggerRange,
                explosionRadius = t.explosionRadius,
                explosionDamage = t.explosionDamage,
                scoreValue = t.scoreValue,
                expValue = t.expValue,
                isBoss = false,
                milestone = milestone
            };
        }
    }
}
