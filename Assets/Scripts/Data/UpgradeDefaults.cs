using Cellstorm.Combat;
using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>GDD V0.1 upgrades, created in code (editor menu saves them as assets; runtime fallback).</summary>
    public static class UpgradeDefaults
    {
        public static Color CategoryColor(UpgradeCategory c)
        {
            switch (c)
            {
                case UpgradeCategory.Swarm: return new Color(0.25f, 0.60f, 0.45f);
                case UpgradeCategory.Attack: return new Color(0.80f, 0.30f, 0.30f);
                case UpgradeCategory.Skill: return new Color(0.55f, 0.35f, 0.80f);
                case UpgradeCategory.Evolution: return new Color(0.85f, 0.65f, 0.15f);
                default: return new Color(0.90f, 0.55f, 0.20f);
            }
        }

        public static UpgradeSO[] CreateAll(WeaponSO jaw, WeaponSO gun, WeaponSO hardening)
        {
            var expand = Make("max_cells", "Expand", "Max cells +10. The new cells appear right away.",
                UpgradeCategory.Swarm, "HP");
            expand.effect = UpgradeEffect.StatAdd; expand.stat = StatType.MaxCells;
            expand.amount = 10f; expand.addCurrentCells = true;

            var regen = Make("regen", "Regeneration", "Cell regen +1 per second.",
                UpgradeCategory.Swarm, "RG");
            regen.effect = UpgradeEffect.StatAdd; regen.stat = StatType.CellRegen; regen.amount = 1f;

            var def = Make("defense", "Hardened Cells", "Cell defense (DEF) +1. Higher DEF always wins a clash.",
                UpgradeCategory.Swarm, "DEF");
            def.effect = UpgradeEffect.StatAdd; def.stat = StatType.Defense; def.amount = 1f;

            var speed = Make("speed", "Swift Cells", "Speed +1.",
                UpgradeCategory.Swarm, "SPD");
            speed.effect = UpgradeEffect.StatAdd; speed.stat = StatType.Speed; speed.amount = 1f;

            var dense = Make("dense_cells", "Dense Cells",
                "+20% DEF, -10% max cells. Collect 5 to unlock an evolution.",
                UpgradeCategory.Swarm, "DNS");
            dense.effect = UpgradeEffect.StatPercent;
            dense.stat = StatType.Defense; dense.percent = 0.2f;
            dense.secondaryStat = StatType.MaxCells; dense.secondaryPercent = -0.1f;

            var jawUp = Make("jaw", "Jaw",
                "Attack. Hold to form a jaw, release to bite. Eats anything in the cone with lower DEF.",
                UpgradeCategory.Attack, "JAW");
            jawUp.effect = UpgradeEffect.GrantWeapon; jawUp.weapon = jaw;
            jawUp.repeatable = false; jawUp.unlockId = "jaw";

            var gunUp = Make("gun", "Gun",
                "Attack. Hold to form a barrel, release to fire a slow bullet with double DEF.",
                UpgradeCategory.Attack, "GUN");
            gunUp.effect = UpgradeEffect.GrantWeapon; gunUp.weapon = gun;
            gunUp.repeatable = false; gunUp.unlockId = "gun";

            var hardenUp = Make("hardening", "Hardening",
                "Skill. Press to double your DEF for 3 seconds. 10 second cooldown.",
                UpgradeCategory.Skill, "HRD");
            hardenUp.effect = UpgradeEffect.GrantWeapon; hardenUp.weapon = hardening;
            hardenUp.repeatable = false; hardenUp.unlockId = "hardening";

            var tank = Make("evo_tank", "Evolution: Tank",
                "Evolution. DEF x5. Max cell and regen upgrades are half as effective from now on.",
                UpgradeCategory.Evolution, "TNK");
            tank.effect = UpgradeEffect.EvolveTank; tank.repeatable = false;
            tank.requirementUpgrade = dense; tank.requirementStacks = 5;

            return new[] { expand, regen, def, speed, dense, jawUp, gunUp, hardenUp, tank };
        }

        static UpgradeSO Make(string id, string displayName, string description, UpgradeCategory category, string glyph)
        {
            var u = ScriptableObject.CreateInstance<UpgradeSO>();
            u.name = id;
            u.id = id;
            u.displayName = displayName;
            u.description = description;
            u.category = category;
            u.iconGlyph = glyph;
            u.iconColor = CategoryColor(category);
            return u;
        }
    }
}
