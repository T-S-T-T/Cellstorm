using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>
    /// Spawn rates (enemies per second) for each enemy type, indexed by milestone.
    /// Index 0 = the first 5 minutes ... index 6 = after Boss 6. Milestones beyond the last
    /// entry reuse the last value (endless mode - the GDD has no numbers for it yet).
    /// </summary>
    [CreateAssetMenu(menuName = "Cellstorm/Spawn Table", fileName = "SpawnTable")]
    public class SpawnTableSO : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public EnemyTypeSO type;
            public float[] ratesPerSecond = new float[7];

            public float RateAt(int milestone)
            {
                if (ratesPerSecond == null || ratesPerSecond.Length == 0) return 0f;
                return Mathf.Max(0f, ratesPerSecond[Mathf.Clamp(milestone, 0, ratesPerSecond.Length - 1)]);
            }
        }

        public Entry[] entries;
    }
}
