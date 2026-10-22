using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Enemies
{
    /// <summary>Everything the Enemy component needs. Built from an EnemyTypeSO or the boss formula.</summary>
    public struct EnemySpec
    {
        public string name;
        public Color color;

        public int maxCells;
        public float regen;
        public float defense;
        public float speed;

        public EnemyBehavior behavior;
        public float preferredRange;
        public WeaponSO weapon;

        public float explosionTriggerRange;
        public float explosionRadius;
        public int explosionDamage;

        public int scoreValue;
        public bool scorePerCell;
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
                maxCells = t.maxCells,
                regen = t.cellRegen,
                defense = t.defense,
                speed = t.speed,
                behavior = t.behavior,
                preferredRange = t.preferredRange,
                weapon = t.weapon,
                explosionTriggerRange = t.explosionTriggerRange,
                explosionRadius = t.explosionRadius,
                explosionDamage = t.explosionDamage,
                scoreValue = t.scoreValue,
                scorePerCell = t.scorePerCell,
                expValue = t.expValue,
                isBoss = false,
                milestone = milestone
            };
        }
    }
}
