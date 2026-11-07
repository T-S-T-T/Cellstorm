using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Enemies;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.World
{
    public enum HazardKind
    {
        Toxic,
        Current
    }

    /// <summary>
    /// A patch on the ground (GDD V0.2 section 8.3): a toxic zone (Toxic 5 HP/s on every swarm inside, 1 s after leaving)
    /// or a current (pushes swarms 3 units/s in one direction). Fades in over 2 s (harmless), lasts 20 s, fades out over 2 s.
    /// Resistance is the swarm's DEF reduction: both the Toxic damage (through the normal DEF in TakeDamage) and the push
    /// are reduced by it, and a swarm at 90% reduction (DEF 50 or more) ignores hazards completely.
    /// Hazards affect bodies only (not weapon cells) and hurt the player, minions and enemies alike. A kill that only
    /// hazards caused gives no score and no EXP (the Toxic ticks are marked as hazard damage).
    /// </summary>
    public class HazardPatch : MonoBehaviour
    {
        public const float FadeIn = 2f;
        public const float Lifetime = 20f;
        public const float FadeOut = 2f;
        public const float ToxicDps = 5f;
        public const float ToxicSeconds = 1f;
        public const float PushSpeed = 3f;
        const float TickSeconds = 0.1f;

        public static readonly List<HazardPatch> Active = new List<HazardPatch>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active.Clear();
        }

        public HazardKind Kind { get; private set; }
        public float Radius { get; private set; }
        public Vector2 Direction { get; private set; }
        public float Age { get; private set; }

        /// <summary>Harmless while it fades in or out.</summary>
        public bool Harmful => Age >= FadeIn && Age < FadeIn + Lifetime;

        SpriteRenderer _disc;
        SpriteRenderer _edge;
        readonly List<SpriteRenderer> _arrows = new List<SpriteRenderer>();
        float _tick;
        Color _color;

        public static HazardPatch Create(Vector2 position, HazardKind kind, float radius, Vector2 direction)
        {
            var go = new GameObject("Hazard " + kind);
            go.transform.position = position;
            var p = go.AddComponent<HazardPatch>();
            p.Kind = kind;
            p.Radius = radius;
            p.Direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            p.Build();
            return p;
        }

        void Build()
        {
            _color = Kind == HazardKind.Toxic ? new Color(0.30f, 0.85f, 0.25f) : new Color(0.30f, 0.55f, 1.00f);

            _disc = Part(ProceduralSprites.Hard, Radius * 2f, -5);
            _edge = Part(FxSprites.Ring, Radius * 2f, -4);

            if (Kind == HazardKind.Current)
            {
                float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
                int n = Mathf.Max(3, Mathf.RoundToInt(Radius * 1.6f));
                for (int i = 0; i < n; i++)
                {
                    var a = Part(FxSprites.Arrow, Mathf.Min(0.7f, Radius * 0.35f), -3);
                    a.transform.localPosition = Random.insideUnitCircle * (Radius * 0.7f);
                    a.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                    _arrows.Add(a);
                }
            }
            Paint();
        }

        SpriteRenderer Part(Sprite sprite, float diameter, int order)
        {
            var c = new GameObject("Part");
            c.transform.SetParent(transform, false);
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
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Age += dt;
            if (Age >= FadeIn + Lifetime + FadeOut)
            {
                Destroy(gameObject);
                return;
            }
            Paint();

            if (!Harmful) return;
            _tick -= dt;
            if (_tick > 0f) return;
            _tick = TickSeconds;
            Affect();
        }

        float Visibility()
        {
            if (Age < FadeIn) return Age / FadeIn;
            if (Age > FadeIn + Lifetime) return Mathf.Clamp01(1f - (Age - FadeIn - Lifetime) / FadeOut);
            return 1f;
        }

        void Paint()
        {
            float v = Visibility();
            _disc.color = new Color(_color.r, _color.g, _color.b, 0.28f * v);
            _edge.color = new Color(_color.r, _color.g, _color.b, 0.65f * v);
            for (int i = 0; i < _arrows.Count; i++)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f + i);
                _arrows[i].color = new Color(_color.r, _color.g, _color.b, (0.35f + 0.4f * pulse) * v);
            }
        }

        void Affect()
        {
            Vector2 c = transform.position;
            float r2 = Radius * Radius;

            for (int i = 0; i < Enemy.Active.Count; i++)
            {
                var e = Enemy.Active[i];
                if (e == null || e.Stats.IsDead) continue;
                if (((Vector2)e.transform.position - c).sqrMagnitude <= r2) Hit(e.Stats);
            }

            var targets = TargetingService.All;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (!TargetingService.IsValid(t)) continue;
                if ((t.Position - c).sqrMagnitude <= r2) Hit(t.Stats);
            }
        }

        void Hit(SwarmStats stats)
        {
            float resist = DefCurve.Reduction(stats.Defense);
            if (resist >= DefCurve.MaxReduction - 0.0001f) return;       // DEF 50 or more: immune

            var status = stats.Status;
            if (Kind == HazardKind.Toxic)
                status.ApplyToxic(ToxicDps, ToxicSeconds, Faction.Enemy, true);    // each tick is reduced by DEF in TakeDamage
            else
                status.SetExternalVelocity(Direction * PushSpeed * (1f - resist)); // the push is reduced by the same resistance
        }
    }
}
