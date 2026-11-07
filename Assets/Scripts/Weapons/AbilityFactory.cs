using Cellstorm.Data;

namespace Cellstorm.Weapons
{
    public static class AbilityFactory
    {
        public static Ability Create(AbilityController owner, WeaponSO data)
        {
            switch (data.type)
            {
                case WeaponType.Ram: return new RamAbility(owner, data);
                case WeaponType.Jaw: return new JawAbility(owner, data);
                case WeaponType.Gun: return new GunAbility(owner, data);
                case WeaponType.Hardening: return new HardeningAbility(owner, data);
                case WeaponType.Whip: return new WhipAbility(owner, data);
                case WeaponType.SpreadShot: return new SpreadShotAbility(owner, data);
                case WeaponType.SporeCloud: return new SporeCloudAbility(owner, data);
                case WeaponType.Mine: return new MineAbility(owner, data);
                case WeaponType.Minion: return new MinionAbility(owner, data);
                case WeaponType.Convert: return new ConvertAbility(owner, data);
                case WeaponType.Shockwave: return new ShockwaveAbility(owner, data);
                default: return null;
            }
        }
    }
}
