using System;
using Cellstorm.Combat;
using Cellstorm.Core;
using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>
    /// The physical body of the player swarm: movement with inertia, size from cell count,
    /// knockback, and a turn-rate limited aim direction. Cells, DEF and speed now come from SwarmStats.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SwarmStats))]
    public class SwarmBody : MonoBehaviour, IAbilityHost
    {
        [Header("Size")]
        [Tooltip("Radius = this * sqrt(cells). 5 cells ~ 0.34, 50 ~ 1.06, 500 ~ 3.4")]
        [SerializeField] float radiusPerSqrtCell = 0.15f;
        [SerializeField] float radiusSmoothing = 6f;

        [Header("Movement")]
        [Tooltip("World units per second for each point of the Speed stat (base 2).")]
        [SerializeField] float unitsPerSpeedPoint = 2.5f;
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
        Vector2 _moveVelocity;
        Vector2 _knockback;
        float _aimAngle = 90f;

        /// <summary>Raised when the cell count changes (new count).</summary>
        public event Action<int> CellCountChanged;

        public SwarmStats Stats => _stats;
        public bool IsDead => _stats.IsDead;
        public int CellCount => _stats.Cells;
        /// <summary>Target radius for the current cell count.</summary>
        public float Radius => radiusPerSqrtCell * Mathf.Sqrt(Mathf.Max(1, _stats.Cells));
        /// <summary>Radius eased toward Radius; use for visuals and camera.</summary>
        public float SmoothedRadius { get; private set; }
        public float MoveSpeed => _stats.Speed * unitsPerSpeedPoint;

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

            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            SmoothedRadius = Radius;
            _col.radius = Radius;
            _stats.CellsChanged += OnCellsChanged;
        }

        void OnDestroy()
        {
            if (_stats != null) _stats.CellsChanged -= OnCellsChanged;
        }

        void OnCellsChanged(int cells)
        {
            _col.radius = Radius;
            CellCountChanged?.Invoke(cells);
        }

        /// <summary>Test helper (Phase 1 debug keys): sets max cells and current cells.</summary>
        public void SetCellCount(int count)
        {
            _stats.DebugSetMaxAndCells(count);
        }

        public void AddKnockback(Vector2 velocity)
        {
            _knockback = Vector2.ClampMagnitude(_knockback + velocity, maxKnockbackSpeed);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            SmoothedRadius = Mathf.Lerp(SmoothedRadius, Radius, 1f - Mathf.Exp(-radiusSmoothing * dt));

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
            Vector2 input = Vector2.zero;
            if (CanAct()) input = BotMove.HasValue ? BotMove.Value : InputService.Move.ReadValue<Vector2>();
            Vector2 targetVelocity = input * MoveSpeed;
            float rate = input.sqrMagnitude > 0.01f ? acceleration : deceleration;

            _moveVelocity = Vector2.MoveTowards(_moveVelocity, targetVelocity, rate * Time.fixedDeltaTime);
            _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
            _rb.linearVelocity = _moveVelocity + _knockback;
        }

        bool CanAct()
        {
            if (_stats != null && _stats.IsDead) return false;
            return GameManager.Instance == null || GameManager.Instance.IsPlaying;
        }
    }
}
