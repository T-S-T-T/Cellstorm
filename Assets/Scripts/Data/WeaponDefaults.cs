using System.Collections.Generic;
using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>
    /// GDD default weapon and skill definitions, created in code. The editor menu saves them as assets;
    /// at runtime they are the fallback when nothing is assigned.
    /// </summary>
    public static class WeaponDefaults
    {
        public const string RamKey = "ram";
        public const string JawKey = "jaw";
        public const string GunKey = "gun";
        public const string HardeningKey = "hardening";
        public const string EnemyGunKey = "enemy_gun";

        static readonly Dictionary<string, WeaponSO> Cache = new Dictionary<string, WeaponSO>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Cache.Clear();
        }

        public static WeaponSO Ram => Get(RamKey);
        public static WeaponSO Jaw => Get(JawKey);
        public static WeaponSO Gun => Get(GunKey);
        public static WeaponSO Hardening => Get(HardeningKey);
        public static WeaponSO EnemyGun => Get(EnemyGunKey);

        static WeaponSO Get(string key)
        {
            if (Cache.TryGetValue(key, out var w) && w != null) return w;
            w = Create(key);
            Cache[key] = w;
            return w;
        }

        /// <summary>Creates a fresh, unsaved instance (used by the editor menu to make assets).</summary>
        public static WeaponSO Create(string key)
        {
            var w = ScriptableObject.CreateInstance<WeaponSO>();
            w.id = key;

            switch (key)
            {
                case RamKey:
                    w.displayName = "Ram";
                    w.description = "Dash quickly toward the cursor.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Ram;
                    w.shapeTime = 0f; w.executeTime = 0f; w.cooldown = 1f;
                    w.dashSpeed = 12f;
                    break;

                case JawKey:
                    w.displayName = "Jaw";
                    w.description = "Hold to form a jaw, release to bite. Eats anything in the cone with lower DEF.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Jaw;
                    w.shapeTime = 3f; w.executeTime = 0.1f; w.cooldown = 3f; w.cellShare = 0.1f;
                    w.coneAngle = 90f; w.coneRange = 2f; w.biteDefMultiplier = 2f;
                    break;

                case GunKey:
                    w.displayName = "Gun";
                    w.description = "Hold to form a barrel, release to fire a slow, high-DEF bullet.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Gun;
                    w.shapeTime = 3f; w.executeTime = 0f; w.cooldown = 3f; w.cellShare = 0.1f;
                    w.bulletSpeedMultiplier = 2f; w.bulletDefMultiplier = 2f; w.pierce = 0; w.bulletRadius = 0.2f;
                    break;

                case HardeningKey:
                    w.displayName = "Hardening";
                    w.description = "DEF x2 for 3 seconds.";
                    w.kind = AbilityKind.Skill; w.type = WeaponType.Hardening;
                    w.executeTime = 3f; w.cooldown = 10f; w.defMultiplier = 2f;
                    break;

                case EnemyGunKey:
                    w.displayName = "Enemy Gun";
                    w.description = "The enemy railgun: 5 seconds to form. Destroy its barrel cells to interrupt it.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Gun;
                    w.shapeTime = 5f; w.executeTime = 0f; w.cooldown = 3f; w.cellShare = 0.1f;
                    w.bulletSpeedMultiplier = 2f; w.bulletDefMultiplier = 2f; w.pierce = 0; w.bulletRadius = 0.2f;
                    break;
            }
            return w;
        }
    }
}
