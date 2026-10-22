using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Data;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Enemies
{
    /// <summary>
    /// A regular enemy or boss: builds the swarm components, then runs one of three behaviours
    /// (chase, keep distance, explode). Cells, DEF and contact damage come from SwarmStats / SwarmCombat.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SwarmStats))]
    public class Enemy : MonoBehaviour, IKnockbackable
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

        public EnemySpec Spec { get; private set; }
        public SwarmStats Stats { get; private set; }
        public bool IsBoss => Spec.isBoss;
        public int ScoreValue => Spec.scorePerCell ? Spec.scoreValue * Spec.maxCells : Spec.scoreValue;

        Rigidbody2D _rb;
        CircleCollider2D _col;
        SwarmBody _target;
        Vector2 _knockback;
        bool _finished;

        public static Enemy Create(EnemySpec spec, Vector2 position)
        {
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
                        // Phase 4: fire the gun here once in range.
                        float error = dist - Spec.preferredRange;
                        if (error > 0.5f) move = dir * speed;
                        else if (error < -0.5f) move = -dir * speed;
                        break;

                    case EnemyBehavior.Explode:
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
