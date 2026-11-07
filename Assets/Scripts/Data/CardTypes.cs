using System;

namespace Cellstorm.Data
{
    /// <summary>GDD V0.2 section 6.4: tags decide which shared upgrade cards apply to a weapon or skill.</summary>
    [Flags]
    public enum WeaponTag
    {
        None = 0,
        Cell = 1,
        Projectile = 2,
        Melee = 4,
        Area = 8,
        Damage = 16,
        Skill = 32,
        Weapon = 64
    }

    public static class WeaponTags
    {
        public static WeaponTag Of(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Ram: return WeaponTag.Melee | WeaponTag.Damage | WeaponTag.Weapon;
                case WeaponType.Jaw: return WeaponTag.Cell | WeaponTag.Melee | WeaponTag.Damage | WeaponTag.Weapon;
                case WeaponType.Gun: return WeaponTag.Cell | WeaponTag.Projectile | WeaponTag.Damage | WeaponTag.Weapon;
                case WeaponType.Whip: return WeaponTag.Cell | WeaponTag.Melee | WeaponTag.Area | WeaponTag.Damage | WeaponTag.Weapon;
                case WeaponType.SpreadShot: return WeaponTag.Cell | WeaponTag.Projectile | WeaponTag.Damage | WeaponTag.Weapon;
                case WeaponType.SporeCloud: return WeaponTag.Area | WeaponTag.Weapon;
                case WeaponType.Mine: return WeaponTag.Area | WeaponTag.Damage | WeaponTag.Weapon;
                default: return WeaponTag.Skill;
            }
        }

        public static bool IsCellWeapon(WeaponType type)
        {
            return (Of(type) & WeaponTag.Cell) != 0;
        }
    }

    /// <summary>
    /// What an upgrade card changes. Every weapon and skill reads its numbers through UpgradeMods.Get(param, base),
    /// so a card is pure data: target + param + operation + amount per stack (GDD V0.2 section 12).
    /// </summary>
    public enum Param
    {
        Damage,
        Cooldown,
        ShapeTime,
        Reach,
        Size,
        ConeAngle,
        ArcAngle,
        FanAngle,
        Pellets,
        Pierce,
        ProjectileSpeed,
        DashSpeed,
        DashWindow,
        PullDistance,
        CloudDuration,
        ToxicDps,
        ToxicDuration,
        SlowPct,
        SlowDuration,
        StunChance,
        StunDuration,
        KnockDistance,
        MineCount,
        ArmTime,
        TriggerRange,
        EffectDuration,
        DefMultiplier,
        MinionCount,
        MinionCost,
        MinionContact,
        MinionHp,
        OrbPct,
        ShockRadius,
        ShockPush,
        ShockStun,
        CellDef,
        CostFraction,
        ExplosionSize,
        ExplosionDamage,
        ExplosiveRounds,
        SecondBite,
        Homing,
        ToxicSpread
    }

    public enum EffectOp
    {
        /// <summary>Adds perStack x stacks (+ firstBonus once owned) to the value.</summary>
        AddFlat,
        /// <summary>Adds (perStack x stacks + firstBonus) x BASE value. Percent adds are summed before the multipliers.</summary>
        AddPercentOfBase,
        /// <summary>Multiplies by perStack ^ stacks.</summary>
        Multiply
    }

    [Serializable]
    public class CardEffect
    {
        public Param param;
        public EffectOp op = EffectOp.AddFlat;
        public float perStack = 1f;
        [UnityEngine.Tooltip("Added once when the card is owned (for example Toxic Tips: 3 HP/s for the first stack, +2 for each more).")]
        public float firstBonus;
        [UnityEngine.Tooltip("Optional: this effect only applies to items with one of these tags (the card itself may match more).")]
        public WeaponTag onlyTags;
        [UnityEngine.Tooltip("Optional: this effect only applies to one weapon type.")]
        public bool filterType;
        public WeaponType onlyType;

        public bool AppliesTo(WeaponType t)
        {
            if (filterType && onlyType != t) return false;
            return onlyTags == WeaponTag.None || (onlyTags & WeaponTags.Of(t)) != WeaponTag.None;
        }
    }

    public enum StackForm
    {
        Additive,
        Multiplicative,
        Diminishing,
        Once
    }

    public enum EvoKind
    {
        None,
        Tank,
        Swarm,
        Toxic,
        Hunter,
        Artillery
    }

    public enum EvoCondition
    {
        /// <summary>N stacks of one card.</summary>
        Stacks,
        /// <summary>Own one of the listed weapons plus N cards of a card group.</summary>
        WeaponAndCards,
        /// <summary>Defeat any boss and own one of the listed weapons.</summary>
        BossReward
    }
}
