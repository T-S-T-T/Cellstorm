using UnityEngine;

namespace Cellstorm.Data
{
    public enum EnemyBehavior
    {
        /// <summary>Move straight toward the player.</summary>
        Chase,
        /// <summary>Approach until in range, then hold distance (ranged cell).</summary>
        KeepDistance,
        /// <summary>Move toward the player and explode when close (exploding cell).</summary>
        Explode,
        /// <summary>V0.2 support cell: follows its target but stops 8 units short, healing nearby enemies.</summary>
        Support
    }

    /// <summary>One regular enemy type. Bosses are built from a formula instead (see EnemySpawner).</summary>
    [CreateAssetMenu(menuName = "Cellstorm/Enemy Type", fileName = "NewEnemyType")]
    public class EnemyTypeSO : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Enemy";
        public Color color = new Color(0.95f, 0.35f, 0.35f, 1f);

        [Header("Stats (enemies never regenerate HP)")]
        [Min(1)] public float maxHp = 10f;
        [Min(0)] public float defense = 1f;
        [Min(0)] public float speed = 1f;

        [Header("Damage")]
        [Tooltip("HP per second dealt to the player while touching (before the player's DEF).")]
        [Min(0)] public float contactDps = 5f;

        [Header("Behaviour")]
        public EnemyBehavior behavior = EnemyBehavior.Chase;
        [Tooltip("KeepDistance only: distance from the player this enemy tries to hold.")]
        public float preferredRange = 6f;

        [Header("Explosion (Explode behaviour only)")]
        [Tooltip("Explodes when the gap between its edge and the player's edge is below this.")]
        public float explosionTriggerRange = 0.6f;
        public float explosionRadius = 1.8f;
        [Tooltip("Damage dealt to the player, reduced by the player's DEF.")]
        [Min(0)] public float explosionDamage = 20f;

        [Header("Support cell (V0.2)")]
        [Tooltip("Support only: it stops this far from its target.")]
        public float supportStandOff = 8f;
        [Tooltip("Support only: heal range, in units.")]
        public float healRange = 4f;
        [Tooltip("Support only: heals every other enemy in range by this share of ITS max HP every second (bosses: 1/5 of it).")]
        public float healFraction = 0.05f;

        [Header("Weapon")]
        [Tooltip("Optional weapon this enemy uses (slot 0). Ranged cell: the Enemy Gun.")]
        public WeaponSO weapon;

        [Header("Rewards")]
        [Tooltip("Flat score for defeating it (GDD V0.1.1: 1, 10, 100, 2).")]
        public int scoreValue = 1;
        [Tooltip("PLACEHOLDER: EXP dropped.")]
        public int expValue = 1;
    }
}
