using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>Every upgrade that can appear in the level-up selection.</summary>
    [CreateAssetMenu(menuName = "Cellstorm/Upgrade Pool", fileName = "UpgradePool")]
    public class UpgradePoolSO : ScriptableObject
    {
        /// <summary>Written by Regenerate data. Pools older than CurrentVersion (before V0.2 cards) are ignored at runtime.</summary>
        public const int CurrentVersion = 2;
        public int version;
        public UpgradeSO[] upgrades;
    }
}
