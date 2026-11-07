using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>The Convert effect state read by enemies: while active, damage dealt by the player drops HP orbs.</summary>
    public static class ConvertEffect
    {
        public static bool Active { get; private set; }
        /// <summary>Share of the damage that comes back as healing (0.1 = 10%).</summary>
        public static float Fraction { get; private set; } = 0.1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active = false;
            Fraction = 0.1f;
        }

        public static void Begin(float fraction)
        {
            Active = true;
            Fraction = Mathf.Max(0f, fraction);
        }

        public static void End()
        {
            Active = false;
        }
    }

    /// <summary>
    /// Key press: for 5 s, enemies that take damage from the player (weapons, Ram, minions) drop HP orbs that heal the
    /// main swarm by 10% of the damage they took. 15 s cooldown, starting when the effect ends.
    /// </summary>
    public class ConvertAbility : Ability
    {
        float _timer;
        float _duration = 5f;

        public ConvertAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        public override float Progress
        {
            get
            {
                if (Phase == AbilityPhase.Executing) return 1f - Mathf.Clamp01(_timer / Mathf.Max(0.01f, _duration));
                return Phase == AbilityPhase.Cooldown ? CooldownProgress : 0f;
            }
        }

        public override void Tick(float dt, AbilityInput input)
        {
            switch (Phase)
            {
                case AbilityPhase.Ready:
                    if (input.pressed)
                    {
                        _duration = Mathf.Max(0.5f, P(Param.EffectDuration, Data.executeTime));
                        _timer = _duration;
                        ConvertEffect.Begin(P(Param.OrbPct, Data.orbFraction));
                        Phase = AbilityPhase.Executing;
                    }
                    break;

                case AbilityPhase.Executing:
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        ConvertEffect.End();
                        StartCooldown(CooldownBase);
                    }
                    break;

                case AbilityPhase.Cooldown:
                    TickCooldown(dt);
                    break;
            }
        }

        public override void Cancel()
        {
            if (Phase == AbilityPhase.Executing) ConvertEffect.End();
            base.Cancel();
        }
    }
}
