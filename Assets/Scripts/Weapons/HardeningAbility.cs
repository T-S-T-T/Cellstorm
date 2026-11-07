using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>Key press: DEF multiplier for the effect duration, then cooldown (starts when the effect ends).</summary>
    public class HardeningAbility : Ability
    {
        StatModifier _mod;
        float _timer;
        float _duration = 3f;

        public HardeningAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        public override float Progress
        {
            get
            {
                if (Phase == AbilityPhase.Executing) return 1f - Mathf.Clamp01(_timer / Mathf.Max(0.01f, _duration));
                if (Phase == AbilityPhase.Cooldown) return CooldownProgress;
                return 0f;
            }
        }

        public override void Tick(float dt, AbilityInput input)
        {
            switch (Phase)
            {
                case AbilityPhase.Ready:
                    if (input.pressed)
                    {
                        _duration = Mathf.Max(0.1f, P(Param.EffectDuration, Data.executeTime));
                        _mod = Owner.Stats.AddModifier(StatType.Defense, ModifierOp.Multiply, P(Param.DefMultiplier, Data.defMultiplier), -1f, this);
                        _timer = _duration;
                        Phase = AbilityPhase.Executing;
                    }
                    break;

                case AbilityPhase.Executing:
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        RemoveMod();
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
            RemoveMod();
            base.Cancel();
        }

        void RemoveMod()
        {
            if (_mod != null)
            {
                Owner.Stats.RemoveModifier(_mod);
                _mod = null;
            }
        }
    }
}
