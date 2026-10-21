using Cellstorm.Combat;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// TEMPORARY. Straight-line chase toward the player, with knockback. Replaced by the real enemy
    /// base in Phase 3.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(SwarmStats))]
    public class DebugEnemyBrain : MonoBehaviour, IKnockbackable
    {
        [SerializeField] float unitsPerSpeedPoint = 2.5f;
        [SerializeField] float knockbackDecay = 20f;
        [SerializeField] float maxKnockbackSpeed = 12f;

        public bool Chase { get; set; } = true;

        Rigidbody2D _rb;
        SwarmStats _stats;
        Transform _target;
        Vector2 _knockback;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stats = GetComponent<SwarmStats>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void AddKnockback(Vector2 velocity)
        {
            _knockback = Vector2.ClampMagnitude(_knockback + velocity, maxKnockbackSpeed);
        }

        void FixedUpdate()
        {
            if (_target == null)
            {
                var player = FindAnyObjectByType<SwarmBody>();
                if (player != null) _target = player.transform;
            }

            Vector2 velocity = Vector2.zero;
            if (Chase && _target != null)
            {
                Vector2 to = (Vector2)_target.position - (Vector2)transform.position;
                if (to.sqrMagnitude > 0.0001f)
                    velocity = to.normalized * (_stats.Speed * unitsPerSpeedPoint);
            }

            _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
            _rb.linearVelocity = velocity + _knockback;
        }
    }
}
