using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Cells form a wide muzzle; on release fires 5 pellets in a 40 degree fan (40 damage each, pierce 0, 6 units range).
    /// Cards: Extra Pellets, Choke, Piercing/Fast Rounds, Heavy Hits, the Hunter evolution ...
    /// </summary>
    public class SpreadShotAbility : ShapedAbility
    {
        const float MuzzleLength = 1.5f;

        public SpreadShotAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        float Fan => Mathf.Max(0f, P(Param.FanAngle, Data.fanAngle));
        float Length => MuzzleLength * P(Param.Size, 1f);
        protected override float ReachUnits => Length;

        protected override Vector2 FormationPoint(int index, int count, Vector2 center, float radius, Vector2 aim)
        {
            // A flared nozzle: two arms opening a little wider than the fan.
            int arms = Mathf.Max(1, (count + 1) / 2);
            int rank = index / 2;
            float side = (index % 2 == 0) ? 1f : -1f;
            float t = (rank + 1f) / arms;
            float half = Mathf.Max(8f, Fan * 0.5f + 8f);

            Vector2 dir = Rotate(aim, side * half * t);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float thick = (Hash01(index) - 0.5f) * 0.10f;
            return center + dir * (radius + Length * (0.35f + 0.65f * t)) + perp * thick;
        }

        protected override void OnExecuteStart()
        {
            Vector2 aim = Owner.Host.AimDirection;
            Vector2 muzzle = Owner.Position + aim * (Owner.Radius + Length * 0.6f);

            int n = Mathf.Max(1, Mathf.RoundToInt(P(Param.Pellets, Data.pellets)));
            float fan = Fan;

            float baseSpeed = Mathf.Max(6f, SpeedMath.MoveSpeed(Owner.Stats.Speed) * Data.bulletSpeedMultiplier);
            float speed = P(Param.ProjectileSpeed, baseSpeed);
            float damage = DamageNow;
            int pierce = Mathf.Max(0, Mathf.RoundToInt(P(Param.Pierce, Data.pierce)));

            var opt = new BulletOptions
            {
                status = StatusFor(default, muzzle),
                homing = Owner.Mods.Flag(Param.Homing, Data),
                maxRange = Data.pelletRange
            };

            for (int i = 0; i < n; i++)
            {
                float angle = n == 1 ? 0f : -fan * 0.5f + fan * i / (n - 1f);
                Bullet.Spawn(muzzle, Rotate(aim, angle), speed, damage, pierce,
                    Owner.Faction, Owner.CellStyle.color, Data.bulletRadius, opt);
            }
        }
    }
}
