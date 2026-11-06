using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>
    /// A set of swarm stats. Used for starting loadouts.
    /// sizeMultiplier scales the fixed body size (Giant = 2); weaponCellCount is how many cells Jaw and Gun spawn.
    /// </summary>
    [CreateAssetMenu(menuName = "Cellstorm/Stat Block", fileName = "NewStatBlock")]
    public class StatBlockSO : ScriptableObject
    {
        [Min(1)] public float maxHp = 100f;
        [Min(0)] public float hpRegen = 1f;   // HP per second
        [Min(0)] public float defense = 2f;
        [Min(0)] public float speed = 2f;

        [Header("Body (fixed size, GDD 4.1)")]
        [Tooltip("Multiplies the fixed player radius of 0.335. Giant = 2.")]
        [Min(0.1f)] public float sizeMultiplier = 1f;
        [Tooltip("Cells spawned by Jaw and Gun. 100 by default, 200 for Giant. Used from Phase 4.")]
        [Min(1)] public int weaponCellCount = 100;
    }
}
