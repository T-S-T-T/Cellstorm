using System;
using Cellstorm.Combat;
using Cellstorm.Core;
using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>
    /// The physical body of the player swarm: movement with inertia, a FIXED size (GDD V0.1.1 section 4.1),
    /// knockback, and a turn-rate limited aim direction. HP, DEF and speed come from SwarmStats.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SwarmStats))]
    public class SwarmBody : MonoBehaviour, IAbilityHost, ITargetable
    {
        [Header("Movement (units per second = Speed x 1, see SpeedMath)")]
        [Tooltip("How quickly the swarm speeds up (units/s^2).")]
        [SerializeField] float acceleration = 16f;
        [Tooltip("How quickly the swarm slows down when no key is held. Lower = more sliding.")]
        [SerializeField] float deceleration = 7f;

        [Header("Knockback")]
        [SerializeField] float knockbackDecay = 20f;
        [SerializeField] float maxKnockbackSpeed = 12f;

        [Header("Aiming")]
        [SerializeField] float turnRateDegPerSec = 360f;

        Rigidbody2D _rb;
        CircleCollider2D _col;
        SwarmStats _stats;
        float _sizeMultiplier = 1f;
        Vector2 _moveVelocity;
        Vector2 _knockback;
        float _aimAngle = 90f;
        Vector2 _dashVelocity;
        float _dashTimer;

        ShoveState _shove;
        StatusController _status;

        public SwarmStats Stats => _stats;
        public bool IsDead => _stats.IsDead;
        public Vector2 Position => transform.position;
        /// <summary>Loadout size multiplier (Giant = 2).</summary>
        public float SizeMultiplier => _sizeMultiplier;
        /// <summary>Visual radius. Fixed: never reads HP. 0.335 x the size multiplier.</summary>
        public float Radius => SwarmSize.ForPlayer(_sizeMultiplier);
        /// <summary>Same as Radius (kept for the visual and camera code that used the eased radius).</summary>
        public float SmoothedRadius => Radius;
        /// <summary>Collider radius: the visual radius x 1.1.</summary>
        public float ColliderRadius => SwarmSize.Collider(Radius);
        public float MoveSpeed => SpeedMath.MoveSpeed(_stats.Speed);

        /// <summary>Weapons set this below 1 while shaping/executing (reduced turning rate).</summary>
        public float TurnRateMultiplier { get; set; } = 1f;

        /// <summary>Balance bot: movement input (replaces the keys) and aim point (replaces the mouse).</summary>
        public Vector2? BotMove { get; set; }
        public Vector2? BotAimPoint { get; set; }
        public float AimAngle => _aimAngle;
        public Vector2 AimDirection =>
            new Vector2(Mathf.Cos(_aimAngle * Mathf.Deg2Rad), Mathf.Sin(_aimAngle * Mathf.Deg2Rad));

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CircleCollider2D>();
            _stats = GetComponent<SwarmStats>();
            if (_stats == null) _stats = gameObject.AddComponent<SwarmStats>();
            _status = _stats.Status;

            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            _col.radius = ColliderRadius;
        }

        /// <summary>Loadout choice (Giant = 2). The size is set once and never changes with HP.</summary>
        public void SetSizeMultiplier(float multiplier)
        {
            _sizeMultiplier = Mathf.Max(0.1f, multiplier);
            if (_col != null) _col.radius = ColliderRadius;
        }

        /// <summary>Test helper: sets max HP and current HP together.</summary>
        public void SetMaxHp(float value)
        {
            _stats.DebugSetMaxAndHp(value);
        }

        void OnEnable()
        {
            TargetingService.Register(this);
        }

        void OnDisable()
        {
            TargetingService.Unregister(this);
        }

        /// <summary>Knockback or pull from a status effect: a short, springy shove (about 0.2 s). DEF does not reduce it.</summary>
        public void Shove(Vector2 displacement, float duration)
        {
            _shove.Set(displacement, duration);
        }

        public void AddKnockback(Vector2 velocity)
        {
            _knockback = Vector2.ClampMagnitude(_knockback + velocity, maxKnockbackSpeed);
        }

        public bool IsDashing => _dashTimer > 0f;

        /// <summary>Ram: straight dash that replaces normal movement until the window ends.</summary>
        public void BeginDash(Vector2 direction, float speed, float duration)
        {
            _dashVelocity = direction.normalized * speed;
            _dashTimer = duration;
        }

        public void EndDash()
        {
            if (_dashTimer <= 0f) return;
            _dashTimer = 0f;
            _moveVelocity = Vector2.ClampMagnitude(_dashVelocity, MoveSpeed);   // keep a little momentum
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (!CanAct()) return;

            var cam = Camera.main;
            if (cam == null) return;

            Vector3 world;
            if (BotAimPoint.HasValue)
            {
                world = BotAimPoint.Value;
            }
            else
            {
                Vector2 screen = InputService.Aim.ReadValue<Vector2>();
                world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            }
            Vector2 to = (Vector2)world - (Vector2)transform.position;
            if (to.sqrMagnitude < 0.0001f) return;

            float target = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            float maxStep = turnRateDegPerSec * TurnRateMultiplier * dt;
            _aimAngle = Mathf.MoveTowardsAngle(_aimAngle, target, maxStep);
        }

        void FixedUpdate()
        {
            if (_dashTimer > 0f)
            {
                _dashTimer -= Time.fixedDeltaTime;
                _rb.linearVelocity = _dashVelocity + _shove.Step(Time.fixedDeltaTime);
                if (_dashTimer <= 0f) EndDashInternal();
                return;
            }

            Vector2 input = Vector2.zero;
            if (CanAct() && !_status.Stunned) input = BotMove.HasValue ? BotMove.Value : InputService.Move.ReadValue<Vector2>();
            Vector2 targetVelocity = input * (MoveSpeed * _status.SpeedFactor);       // Slow lowers moving speed only
            float rate = input.sqrMagnitude > 0.01f ? acceleration : deceleration;

            _moveVelocity = Vector2.MoveTowards(_moveVelocity, targetVelocity, rate * Time.fixedDeltaTime);
            _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
            _rb.linearVelocity = _moveVelocity + _knockback + _shove.Step(Time.fixedDeltaTime) + _status.External;
        }

        void EndDashInternal()
        {
            _dashTimer = 0f;
            _moveVelocity = Vector2.ClampMagnitude(_dashVelocity, MoveSpeed);
        }

        bool CanAct()
        {
            if (_stats != null && _stats.IsDead) return false;
            return GameManager.Instance == null || GameManager.Instance.IsPlaying;
        }
    }
}
