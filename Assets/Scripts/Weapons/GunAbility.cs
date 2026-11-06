using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Cells line up into a barrel toward the aim direction. When complete and released, fires one bullet
    /// (speed = moving speed x 2 but at least 6, damage from the weapon data reduced by the target's DEF, pierce 0)
    /// with execution time 0.
    /// </summary>
    public class GunAbility : ShapedAbility
    {
        public GunAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        static float BarrelLength(int count)
        {
            return Mathf.Clamp(0.6f + count * 0.3f, 0.9f, 3.5f);
        }

        protected override Vector2 FormationPoint(int index, int count, Vector2 center, float radius, Vector2 aim)
        {
            float distance = radius + BarrelLength(count) * (index + 1) / count;
            return center + aim * distance;
        }

        protected override void OnExecuteStart()
        {
            Vector2 aim = Owner.Host.AimDirection;
            Vector2 muzzle = Owner.Position + aim * (Owner.Radius + BarrelLength(CellCount) + 0.2f);

            float speed = Mathf.Max(6f, SpeedMath.MoveSpeed(Owner.Stats.Speed) * Data.bulletSpeedMultiplier);
            float damage = Data.damage * Owner.DamageMultiplier;

            Bullet.Spawn(muzzle, aim, speed, damage, Data.pierce,
                Owner.Faction, Owner.CellStyle.color, Data.bulletRadius);
        }
    }
}
