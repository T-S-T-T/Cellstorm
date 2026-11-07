using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Key press: splits small AI swarms off the body. Each costs 2.5% of the main swarm's max HP, taken at once (DEF
    /// does not reduce it, no hit flash). Needs the main swarm's body HP to be greater than the total cost, so the skill
    /// can never kill the swarm; the HP held by deployed weapon cells is not spendable. 0.5 s cooldown.
    /// </summary>
    public class MinionAbility : Ability
    {
        public MinionAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        public override float Progress => Phase == AbilityPhase.Cooldown ? CooldownProgress : 0f;

        public int CountNow => Mathf.Clamp(Mathf.RoundToInt(P(Param.MinionCount, Data.minionCount)), 1, 20);
        /// <summary>Cost of ONE minion in HP: x% of max HP, never below 1%.</summary>
        public float CostEach => Mathf.Max(0.01f, P(Param.MinionCost, Data.minionCostFraction)) * Owner.Stats.MaxHp;
        public float TotalCost => CostEach * CountNow;
        /// <summary>Can the skill be used right now (the HUD tints the slot red when not)?</summary>
        public bool CanAfford => Owner.Stats.Hp - Owner.Stats.HeldHp > TotalCost;
        public float ContactDpsNow => Mathf.Max(0f, P(Param.MinionContact, Data.minionContactDps));
        public float HpFor(float cost) => Mathf.Max(0.1f, P(Param.MinionHp, cost));

        public override void Tick(float dt, AbilityInput input)
        {
            switch (Phase)
            {
                case AbilityPhase.Ready:
                    if (input.pressed && CanAfford) Spawn();
                    break;
                case AbilityPhase.Cooldown:
                    TickCooldown(dt);
                    break;
            }
        }

        void Spawn()
        {
            int count = CountNow;
            float each = CostEach;
            float total = each * count;
            var stats = Owner.Stats;

            // HP leaves the main swarm at once. SetHp keeps 0 <= HeldHp <= Hp, and CanAfford guarantees the body can pay.
            stats.SetHp(stats.Hp - total);
            float def = stats.Defense;

            for (int i = 0; i < count; i++)
                Minion.Create(Owner.Position, HpFor(each), def, this, i);

            StartCooldown(CooldownBase);
        }
    }
}
