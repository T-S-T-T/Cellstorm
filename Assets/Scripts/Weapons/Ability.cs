using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    public enum AbilityPhase
    {
        Ready,
        Shaping,
        /// <summary>Shape complete, waiting for the key to be released (Jaw / Gun).</summary>
        Primed,
        Executing,
        Cooldown
    }

    public struct AbilityInput
    {
        public bool pressed;
        public bool held;
        public bool released;
    }

    /// <summary>Runtime state of one equipped weapon or skill. Created by AbilityFactory.</summary>
    public abstract class Ability
    {
        public readonly AbilityController Owner;
        public readonly WeaponSO Data;

        public AbilityPhase Phase { get; protected set; } = AbilityPhase.Ready;
        public float CooldownRemaining { get; protected set; }

        float _cooldownTotal = 1f;

        protected Ability(AbilityController owner, WeaponSO data)
        {
            Owner = owner;
            Data = data;
        }

        /// <summary>Multiplier for the owner's turn rate while this ability is active.</summary>
        public virtual float TurnPenalty => 1f;

        /// <summary>0..1 progress of the current phase (for the slot pie overlay later).</summary>
        public abstract float Progress { get; }

        public abstract void Tick(float dt, AbilityInput input);

        /// <summary>Stop everything and clean up (unequip, death).</summary>
        public virtual void Cancel()
        {
            Phase = AbilityPhase.Ready;
            CooldownRemaining = 0f;
        }

        protected void StartCooldown(float seconds)
        {
            if (seconds <= 0f)
            {
                CooldownRemaining = 0f;
                Phase = AbilityPhase.Ready;
                return;
            }
            _cooldownTotal = seconds;
            CooldownRemaining = seconds;
            Phase = AbilityPhase.Cooldown;
        }

        protected void TickCooldown(float dt)
        {
            CooldownRemaining -= dt;
            if (CooldownRemaining <= 0f)
            {
                CooldownRemaining = 0f;
                Phase = AbilityPhase.Ready;
            }
        }

        protected float CooldownProgress => 1f - Mathf.Clamp01(CooldownRemaining / _cooldownTotal);

        public string Status
        {
            get
            {
                switch (Phase)
                {
                    case AbilityPhase.Ready: return "Ready";
                    case AbilityPhase.Shaping: return "Shaping " + Mathf.RoundToInt(Progress * 100f) + "%";
                    case AbilityPhase.Primed: return "PRIMED (release to use)";
                    case AbilityPhase.Executing: return "Executing";
                    default: return "Cooldown " + CooldownRemaining.ToString("F1") + "s";
                }
            }
        }
    }
}
