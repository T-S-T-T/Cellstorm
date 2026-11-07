using System.Collections.Generic;
using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Enemies;
using Cellstorm.Progression;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.World
{
    public enum GuardKind
    {
        Group,
        Elite,
        Mixed
    }

    /// <summary>
    /// A marked spot guarded by enemies (GDD V0.2 section 8.2). Defeating every guard captures it and drops the reward:
    /// an EXP burst plus pickups that depend on the guards. An uncaptured point disappears after 3 minutes and its
    /// remaining guards despawn. Guards stand near the point until the player comes within 8 units.
    /// </summary>
    public class ResourcePoint : MonoBehaviour
    {
        public const float Lifetime = 180f;
        public const int MaxGuards = 10;

        public static readonly List<ResourcePoint> Active = new List<ResourcePoint>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active.Clear();
        }

        public GuardKind Kind { get; private set; }
        public float Age { get; private set; }
        public int GuardsLeft => _guards.Count;
        public bool Captured { get; private set; }

        readonly List<Enemy> _guards = new List<Enemy>();
        SpriteRenderer _ring;
        SpriteRenderer _zone;
        float _poll;
        bool _closing;

        /// <summary>Creates the point and its guards from the spawn row of the milestone.</summary>
        public static ResourcePoint Create(Vector2 position, GuardKind kind, int milestone, EnemySpawner spawner)
        {
            var go = new GameObject("Resource Point (" + kind + ")");
            go.transform.position = position;
            var p = go.AddComponent<ResourcePoint>();
            p.Kind = kind;
            p.Build();
            p.SpawnGuards(milestone, spawner);
            return p;
        }

        void Build()
        {
            _zone = Part(ProceduralSprites.Hard, 6f, -6);
            _zone.color = new Color(1f, 0.85f, 0.3f, 0.07f);
            _ring = Part(FxSprites.Ring, 3f, -5);
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

        void SpawnGuards(int milestone, EnemySpawner spawner)
        {
            if (spawner == null) return;
            Vector2 center = transform.position;

            switch (Kind)
            {
                case GuardKind.Group:
                {
                    int n = Random.Range(6, 11);                  // 6 to 10 normal enemies
                    for (int i = 0; i < n; i++) AddNormal(spawner, milestone, center);
                    break;
                }
                case GuardKind.Elite:
                    AddElite(spawner, milestone, center);
                    break;
                default:
                {
                    AddElite(spawner, milestone, center);
                    int n = Random.Range(3, 6);                   // an elite plus 3 to 5 normal enemies
                    for (int i = 0; i < n; i++) AddNormal(spawner, milestone, center);
                    break;
                }
            }
        }

        void AddNormal(EnemySpawner spawner, int milestone, Vector2 center)
        {
            if (_guards.Count >= MaxGuards) return;
            var type = spawner.PickNormalType(milestone);
            if (type == null) return;
            _guards.Add(spawner.SpawnGuard(type, milestone, center + Random.insideUnitCircle * 3f, center, 1f, 1f));
        }

        void AddElite(EnemySpawner spawner, int milestone, Vector2 center)
        {
            if (_guards.Count >= MaxGuards) return;
            var type = spawner.ToughestType(milestone);
            if (type == null) return;
            // The toughest type in the row with 3 x max HP and 1.5 x damage.
            _guards.Add(spawner.SpawnGuard(type, milestone, center + Random.insideUnitCircle * 1.5f, center, 3f, 1.5f));
        }

        void OnEnable() { Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Captured || _closing) return;

            Age += dt;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 3f);
            _ring.color = new Color(1f, 0.85f, 0.3f, 0.5f + 0.5f * pulse);
            float s = 3f + 0.2f * pulse;
            _ring.transform.localScale = new Vector3(s, s, 1f);

            if (Age >= Lifetime)
            {
                Expire();
                return;
            }

            _poll -= dt;
            if (_poll > 0f) return;
            _poll = 0.25f;

            // Guards that died anywhere (or exploded) count, so look for guards that no longer exist.
            for (int i = _guards.Count - 1; i >= 0; i--)
            {
                var g = _guards[i];
                if (g == null || g.Stats.IsDead) _guards.RemoveAt(i);
            }
            if (_guards.Count == 0) Capture();
        }

        void Capture()
        {
            if (Captured) return;
            Captured = true;
            Drop();
            Destroy(gameObject);
        }

        /// <summary>Guards still alive when the point disappears despawn.</summary>
        void Expire()
        {
            _closing = true;
            foreach (var g in _guards)
                if (g != null) g.Despawn();
            _guards.Clear();
            Destroy(gameObject);
        }

        void Drop()
        {
            Vector2 p = transform.position;
            var progress = PlayerProgress.Instance;
            float frac = Kind == GuardKind.Group ? 0.5f : (Kind == GuardKind.Elite ? 0.75f : 1f);
            int burst = progress != null ? Mathf.Max(1, Mathf.RoundToInt(progress.ExpToNext * frac)) : 5;

            // EXP burst: a few orbs sharing the total.
            int orbs = Mathf.Min(5, burst);
            int each = burst / orbs;
            int rest = burst - each * orbs;
            for (int i = 0; i < orbs; i++)
                Pickup.Spawn(PickupType.Exp, p + Random.insideUnitCircle * 0.8f, each + (i == 0 ? rest : 0));

            // Pickups: Group 1 random, Elite 2 different, Mixed all three (Nutrient, a buff, a reroll token).
            var kinds = new List<int> { 0, 1, 2 };            // 0 nutrient, 1 buff, 2 reroll
            int count = Kind == GuardKind.Group ? 1 : (Kind == GuardKind.Elite ? 2 : 3);
            for (int i = 0; i < count; i++)
            {
                int pick = Random.Range(0, kinds.Count);
                SpawnReward(kinds[pick], p + Random.insideUnitCircle * 0.9f);
                kinds.RemoveAt(pick);
            }
        }

        static void SpawnReward(int kind, Vector2 position)
        {
            switch (kind)
            {
                case 0: Pickup.Spawn(PickupType.Nutrient, position); break;
                case 1: Pickup.Spawn(Random.value < 0.5f ? PickupType.Shells : PickupType.Adrenaline, position); break;
                default: Pickup.Spawn(PickupType.Reroll, position); break;
            }
        }

        /// <summary>Debug: capture right now.</summary>
        public void DebugCapture()
        {
            foreach (var g in _guards)
                if (g != null) g.Despawn();
            _guards.Clear();
            Capture();
        }
    }
}
