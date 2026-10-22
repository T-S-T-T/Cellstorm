using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>Every upgrade that can appear in the level-up selection.</summary>
    [CreateAssetMenu(menuName = "Cellstorm/Upgrade Pool", fileName = "UpgradePool")]
    public class UpgradePoolSO : ScriptableObject
    {
        public UpgradeSO[] upgrades;
    }
}
