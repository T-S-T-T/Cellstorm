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
                default: return null;
            }
        }
    }
}
