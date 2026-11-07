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
        /// <summary>Permanent flat change to a base stat (Max HP +20, Regen +1, DEF +1, Speed +1).</summary>
        StatAdd,
        /// <summary>Permanent percentage change, e.g. +20% DEF and -10% max HP (Dense Cells).</summary>
        StatPercent,
        /// <summary>Equip a weapon or skill.</summary>
        GrantWeapon,
        /// <summary>The Tank evolution.</summary>
        EvolveTank,
        /// <summary>Potent Cells (V0.1.1, old assets only). V0.2 uses a Card.</summary>
        PotentCells,
        /// <summary>V0.2: an upgrade card boosting aspects of weapons and skills (CardEffect list).</summary>
        Card,
        /// <summary>V0.2: a one-time evolution with a condition and a lock-out list.</summary>
        Evolution
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
        [Tooltip("StatAdd on Max HP: also heal that much right now.")]
        public bool addCurrentCells = true;
        [Tooltip("StatPercent: +0.2 = +20%, -0.1 = -10%.")]
        public float percent = 0f;
        public StatType secondaryStat = StatType.MaxHp;
        public float secondaryPercent = 0f;
        public WeaponSO weapon;

        [Header("Potent Cells")]
        [Tooltip("Share of max HP a deployed weapon holds (replaces the weapon's own 10%).")]
        public float potentCostFraction = 0.3f;
        [Tooltip("Multiplier on the damage of every weapon.")]
        public float potentDamageMultiplier = 1.5f;

        [Header("V0.2 card: who it applies to")]
        [Tooltip("Specific cards: the weapon or skill types it applies to (they must be owned).")]
        public WeaponType[] targetTypes;
        [Tooltip("Shared cards: applies to every owned item with any of these tags.")]
        public WeaponTag targetTags;
        [Tooltip("Applies to everything (evolutions).")]
        public bool appliesToAll;
        public CardEffect[] effects;
        [Tooltip("0 = no cap. Once-only cards use repeatable = false.")]
        public int maxStacks;
        public StackForm form = StackForm.Additive;
        [Tooltip("Counted by evolutions: 'minion' or 'projectile'.")]
        public string cardGroup;
        [Tooltip("Shown in the tooltip, for example '+15% damage of base per stack'.")]
        public string stackText;

        [Header("V0.2 evolution")]
        public EvoKind evo = EvoKind.None;
        public EvoCondition condition = EvoCondition.Stacks;
        [Tooltip("Stacks: id of the card. WeaponAndCards: the card group name.")]
        public string requirementId;
        public int requirementCount = 1;
        [Tooltip("WeaponAndCards / BossReward: owning any of these weapons or skills is needed.")]
        public WeaponType[] requiredAnyOwned;
        [Tooltip("Ids of evolutions that block this one (and that this one blocks).")]
        public string[] lockoutIds;
        [Tooltip("Swarm evolution: multiplier on max HP (0.8).")]
        public float evoMaxHpMultiplier = 1f;

        [Header("Evolution (Tank)")]
        public float evolutionDefMultiplier = 5f;
        [Tooltip("Multiplier on the effect of Max HP and Regen upgrades taken after evolving.")]
        public float evolutionGrowthEffect = 0.5f;

        /// <summary>Tooltip line about stacking: the rule and how many the player owns.</summary>
        public string StackLine(int owned)
        {
            string rule = !string.IsNullOrEmpty(stackText) ? stackText : "";
            string cap = maxStacks > 0 ? owned + " of " + maxStacks : (repeatable ? owned + " owned" : (owned > 0 ? "owned" : "one-time"));
            if (!repeatable && owned == 0) cap = "one-time";
            return (rule.Length > 0 ? rule + "  |  " : "") + cap;
        }
    }
}

