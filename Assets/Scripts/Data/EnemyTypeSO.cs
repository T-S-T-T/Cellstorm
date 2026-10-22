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
        Explode
    }

    /// <summary>One regular enemy type. Bosses are built from a formula instead (see EnemySpawner).</summary>
    [CreateAssetMenu(menuName = "Cellstorm/Enemy Type", fileName = "NewEnemyType")]
    public class EnemyTypeSO : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Enemy";
        public Color color = new Color(0.95f, 0.35f, 0.35f, 1f);

        [Header("Stats")]
        [Min(1)] public int maxCells = 1;
        [Min(0)] public float cellRegen = 0f;
        [Min(0)] public float defense = 1f;
        [Min(0)] public float speed = 1f;

        [Header("Behaviour")]
        public EnemyBehavior behavior = EnemyBehavior.Chase;
        [Tooltip("KeepDistance only: distance from the player this enemy tries to hold.")]
        public float preferredRange = 6f;

        [Header("Explosion (Explode behaviour only) - PLACEHOLDER VALUES")]
        [Tooltip("Explodes when the gap between its edge and the player's edge is below this.")]
        public float explosionTriggerRange = 0.6f;
        public float explosionRadius = 1.8f;
        [Tooltip("Flat cells removed from the player. Ignores DEF for now (the GDD leaves this open).")]
        [Min(0)] public int explosionDamage = 2;

        [Header("Weapon")]
        [Tooltip("Optional weapon this enemy uses (slot 0). Ranged cell: the Enemy Gun.")]
        public WeaponSO weapon;

        [Header("Rewards")]
        public int scoreValue = 1;
        [Tooltip("If true, score = scoreValue x max cells (GDD: multi 1/cell, ranged 2/cell).")]
        public bool scorePerCell = false;
        [Tooltip("PLACEHOLDER: EXP orbs arrive in Phase 5 and the GDD has no numbers yet.")]
        public int expValue = 1;
    }
}
