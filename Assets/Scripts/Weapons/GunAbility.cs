using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Cells line up into a barrel toward the aim direction. When complete and released, fires one bullet
    /// (speed = 2 x moving speed but at least 6, damage from the weapon data reduced by the target's DEF, pierce 0).
    /// V0.2 cards: Rifled Barrel, Piercing/Fast Rounds, Explosive Rounds, the Hunter and Artillery evolutions.
    /// </summary>
    public class GunAbility : ShapedAbility
    {
        public GunAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        const float BarrelLengthUnits = 3f;

        float Barrel => BarrelLengthUnits * P(Param.Size, 1f);
        protected override float ReachUnits => Barrel;

        protected override Vector2 FormationPoint(int index, int count, Vector2 center, float radius, Vector2 aim)
        {
            float t = (index + 1f) / count;
            Vector2 perp = new Vector2(-aim.y, aim.x);
            float thick = (Hash01(index) - 0.5f) * 0.10f;
            return center + aim * (radius + Barrel * t) + perp * thick;
        }

        protected override void OnExecuteStart()
        {
            Vector2 aim = Owner.Host.AimDirection;
            Vector2 muzzle = Owner.Position + aim * (Owner.Radius + Barrel + 0.2f);

            float baseSpeed = Mathf.Max(6f, SpeedMath.MoveSpeed(Owner.Stats.Speed) * Data.bulletSpeedMultiplier);
            float speed = P(Param.ProjectileSpeed, baseSpeed);
            float damage = DamageNow;

            var opt = new BulletOptions
            {
                status = StatusFor(default, muzzle),
                homing = Owner.Mods.Flag(Param.Homing, Data)
            };
            if (Owner.Mods.Flag(Param.ExplosiveRounds, Data))
            {
                opt.explodeRadius = 1f * P(Param.ExplosionSize, 1f);
                opt.explodeDamage = damage * 0.5f * P(Param.ExplosionDamage, 1f);
            }

            Bullet.Spawn(muzzle, aim, speed, damage, Mathf.Max(0, Mathf.RoundToInt(P(Param.Pierce, Data.pierce))),
                Owner.Faction, Owner.CellStyle.color, Data.bulletRadius, opt);
        }
    }
}
