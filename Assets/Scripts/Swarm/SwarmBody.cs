using System;
using Cellstorm.Core;
using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>
    /// The physical body of a swarm: movement with inertia ("drag"), cell-count based size,
    /// and a turn-rate limited aim direction. Cell count lives here temporarily; in Phase 2 it moves
    /// to the stat component and this class reads it from there.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class SwarmBody : MonoBehaviour
    {
        [Header("Cells (temporary - moves to the stat component in Phase 2)")]
        [SerializeField, Min(1)] int cellCount = 5;

        [Header("Size")]
        [Tooltip("Radius = this * sqrt(cells). 5 cells ~ 0.34, 50 ~ 1.06, 500 ~ 3.4")]
        [SerializeField] float radiusPerSqrtCell = 0.15f;
        [SerializeField] float radiusSmoothing = 6f;

        [Header("Movement")]
        [Tooltip("GDD speed stat (base 2).")]
        [SerializeField] float speedStat = 2f;
        [SerializeField] float unitsPerSpeedPoint = 2.5f;
        [Tooltip("How quickly the swarm speeds up (units/s^2).")]
        [SerializeField] float acceleration = 16f;
        [Tooltip("How quickly the swarm slows down when no key is held. Lower = more sliding.")]
        [SerializeField] float deceleration = 7f;

        [Header("Aiming")]
        [SerializeField] float turnRateDegPerSec = 360f;

        Rigidbody2D _rb;
        CircleCollider2D _col;
        float _aimAngle = 90f;

        /// <summary>Raised when the cell count changes (new count).</summary>
        public event Action<int> CellCountChanged;

        public int CellCount => cellCount;
        /// <summary>Target radius for the current cell count.</summary>
        public float Radius => radiusPerSqrtCell * Mathf.Sqrt(cellCount);
        /// <summary>Radius eased toward Radius; use for visuals and camera.</summary>
        public float SmoothedRadius { get; private set; }
        public float MoveSpeed => speedStat * unitsPerSpeedPoint;

        /// <summary>Weapons set this below 1 while shaping/executing (reduced turning rate).</summary>
        public float TurnRateMultiplier { get; set; } = 1f;
        public float AimAngle => _aimAngle;
        public Vector2 AimDirection =>
            new Vector2(Mathf.Cos(_aimAngle * Mathf.Deg2Rad), Mathf.Sin(_aimAngle * Mathf.Deg2Rad));

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CircleCollider2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            cellCount = Mathf.Max(1, cellCount);
            SmoothedRadius = Radius;
            _col.radius = Radius;
        }

        void OnValidate()
        {
            cellCount = Mathf.Max(1, cellCount);
            if (Application.isPlaying && _col != null) _col.radius = Radius;
        }

        public void SetCellCount(int count)
        {
            count = Mathf.Max(1, count);
            if (count == cellCount) return;
            cellCount = count;
            if (_col != null) _col.radius = Radius;
            CellCountChanged?.Invoke(cellCount);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            SmoothedRadius = Mathf.Lerp(SmoothedRadius, Radius, 1f - Mathf.Exp(-radiusSmoothing * dt));

            if (!CanAct()) return;

            var cam = Camera.main;
            if (cam == null) return;

            Vector2 screen = InputService.Aim.ReadValue<Vector2>();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            Vector2 to = (Vector2)world - (Vector2)transform.position;
            if (to.sqrMagnitude < 0.0001f) return;

            float target = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            float maxStep = turnRateDegPerSec * TurnRateMultiplier * dt;
            _aimAngle = Mathf.MoveTowardsAngle(_aimAngle, target, maxStep);
        }

        void FixedUpdate()
        {
            Vector2 input = CanAct() ? InputService.Move.ReadValue<Vector2>() : Vector2.zero;
            Vector2 targetVelocity = input * MoveSpeed;
            float rate = input.sqrMagnitude > 0.01f ? acceleration : deceleration;
            _rb.linearVelocity = Vector2.MoveTowards(
                _rb.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);
        }

        static bool CanAct()
        {
            return GameManager.Instance == null || GameManager.Instance.IsPlaying;
        }
    }
}
