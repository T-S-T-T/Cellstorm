using Cellstorm.Combat;
using Cellstorm.Data;
using Cellstorm.Enemies;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Key press: a ring bursts from the swarm (radius 5). Every enemy inside is pushed 4 units away (0.2 s) and stunned
    /// for 1 s. No damage. Bosses: half the push and half the stun (StatusController). Minions, pickups and bullets are
    /// not affected. 12 s cooldown.
    /// </summary>
    public class ShockwaveAbility : Ability
    {
        public ShockwaveAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        public override float Progress => Phase == AbilityPhase.Cooldown ? CooldownProgress : 0f;

        public override void Tick(float dt, AbilityInput input)
        {
            switch (Phase)
            {
                case AbilityPhase.Ready:
                    if (input.pressed)
                    {
                        Burst();
                        StartCooldown(CooldownBase);
                    }
                    break;
                case AbilityPhase.Cooldown:
                    TickCooldown(dt);
                    break;
            }
        }

        void Burst()
        {
            Vector2 c = Owner.Position;
            float radius = Mathf.Max(0.5f, P(Param.ShockRadius, Data.shockRadius));
            float push = Mathf.Max(0f, P(Param.ShockPush, Data.shockPush));
            float stun = Mathf.Max(0f, P(Param.ShockStun, Data.shockStun));

            RingFx.Spawn(c, radius, 0.35f, new Color(0.7f, 0.9f, 1f));

            // Copy first: stunning can release cells but never removes enemies, still be safe against list changes.
            for (int i = 0; i < Enemy.Active.Count; i++)
            {
                var e = Enemy.Active[i];
                if (e == null || e.Stats.IsDead) continue;
                float reach = radius + e.Radius;
                if (((Vector2)e.transform.position - c).sqrMagnitude > reach * reach) continue;

                var status = e.Stats.Status;
                status.ApplyStun(stun);
                status.Shove(c, push);
            }
        }
    }
}
