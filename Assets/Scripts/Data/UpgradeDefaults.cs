using Cellstorm.Combat;
using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>GDD V0.1.1 upgrades, created in code (editor menu saves them as assets; runtime fallback).</summary>
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

        /// <summary>The full V0.2 upgrade list. Jaw, Gun and Hardening can be passed as saved assets; the rest use the defaults.</summary>
        public static UpgradeSO[] CreateAll(WeaponSO jaw, WeaponSO gun, WeaponSO hardening)
        {
            return CardCatalog.CreateAll(key =>
            {
                if (key == WeaponDefaults.JawKey && jaw != null) return jaw;
                if (key == WeaponDefaults.GunKey && gun != null) return gun;
                if (key == WeaponDefaults.HardeningKey && hardening != null) return hardening;
                return WeaponDefaults.ByKey(key);
            });
        }

        public static UpgradeSO[] CreateAll()
        {
            return CardCatalog.CreateAll(WeaponDefaults.ByKey);
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
