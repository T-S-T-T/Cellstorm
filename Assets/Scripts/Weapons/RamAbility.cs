using Cellstorm.Data;

namespace Cellstorm.Weapons
{
    /// <summary>Key press: dash toward the aim direction. Execution time 0.</summary>
    public class RamAbility : Ability
    {
        public RamAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        public override float Progress => Phase == AbilityPhase.Cooldown ? CooldownProgress : 0f;

        public override void Tick(float dt, AbilityInput input)
        {
            switch (Phase)
            {
                case AbilityPhase.Ready:
                    if (input.pressed)
                    {
                        Owner.Host.AddKnockback(Owner.Host.AimDirection * Data.dashSpeed);
                        StartCooldown(Data.cooldown);
                    }
                    break;

                case AbilityPhase.Cooldown:
                    TickCooldown(dt);
                    break;
            }
        }
    }
}
