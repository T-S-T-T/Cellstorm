using Cellstorm.Combat;
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

    public enum UpgradeEffect
    {
        /// <summary>Permanent flat change to a base stat (Max cells +10, Regen +1, DEF +1, Speed +1).</summary>
        StatAdd,
        /// <summary>Permanent percentage change, e.g. +20% DEF and -10% max cells (Dense Cells).</summary>
        StatPercent,
        /// <summary>Equip a weapon or skill.</summary>
        GrantWeapon,
        /// <summary>The Tank evolution.</summary>
        EvolveTank
    }

    /// <summary>One level-up option.</summary>
    [CreateAssetMenu(menuName = "Cellstorm/Upgrade", fileName = "NewUpgrade")]
    public class UpgradeSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Tooltip("Optional. Without a sprite the level-up card shows the glyph on a coloured background.")]
        public Sprite icon;
        public string iconGlyph = "?";
        [Tooltip("Alpha 0 = use the category colour.")]
        public Color iconColor = new Color(0f, 0f, 0f, 0f);
        public UpgradeCategory category = UpgradeCategory.Swarm;
        public bool repeatable = true;

        [Header("Pool rules")]
        [Tooltip("Empty = always available. Otherwise it must be unlocked (UnlockState).")]
        public string unlockId;
        [Tooltip("Each of these must have been taken at least once.")]
        public UpgradeSO[] prerequisites;
        [Tooltip("Evolutions: the upgrade you need several copies of.")]
        public UpgradeSO requirementUpgrade;
        public int requirementStacks = 1;
        [Tooltip("If you own any of these, this upgrade never appears (evolution lock-outs).")]
        public UpgradeSO[] lockedOutBy;

        [Header("Effect")]
        public UpgradeEffect effect = UpgradeEffect.StatAdd;
        public StatType stat = StatType.Defense;
        [Tooltip("StatAdd: amount added to the base stat.")]
        public float amount = 1f;
        [Tooltip("StatAdd on Max Cells: also add that many cells right now.")]
        public bool addCurrentCells = true;
        [Tooltip("StatPercent: +0.2 = +20%, -0.1 = -10%.")]
        public float percent = 0f;
        public StatType secondaryStat = StatType.MaxCells;
        public float secondaryPercent = 0f;
        public WeaponSO weapon;

        [Header("Evolution (Tank)")]
        public float evolutionDefMultiplier = 5f;
        [Tooltip("Multiplier on the effect of Max Cells and Regen upgrades taken after evolving.")]
        public float evolutionGrowthEffect = 0.5f;
    }
}
