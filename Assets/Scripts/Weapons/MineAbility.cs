using Cellstorm.Combat;
using Cellstorm.Data;
using Cellstorm.Enemies;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Key press: drop a mine (or several, with Extra Mines). 4 s cooldown, no cells, no HP. A mine arms after 1 s and
    /// explodes when a hostile swarm comes within its trigger range, or after 15 s. Player mines never hurt the player
    /// or minions; enemy mines hurt the player and minions and never other enemies.
    /// </summary>
    public class MineAbility : Ability
    {
        public MineAbility(AbilityController owner, WeaponSO data) : base(owner, data) { }

        public override float Progress => Phase == AbilityPhase.Cooldown ? CooldownProgress : 0f;

        public override void Tick(float dt, AbilityInput input)
        {
            switch (Phase)
            {
                case AbilityPhase.Ready:
                    if (input.pressed)
                    {
                        Drop();
                        StartCooldown(CooldownBase);
                    }
                    break;
                case AbilityPhase.Cooldown:
                    TickCooldown(dt);
                    break;
            }
        }

        void Drop()
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(P(Param.MineCount, 1f)), 1, 12);
            Vector2 c = Owner.Position;
            float ring = Owner.Radius + 0.6f;

            for (int i = 0; i < count; i++)
            {
                Vector2 pos = c;
                if (count > 1)
                {
                    float a = Mathf.PI * 2f * i / count + 0.5f;
                    pos = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ring;
                }

                float damage = DamageNow * P(Param.ExplosionDamage, 1f);
                float blast = Data.blastRadius * P(Param.Size, 1f) * P(Param.ExplosionSize, 1f);
                var spec = new MineEntity.Spec
                {
                    faction = Owner.Faction,
                    damage = damage,
                    blastRadius = blast,
                    triggerRange = Mathf.Max(0.3f, P(Param.TriggerRange, Data.mineTriggerRange)),
                    armTime = Mathf.Max(0.25f, P(Param.ArmTime, Data.mineArmTime)),
                    lifetime = Data.mineLifetime,
                    status = StatusFor(default, pos),
                    color = Owner.Faction == Faction.Player ? Owner.CellStyle.color : new Color(0.95f, 0.45f, 0.25f)
                };
                MineEntity.Create(pos, spec);
            }
        }
    }

    /// <summary>One dropped mine: a pale outline while arming, a solid disc once armed.</summary>
    public class MineEntity : MonoBehaviour
    {
        public struct Spec
        {
            public Faction faction;
            public float damage, blastRadius, triggerRange, armTime, lifetime;
            public StatusPayload status;
            public Color color;
        }

        public static readonly System.Collections.Generic.List<MineEntity> Active = new System.Collections.Generic.List<MineEntity>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active.Clear();
        }

        Spec _spec;
        float _age;
        SpriteRenderer _disc, _ring;
        bool _done;

        public bool Armed => _age >= _spec.armTime;
        public Faction Faction => _spec.faction;

        public static MineEntity Create(Vector2 position, Spec spec)
        {
            var go = new GameObject("Mine");
            go.transform.position = position;
            var m = go.AddComponent<MineEntity>();
            m._spec = spec;

            m._ring = Part(go.transform, FxSprites.Ring, spec.triggerRange * 2f, 6);
            m._disc = Part(go.transform, ProceduralSprites.Hard, 0.3f, 7);
            m.Paint();
            return m;
        }

        static SpriteRenderer Part(Transform parent, Sprite sprite, float diameter, int order)
        {
            var c = new GameObject("Part");
            c.transform.SetParent(parent, false);
            c.transform.localScale = new Vector3(diameter, diameter, 1f);
            var sr = c.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = SpriteMaterials.Default;
            sr.sortingOrder = order;
            return sr;
        }

        void OnEnable() { Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        void Update()
        {
            if (_done) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _age += dt;
            Paint();

            if (_age >= _spec.lifetime) { Detonate(); return; }
            if (!Armed) return;

            Vector2 p = transform.position;
            if (_spec.faction == Faction.Player)
            {
                for (int i = 0; i < Enemy.Active.Count; i++)
                {
                    var e = Enemy.Active[i];
                    if (e == null || e.Stats.IsDead) continue;
                    float reach = _spec.triggerRange + e.Radius;
                    if (((Vector2)e.transform.position - p).sqrMagnitude <= reach * reach) { Detonate(); return; }
                }
            }
            else
            {
                var list = TargetingService.All;
                for (int i = 0; i < list.Count; i++)
                {
                    var t = list[i];
                    if (t == null || t.IsDead) continue;
                    float reach = _spec.triggerRange + t.Radius;
                    if ((t.Position - p).sqrMagnitude <= reach * reach) { Detonate(); return; }
                }
            }
        }

        void Paint()
        {
            var c = _spec.color;
            if (Armed)
            {
                _disc.color = new Color(c.r, c.g, c.b, 1f);
                _ring.color = new Color(c.r, c.g, c.b, 0.55f);
            }
            else
            {
                _disc.color = new Color(c.r, c.g, c.b, 0.25f);
                _ring.color = new Color(c.r, c.g, c.b, 0.18f);   // pale outline while arming
            }
        }

        void Detonate()
        {
            if (_done) return;
            _done = true;
            Vector2 p = transform.position;
            var status = _spec.status;
            status.sourcePos = p;
            DamageUtil.Explode(p, _spec.blastRadius, _spec.damage, _spec.faction, status);
            ExplosionFx.Spawn(p, _spec.blastRadius, _spec.color);
            Destroy(gameObject);
        }
    }
}
