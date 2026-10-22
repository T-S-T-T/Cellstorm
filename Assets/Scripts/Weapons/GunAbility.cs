using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Cells line up into a barrel toward the aim direction. When complete and released, fires one bullet
    /// (speed = swarm speed x 2, DEF = swarm DEF x 2, pierce 0) with execution time 0.
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

            float speed = Owner.Stats.Speed * Data.bulletSpeedMultiplier * AbilityController.UnitsPerSpeedPoint;
            float defense = Owner.Stats.Defense * Data.bulletDefMultiplier;

            Bullet.Spawn(muzzle, aim, speed, defense, Data.pierce, Data.autoWin,
                Owner.Faction, Owner.CellStyle.color, Data.bulletRadius, Owner.Stats.Cells);
        }
    }
}
