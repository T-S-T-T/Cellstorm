using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>
    /// A set of swarm stats. Used for starting loadouts now, and enemy types / bosses later.
    /// </summary>
    [CreateAssetMenu(menuName = "Cellstorm/Stat Block", fileName = "NewStatBlock")]
    public class StatBlockSO : ScriptableObject
    {
        [Min(1)] public int maxCells = 5;
        [Min(0)] public float cellRegen = 1f;   // cells per second
        [Min(0)] public float defense = 2f;
        [Min(0)] public float speed = 2f;
    }
}
