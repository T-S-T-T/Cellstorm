using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Progression
{
    public enum PickupType
    {
        Exp,
        Reroll,
        Shells,
        /// <summary>V0.2: heals 30% of max HP (resource point reward).</summary>
        Nutrient,
        /// <summary>V0.2: Speed +2 for 10 seconds (resource point reward).</summary>
        Adrenaline,
        /// <summary>V0.2: small heal dropped while Convert is active.</summary>
        HpOrb
    }

    /// <summary>
    /// EXP orbs, reroll tokens and shells. Collected by touching them, with strong magnetism only when
    /// the swarm is close. They disappear after 2 minutes.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        public const float Lifetime = 120f;
        public const float MagnetRange = 3.5f;
        public const float ShellsDefense = 2f;
        public const float ShellsDuration = 10f;
        public const float AdrenalineSpeed = 2f;
        public const float AdrenalineDuration = 10f;
        public const float NutrientFraction = 0.3f;
        public const float HpOrbLifetime = 10f;
        public static readonly object AdrenalineSource = new object();

        public static readonly List<Pickup> Active = new List<Pickup>();
        public static readonly object ShellsSource = new object();

        static Material _material;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active.Clear();
            _material = null;
        }

        public PickupType Type { get; private set; }
        public int Value { get; private set; }
        /// <summary>HP orbs: HP healed when collected.</summary>
        public float Amount { get; private set; }

        float _life = Lifetime;
        float _radius = 0.2f;
        Color _color = Color.white;
        SpriteRenderer _sr;
        SwarmBody _player;

        public static Pickup Spawn(PickupType type, Vector2 position, int value = 1)
        {
            var go = new GameObject(type + " Pickup");
            go.transform.position = position;
            var p = go.AddComponent<Pickup>();
            p.Init(type, value);
            return p;
        }

        /// <summary>A healing pickup with an exact amount (HP orbs from Convert).</summary>
        public static Pickup SpawnHeal(PickupType type, Vector2 position, float amount)
        {
            var go = new GameObject(type + " Pickup");
            go.transform.position = position;
            var p = go.AddComponent<Pickup>();
            p.Init(type, 1);
            p.Amount = amount;
            return p;
        }

        void Init(PickupType type, int value)
        {
            Type = type;
            Value = Mathf.Max(1, value);

            switch (type)
            {
                case PickupType.Exp:
                    _color = new Color(0.40f, 1.00f, 0.80f);
                    _radius = Mathf.Clamp(0.12f + 0.05f * Mathf.Sqrt(Value), 0.12f, 0.7f); // bigger orb for bigger EXP
                    break;
                case PickupType.Reroll:
                    _color = new Color(0.80f, 0.50f, 1.00f);
                    _radius = 0.28f;
                    break;
                case PickupType.Nutrient:
                    _color = new Color(0.35f, 1.00f, 0.45f);
                    _radius = 0.32f;
                    break;
                case PickupType.Adrenaline:
                    _color = new Color(1.00f, 0.95f, 0.30f);
                    _radius = 0.30f;
                    break;
                case PickupType.HpOrb:
                    _color = new Color(1.00f, 0.35f, 0.45f);
                    _radius = 0.16f;
                    _life = HpOrbLifetime;
                    break;
                default:
                    _color = new Color(1.00f, 0.70f, 0.20f);
                    _radius = 0.28f;
                    break;
            }

            if (_material == null) _material = new Material(Shader.Find("Sprites/Default"));

            var visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * (_radius * 2f);
            _sr = visual.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralSprites.Hard;
            _sr.sharedMaterial = _material;
            _sr.color = _color;
            _sr.sortingOrder = 4;
        }

        void OnEnable()
        {
            Active.Add(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _life -= dt;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (_sr != null && _life < 10f)
            {
                float a = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.time * 12f));
                _sr.color = new Color(_color.r, _color.g, _color.b, a);
            }

            if (_player == null) _player = FindAnyObjectByType<SwarmBody>();
            if (_player == null || _player.IsDead) return;

            Vector2 pos = transform.position;
            Vector2 playerPos = _player.transform.position;
            float dist = Vector2.Distance(pos, playerPos);

            if (dist <= _player.Radius + _radius)
            {
                Collect();
                return;
            }

            float magnet = _player.Radius + MagnetRange;
            if (dist < magnet)
            {
                float t = 1f - dist / magnet;
                float speed = Mathf.Lerp(4f, 20f, t);
                Vector2 next = Vector2.MoveTowards(pos, playerPos, speed * dt);
                transform.position = new Vector3(next.x, next.y, 0f);
            }
        }

        void Collect()
        {
            switch (Type)
            {
                case PickupType.Exp:
                    if (PlayerProgress.Instance != null) PlayerProgress.Instance.AddExp(Value);
                    break;

                case PickupType.Reroll:
                    if (PlayerProgress.Instance != null) PlayerProgress.Instance.AddRerolls(1);
                    break;

                case PickupType.Nutrient:
                    _player.Stats.Heal(_player.Stats.MaxHp * NutrientFraction);        // main swarm only
                    break;

                case PickupType.HpOrb:
                    _player.Stats.Heal(Amount);
                    break;

                case PickupType.Adrenaline:
                {
                    // A new one refreshes the effect instead of stacking.
                    var stats = _player.Stats;
                    stats.RemoveModifiersFrom(AdrenalineSource);
                    stats.AddModifier(StatType.Speed, ModifierOp.Add, AdrenalineSpeed, AdrenalineDuration, AdrenalineSource);
                    break;
                }

                case PickupType.Shells:
                {
                    // A new shell refreshes the effect instead of stacking.
                    var stats = _player.Stats;
                    stats.RemoveModifiersFrom(ShellsSource);
                    stats.AddModifier(StatType.Defense, ModifierOp.Add, ShellsDefense, ShellsDuration, ShellsSource);
                    break;
                }
            }
            Destroy(gameObject);
        }
    }
}
