using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Enemies;
using Cellstorm.Progression;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// A small AI swarm split off the player (GDD V0.2 section 5.10): radius 0.15, moves toward the nearest enemy at
    /// 1.5 x the main swarm's moving speed (at least 3), deals contact damage (10/s) while touching, orbits the player
    /// when no enemy is within 15 units. HP = what it cost, DEF = the player's DEF at spawn, no regen, never merges back.
    /// Cheap body: a circle moved by script. Enemies chase the nearest of the player and its minions (TargetingService).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SwarmStats))]
    public class Minion : MonoBehaviour, ITargetable, IKnockbackable
    {
        public const float BodyRadius = 0.15f * SwarmSize.Scale;
        public const float SeekRange = 15f;
        /// <summary>An unnamed physics layer so minions can pass through each other (layer 29 is not used elsewhere).</summary>
        public const int PhysicsLayer = 29;

        public static readonly List<Minion> Active = new List<Minion>();

        static bool _layersSet;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active.Clear();
            _layersSet = false;
        }

        Rigidbody2D _rb;
        SwarmStats _stats;
        SwarmCombat _combat;
        StatusController _status;
        SpriteRenderer _sr;
        MinionAbility _ability;
        Color _color;
        Vector2 _knockback;
        ShoveState _shove;
        Enemy _target;
        float _retarget;
        float _refresh;
        float _orbitAngle;
        float _orbitRadius;
        float _flash;

        // ITargetable
        public Vector2 Position => transform.position;
        public float Radius => BodyRadius;
        public bool IsDead => _stats != null && _stats.IsDead;
        public SwarmStats Stats => _stats;

        public static int Count => Active.Count;

        public static Minion Create(Vector2 position, float hp, float defense, MinionAbility ability, int index)
        {
            if (!_layersSet)
            {
                _layersSet = true;
                Physics2D.IgnoreLayerCollision(PhysicsLayer, PhysicsLayer, true);     // minions pass through each other
            }

            var go = new GameObject("Minion");
            go.layer = PhysicsLayer;
            go.transform.position = position + Random.insideUnitCircle * 0.3f;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.mass = 0.3f;
            rb.linearDamping = 0f;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = SwarmSize.Collider(BodyRadius);

            go.AddComponent<SwarmStats>().Configure(hp, 0f, defense, 2f);
            var combat = go.AddComponent<SwarmCombat>();
            combat.SetFaction(Faction.Player);
            combat.IsMinion = true;
            combat.ContactDps = ability.ContactDpsNow;

            // Minions do not push the player's body around (enemies share its layer, so this is a per-collider rule).
            var player = PlayerBuild.Instance;
            if (player != null && player.TryGetComponent<Collider2D>(out var playerCol))
                Physics2D.IgnoreCollision(col, playerCol, true);

            var m = go.AddComponent<Minion>();
            m._ability = ability;
            m._color = ability.Owner.CellStyle.color;
            m._orbitAngle = index * 2.4f + Random.value * 6.28f;
            m._orbitRadius = 0.9f + 0.35f * (index % 4);
            m._retarget = Random.value * 0.25f;
            return m;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stats = GetComponent<SwarmStats>();
            _combat = GetComponent<SwarmCombat>();
            _status = _stats.Status;
            _stats.Damaged += OnDamaged;

            var v = new GameObject("Visual");
            v.transform.SetParent(transform, false);
            v.transform.localScale = Vector3.one * (BodyRadius * 2f);
            _sr = v.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralSprites.Hard;
            _sr.sharedMaterial = SpriteMaterials.Default;
            _sr.sortingOrder = 8;
        }

        void OnEnable()
        {
            Active.Add(this);
            TargetingService.Register(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
            TargetingService.Unregister(this);
        }

        void OnDestroy()
        {
            if (_stats != null) _stats.Damaged -= OnDamaged;
        }

        void OnDamaged(float amount)
        {
            _flash = 0.1f;
        }

        public void AddKnockback(Vector2 velocity)
        {
            _knockback = Vector2.ClampMagnitude(_knockback + velocity, 12f);
        }

        public void Shove(Vector2 displacement, float duration)
        {
            _shove.Set(displacement, duration);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || IsDead) return;

            _retarget -= dt;
            if (_retarget <= 0f)
            {
                _retarget = 0.25f;
                PickTarget();
            }

            _refresh -= dt;
            if (_refresh <= 0f)
            {
                _refresh = 0.5f;
                // Sharp Minions and the Swarm evolution also boost minions that already exist.
                if (_ability != null) _combat.ContactDps = _ability.ContactDpsNow;
            }

            if (_flash > 0f) _flash -= dt;
            var c = _status.Tint(_color);
            _sr.color = _flash > 0f ? Color.Lerp(c, Color.white, 0.8f) : c;
        }

        void PickTarget()
        {
            _target = null;
            float best = SeekRange;
            Vector2 p = transform.position;
            for (int i = 0; i < Enemy.Active.Count; i++)
            {
                var e = Enemy.Active[i];
                if (e == null || e.Stats.IsDead) continue;
                float gap = ((Vector2)e.transform.position - p).magnitude - e.Radius - BodyRadius;
                if (gap < best)
                {
                    best = gap;
                    _target = e;
                }
            }
        }

        void FixedUpdate()
        {
            if (IsDead) return;
            if (GameManagerPaused()) { _rb.linearVelocity = Vector2.zero; return; }

            float dt = Time.fixedDeltaTime;
            Vector2 vel = Vector2.zero;

            if (!_status.Stunned)
            {
                var owner = PlayerBuild.Instance;
                float mainSpeed = owner != null ? SpeedMath.MoveSpeed(owner.Stats.Speed) : 2f;
                float speed = Mathf.Max(3f, 1.5f * mainSpeed) * _status.SpeedFactor;
                Vector2 pos = transform.position;

                if (_target != null && !_target.Stats.IsDead)
                {
                    Vector2 to = (Vector2)_target.transform.position - pos;
                    if (to.sqrMagnitude > 0.0001f) vel = to.normalized * speed;
                }
                else if (owner != null)
                {
                    _orbitAngle += 1.6f * dt;
                    Vector2 want = (Vector2)owner.transform.position
                                   + new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * _orbitRadius;
                    Vector2 to = want - pos;
                    float d = to.magnitude;
                    vel = d > 0.02f ? to / d * Mathf.Min(speed, d / dt) : Vector2.zero;
                }
            }

            _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, 20f * dt);
            _rb.linearVelocity = vel + _knockback + _shove.Step(dt) + _status.External;
        }

        static bool GameManagerPaused()
        {
            var gm = Cellstorm.Core.GameManager.Instance;
            return gm != null && !gm.IsPlaying;
        }
    }
}
