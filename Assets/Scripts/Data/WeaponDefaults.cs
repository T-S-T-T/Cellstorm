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
        public const string EnemyRamKey = "enemy_ram";
        public const string EnemyJawKey = "enemy_jaw";
        public const string WhipKey = "whip";
        public const string SpreadShotKey = "spread_shot";
        public const string SporeCloudKey = "spore_cloud";
        public const string MineKey = "mine";
        public const string MinionKey = "minion";
        public const string ConvertKey = "convert";
        public const string ShockwaveKey = "shockwave";
        public const string EnemyWhipKey = "enemy_whip";
        public const string EnemySpreadKey = "enemy_spread_shot";
        public const string EnemySporeKey = "enemy_spore_cloud";
        public const string EnemyMineKey = "enemy_mine";

        /// <summary>Every key, in a stable order (the editor menu creates one asset per key).</summary>
        public static readonly string[] AllKeys =
        {
            RamKey, JawKey, GunKey, HardeningKey, WhipKey, SpreadShotKey, SporeCloudKey, MineKey,
            MinionKey, ConvertKey, ShockwaveKey,
            EnemyGunKey, EnemyRamKey, EnemyJawKey, EnemyWhipKey, EnemySpreadKey, EnemySporeKey, EnemyMineKey
        };

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
        public static WeaponSO EnemyRam => Get(EnemyRamKey);
        public static WeaponSO EnemyJaw => Get(EnemyJawKey);
        public static WeaponSO Whip => Get(WhipKey);
        public static WeaponSO SpreadShot => Get(SpreadShotKey);
        public static WeaponSO SporeCloud => Get(SporeCloudKey);
        public static WeaponSO Mine => Get(MineKey);
        public static WeaponSO Minion => Get(MinionKey);
        public static WeaponSO Convert => Get(ConvertKey);
        public static WeaponSO Shockwave => Get(ShockwaveKey);

        /// <summary>The weapon or skill for a key, or null for an empty or unknown key.</summary>
        public static WeaponSO ByKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return System.Array.IndexOf(AllKeys, key) >= 0 ? Get(key) : null;
        }

        /// <summary>The boss weapon pool: Ram, Jaw, Gun, Whip, Spread Shot, Spore Cloud and Mine (GDD V0.2 section 7.2).</summary>
        public static WeaponSO[] BossPool()
        {
            return new[] { EnemyRam, EnemyJaw, EnemyGun, Get(EnemyWhipKey), Get(EnemySpreadKey), Get(EnemySporeKey), Get(EnemyMineKey) };
        }

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
                    w.description = "Dash toward the cursor for 0.3 s. Everything you touch takes 30 damage once.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Ram;
                    w.shapeTime = 0f; w.executeTime = 0.3f; w.cooldown = 1f;
                    w.dashMultiplier = 5f; w.damage = 30f;
                    break;

                case JawKey:
                    w.displayName = "Jaw";
                    w.description = "Hold to form a jaw, release to bite everything in the cone for 150 damage.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Jaw;
                    w.shapeTime = 3f; w.executeTime = 0.1f; w.cooldown = 3f; w.cellShare = 0.1f;
                    w.coneAngle = 90f; w.coneRange = 2f; w.damage = 150f;
                    break;

                case GunKey:
                    w.displayName = "Gun";
                    w.description = "Hold to form a barrel, release to fire a bullet for 100 damage.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Gun;
                    w.shapeTime = 3f; w.executeTime = 0f; w.cooldown = 3f; w.cellShare = 0.1f;
                    w.bulletSpeedMultiplier = 2f; w.pierce = 0; w.bulletRadius = 0.2f; w.damage = 100f;
                    break;

                case HardeningKey:
                    w.displayName = "Hardening";
                    w.description = "DEF x2 for 3 seconds.";
                    w.kind = AbilityKind.Skill; w.type = WeaponType.Hardening;
                    w.executeTime = 3f; w.cooldown = 10f; w.defMultiplier = 2f;
                    break;

                case EnemyRamKey:
                    w.displayName = "Enemy Ram";
                    w.description = "Boss dash: 20 damage to whatever it touches.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Ram;
                    w.shapeTime = 0f; w.executeTime = 0.3f; w.cooldown = 3f;
                    w.dashMultiplier = 5f; w.damage = 20f;
                    break;

                case EnemyJawKey:
                    w.displayName = "Enemy Jaw";
                    w.description = "Boss jaw: 40 damage. Destroy its cells to interrupt it.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Jaw;
                    w.shapeTime = 3f; w.executeTime = 0.1f; w.cooldown = 3f; w.cellShare = 0.1f;
                    w.coneAngle = 90f; w.coneRange = 2f; w.damage = 40f;
                    break;

                case EnemyGunKey:
                    w.displayName = "Enemy Gun";
                    w.description = "The enemy railgun: 5 seconds to form. Destroy its barrel cells to interrupt it.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Gun;
                    w.shapeTime = 5f; w.executeTime = 0f; w.cooldown = 3f; w.cellShare = 0.1f;
                    w.bulletSpeedMultiplier = 2f; w.pierce = 0; w.bulletRadius = 0.2f; w.damage = 20f;
                    break;

                case WhipKey:
                    w.displayName = "Whip";
                    w.description = "Hold to grow a tentacle, release to lash a 120 degree arc: 80 damage and a pull toward you.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Whip;
                    w.shapeTime = 3f; w.executeTime = 0.2f; w.cooldown = 3f; w.cellShare = 0.1f;
                    w.whipArc = 120f; w.whipRange = 3f; w.pullDistance = 1.5f; w.damage = 80f;
                    break;

                case SpreadShotKey:
                    w.displayName = "Spread Shot";
                    w.description = "Hold to form a wide muzzle, release to fire 5 pellets in a fan. 40 damage each, short range.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.SpreadShot;
                    w.shapeTime = 3f; w.executeTime = 0f; w.cooldown = 3f; w.cellShare = 0.1f;
                    w.pellets = 5; w.fanAngle = 40f; w.pelletRange = 6f; w.damage = 40f;
                    w.bulletSpeedMultiplier = 2f; w.bulletRadius = 0.15f; w.pierce = 0;
                    break;

                case SporeCloudKey:
                    w.displayName = "Spore Cloud";
                    w.description = "Press to release a cloud (radius 2.5, 4 s) around you. Enemies inside get Toxic 5 HP/s. No direct damage.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.SporeCloud;
                    w.cooldown = 8f; w.damage = 0f;
                    w.cloudRadius = 2.5f; w.cloudDuration = 4f; w.toxicDps = 5f; w.toxicDuration = 1f;
                    break;

                case MineKey:
                    w.displayName = "Mine";
                    w.description = "Press to drop a mine. It arms after 1 s and explodes when an enemy comes close: 60 damage in 2 units.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Mine;
                    w.cooldown = 4f; w.damage = 60f;
                    w.mineArmTime = 1f; w.mineTriggerRange = 1.2f; w.mineLifetime = 15f; w.blastRadius = 2f;
                    break;

                case MinionKey:
                    w.displayName = "Minion";
                    w.description = "Press to split a small swarm off your body. Each costs 2.5% of your max HP, fights on its own and never gives the HP back.";
                    w.kind = AbilityKind.Skill; w.type = WeaponType.Minion;
                    w.cooldown = 0.5f;
                    w.minionCostFraction = 0.025f; w.minionContactDps = 10f; w.minionCount = 1;
                    break;

                case ConvertKey:
                    w.displayName = "Convert";
                    w.description = "For 5 s, enemies you damage drop HP orbs that heal you for 10% of the damage. 15 s cooldown.";
                    w.kind = AbilityKind.Skill; w.type = WeaponType.Convert;
                    w.executeTime = 5f; w.cooldown = 15f; w.orbFraction = 0.1f;
                    break;

                case ShockwaveKey:
                    w.displayName = "Shockwave";
                    w.description = "A ring bursts out (radius 5): enemies inside are pushed 4 units and stunned for 1 s. No damage. 12 s cooldown.";
                    w.kind = AbilityKind.Skill; w.type = WeaponType.Shockwave;
                    w.cooldown = 12f; w.shockRadius = 5f; w.shockPush = 4f; w.shockStun = 1f;
                    break;

                case EnemyWhipKey:
                    w.displayName = "Enemy Whip";
                    w.description = "Boss whip: 25 damage and a pull. Takes 5 s to grow.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Whip;
                    w.shapeTime = 5f; w.executeTime = 0.2f; w.cooldown = 3f; w.cellShare = 0.1f;
                    w.whipArc = 120f; w.whipRange = 3f; w.pullDistance = 1.5f; w.damage = 25f;
                    break;

                case EnemySpreadKey:
                    w.displayName = "Enemy Spread Shot";
                    w.description = "Boss shotgun: 8 damage per pellet. Takes 5 s to form.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.SpreadShot;
                    w.shapeTime = 5f; w.executeTime = 0f; w.cooldown = 3f; w.cellShare = 0.1f;
                    w.pellets = 5; w.fanAngle = 40f; w.pelletRange = 6f; w.damage = 8f;
                    w.bulletSpeedMultiplier = 2f; w.bulletRadius = 0.15f; w.pierce = 0;
                    break;

                case EnemySporeKey:
                    w.displayName = "Enemy Spore Cloud";
                    w.description = "Boss cloud: Toxic 3 HP/s on you and your minions.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.SporeCloud;
                    w.cooldown = 8f; w.damage = 0f;
                    w.cloudRadius = 2.5f; w.cloudDuration = 4f; w.toxicDps = 3f; w.toxicDuration = 1f;
                    break;

                case EnemyMineKey:
                    w.displayName = "Enemy Mine";
                    w.description = "Boss mine: 20 damage to you and your minions, never to other enemies.";
                    w.kind = AbilityKind.Weapon; w.type = WeaponType.Mine;
                    w.cooldown = 4f; w.damage = 20f;
                    w.mineArmTime = 1f; w.mineTriggerRange = 1.2f; w.mineLifetime = 15f; w.blastRadius = 2f;
                    break;
            }
            return w;
        }
    }
}
