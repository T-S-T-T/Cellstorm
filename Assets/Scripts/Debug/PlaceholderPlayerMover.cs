using Cellstorm.Core;
using UnityEngine;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// TEMPORARY. Moves a circle with WASD so Phase 0 can be verified.
    /// Replaced by the real swarm body in Phase 1.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlaceholderPlayerMover : MonoBehaviour
    {
        [SerializeField] float speed = 6f;

        Rigidbody2D _rb;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        void FixedUpdate()
        {
            bool canMove = GameManager.Instance == null || GameManager.Instance.IsPlaying;
            Vector2 input = canMove ? InputService.Move.ReadValue<Vector2>() : Vector2.zero;
            _rb.linearVelocity = input * speed;
        }
    }
}
