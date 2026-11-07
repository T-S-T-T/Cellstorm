using Cellstorm.Combat;
using Cellstorm.Data;
using Cellstorm.Enemies;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Key press: a cloud (radius 2.5, 4 s) follows the swarm and applies Toxic (5 HP/s, 1 s after leaving) to every
    /// hostile swarm inside. No cells, no HP, no direct damage, not affected by Speed. The 8 s cooldown starts when the
    /// cloud ends. The enemy version poisons the player and minions for 3 HP/s.
    /// </summary>
    public class SporeCloudAbility : Ability
    {
        const float TickSeconds = 0.25f;

        float _timer;
        float _duration = 4f;
        float _tick;
        SporeCloudFx _fx;

        public SporeCloudAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        public float Radius => Mathf.Max(0.5f, Data.cloudRadius * P(Param.Size, 1f));

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
                    if (input.pressed) Begin();
                    break;

                case AbilityPhase.Executing:
                    _timer -= dt;
                    _tick -= dt;
                    if (_tick <= 0f)
                    {
                        _tick = TickSeconds;
                        Apply();
                    }
                    if (_timer <= 0f)
                    {
                        EndFx();
                        StartCooldown(CooldownBase);          // starts when the cloud ends
                    }
                    break;

                case AbilityPhase.Cooldown:
                    TickCooldown(dt);
                    break;
            }
        }

        void Begin()
        {
            _duration = Mathf.Max(0.5f, P(Param.CloudDuration, Data.cloudDuration));
            _timer = _duration;
            _tick = 0f;
            Phase = AbilityPhase.Executing;
            var color = Owner.Faction == Faction.Player ? new Color(0.35f, 0.9f, 0.35f) : new Color(0.55f, 0.85f, 0.25f);
            _fx = SporeCloudFx.Spawn(Owner.transform, Radius, color);
        }

        void Apply()
        {
            Vector2 c = Owner.Position;
            float radius = Radius;
            var baseP = new StatusPayload
            {
                toxicDps = Data.toxicDps,
                toxicDuration = Data.toxicDuration,
                slowDuration = 1f            // Choking Cloud: slowed as long as they are inside, +1 s after
            };
            var payload = Owner.Mods.Status(Data, baseP, c);

            if (Owner.Faction == Faction.Player)
            {
                for (int i = 0; i < Enemy.Active.Count; i++)
                {
                    var e = Enemy.Active[i];
                    if (e == null || e.Stats.IsDead) continue;
                    float reach = radius + e.Radius;
                    if (((Vector2)e.transform.position - c).sqrMagnitude <= reach * reach)
                        e.Stats.Status.Apply(payload, Owner.Faction, false);
                }
            }
            else
            {
                var list = TargetingService.All;
                for (int i = 0; i < list.Count; i++)
                {
                    var t = list[i];
                    if (t == null || t.IsDead) continue;
                    float reach = radius + t.Radius;
                    if ((t.Position - c).sqrMagnitude <= reach * reach)
                        t.Stats.Status.Apply(payload, Owner.Faction, false);
                }
            }
        }

        void EndFx()
        {
            if (_fx != null) _fx.End();
            _fx = null;
        }

        public override void Cancel()
        {
            EndFx();
            base.Cancel();
        }
    }
}
