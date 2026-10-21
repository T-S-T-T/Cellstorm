using UnityEngine;

namespace Cellstorm.Data
{
    public enum UpgradeCategory
    {
        Swarm,
        Attack,
        Skill,
        Evolution,
        PowerUp
    }

    /// <summary>
    /// Shell for a level-up option. Effects, evolution triggers and pool rules are added in Phase 5.
    /// </summary>
    [CreateAssetMenu(menuName = "Cellstorm/Upgrade", fileName = "NewUpgrade")]
    public class UpgradeSO : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public UpgradeCategory category = UpgradeCategory.Swarm;
        public bool repeatable = true;
        public UpgradeSO[] prerequisites;
    }
}
