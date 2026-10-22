using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Data;
using Cellstorm.Swarm;
using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.Enemies
{
    /// <summary>
    /// A regular enemy or boss: builds the swarm components, runs one of three movement behaviours
    /// (chase, keep distance, explode) and, if it has a weapon, a simple weapon AI that uses the same
    /// abilities as the player (Ram, Jaw, Gun).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SwarmStats))]
    public class Enemy : MonoBehaviour, IAbilityHost
    {
        public const float UnitsPerSpeedPoint = 2.5f;

        public static readonly List<Enemy> Active = new List<Enemy>();

        /// <summary>Raised when an enemy dies from damage (not when it despawns or explodes).</summary>
        public static event System.Action<Enemy> Killed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active.Clear();
            Killed = null;
        }

        [SerializeField] float turnRateDegPerSec = 240f;

        public EnemySpec Spec { get; private set; }
        public SwarmStats Stats { get; private set; }
        public bool IsBoss => Spec.isBoss;
        public int ScoreValue => Spec.scorePerCell ? Spec.scoreValue * Spec.maxCells : Spec.scoreValue;

        public float TurnRateMultiplier { get; set; } = 1f;
        public Vector2 AimDirection =>
            new Vector2(Mathf.Cos(_aimAngle * Mathf.Deg2Rad), Mathf.Sin(_aimAngle * Mathf.Deg2Rad));

        Rigidbody2D _rb;
        CircleCollider2D _col;
        SwarmBody _target;
        AbilityController _abilities;
        Vector2 _knockback;
        float _aimAngle;
        bool _aimInit;
        bool _finished;

        public static Enemy Create(EnemySpec spec, Vector2 position)
        {
            // Ranged enemies always carry the enemy gun, even if their data asset predates weapons.
            if (spec.weapon == null && spec.behavior == EnemyBehavior.KeepDistance)
                spec.weapon = WeaponDefaults.EnemyGun;

            var go = new GameObject(spec.name);
            go.transform.position = position;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.mass = spec.isBoss ? 20f : 1f;

            go.AddComponent<CircleCollider2D>();
            go.AddComponent<SwarmStats>().Configure(spec.maxCells, spec.regen, spec.defense, spec.speed);
            go.AddComponent<SwarmCombat>().SetFaction(Faction.Enemy);
            go.AddComponent<CircleSwarmVisual>().SetColor(spec.color);

            var enemy = go.AddComponent<Enemy>();
            enemy.Spec = spec;

            if (spec.weapon != null)
            {
                enemy._abilities = go.AddComponent<AbilityController>();
                enemy._abilities.Equip(0, spec.weapon);
            }
            return enemy;
        }

        public static int CountNonBoss()
        {
            int n = 0;
            for (int i = 0; i < Active.Count; i++)
                if (!Active[i].IsBoss) n++;
            return n;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CircleCollider2D>();
            Stats = GetComponent<SwarmStats>();
            Stats.Died += OnStatsDied;
        }

        void OnEnable()
        {
            Active.Add(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
        }

        void OnDestroy()
        {
            if (Stats != null) Stats.Died -= OnStatsDied;
        }

        void OnStatsDied()
        {
            if (_finished) return;
            _finished = true;
            Killed?.Invoke(this);
            // SwarmCombat destroys the object.
        }

        public void AddKnockback(Vector2 velocity)
        {
            _knockback = Vector2.ClampMagnitude(_knockback + velocity, 12f);
        }

        /// <summary>Remove without reward (too far from the player).</summary>
        public void Despawn()
        {
            if (_finished) return;
            _finished = true;
            Destroy(gameObject);
        }

        void Update()
        {
            if (_finished) return;

            if (_target == null) _target = FindAnyObjectByType<SwarmBody>();
            if (_target == null) return;

            Vector2 to = (Vector2)_target.transform.position - (Vector2)transform.position;
            float targetAngle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;

            if (!_aimInit)
            {
                _aimAngle = targetAngle;
                _aimInit = true;
            }
            if (!_target.IsDead)
            {
                _aimAngle = Mathf.MoveTowardsAngle(
                    _aimAngle, targetAngle, turnRateDegPerSec * TurnRateMultiplier * Time.deltaTime);
            }

            UpdateWeaponAI(to);
        }

        void UpdateWeaponAI(Vector2 to)
        {
            if (_abilities == null) return;
            var ability = _abilities.GetSlot(0);
            if (ability == null) return;

            bool held = false;

            if (!_target.IsDead)
            {
                var data = ability.Data;
                float dist = to.magnitude;
                float edge = dist - _target.Radius - _col.radius;
                float angleError = Vector2.Angle(AimDirection, to);

                switch (data.type)
                {
                    case WeaponType.Gun:
                    {
                        // Start forming the gun when in range; fire once complete and lined up.
                        bool inRange = dist <= Spec.preferredRange + 4f;
                        bool aligned = angleError < 8f;
                        held = inRange && !(ability.Phase == AbilityPhase.Primed && aligned);
                        break;
                    }
                    case WeaponType.Jaw:
                    {
                        bool close = edge <= data.coneRange + 1.5f;
                        bool inBiteRange = edge <= data.coneRange * 0.9f && angleError < 20f;
                        held = close && !(ability.Phase == AbilityPhase.Primed && inBiteRange);
                        break;
                    }
                    case WeaponType.Ram:
                        held = dist > 3f && dist < 9f && angleError < 15f;
                        break;
                }
            }

            _abilities.SetAiHeld(0, held);
        }

        void FixedUpdate()
        {
            if (_finished) return;

            if (_target == null) _target = FindAnyObjectByType<SwarmBody>();

            Vector2 move = Vector2.zero;

            if (_target != null && !_target.IsDead)
            {
                Vector2 to = (Vector2)_target.transform.position - (Vector2)transform.position;
                float dist = to.magnitude;
                Vector2 dir = dist > 0.0001f ? to / dist : Vector2.zero;
                float speed = Stats.Speed * UnitsPerSpeedPoint;

                switch (Spec.behavior)
                {
                    case EnemyBehavior.Chase:
                        move = dir * speed;
                        break;

                    case EnemyBehavior.KeepDistance:
                    {
                        float error = dist - Spec.preferredRange;
                        if (error > 0.5f) move = dir * speed;
                        else if (error < -0.5f) move = -dir * speed;
                        break;
                    }

                    case EnemyBehavior.Explode:
                    {
                        move = dir * speed;
                        float gap = dist - _target.Radius - _col.radius;
                        if (gap <= Spec.explosionTriggerRange)
                        {
                            Explode();
                            return;
                        }
                        break;
                    }
                }
            }

            _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, 20f * Time.fixedDeltaTime);
            _rb.linearVelocity = move + _knockback;
        }

        void Explode()
        {
            if (_finished) return;
            _finished = true;

            Vector2 center = transform.position;

            if (_target != null && !_target.IsDead)
            {
                float gap = Vector2.Distance(center, _target.transform.position) - _target.Radius;
                // PLACEHOLDER: flat damage that ignores DEF (the GDD leaves explosions undefined).
                if (gap <= Spec.explosionRadius) _target.Stats.TakeDamage(Spec.explosionDamage);
            }

            ExplosionFx.Spawn(center, Spec.explosionRadius, Spec.color);
            Destroy(gameObject);
        }
    }
}
