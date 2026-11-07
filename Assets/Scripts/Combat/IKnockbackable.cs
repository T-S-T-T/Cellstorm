using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>Anything that can be shoved. AddKnockback is a decaying velocity; Shove moves a set distance in a set time.</summary>
    public interface IKnockbackable
    {
        void AddKnockback(Vector2 velocity);

        /// <summary>Instant-feeling shove: moves 'displacement' world units over 'duration' seconds (about 0.2). DEF does not reduce it.</summary>
        void Shove(Vector2 displacement, float duration);
    }

    /// <summary>Small helper the hosts use to play a shove.</summary>
    public struct ShoveState
    {
        Vector2 _velocity;
        float _timer;

        public bool Active => _timer > 0f;

        public void Set(Vector2 displacement, float duration)
        {
            duration = Mathf.Max(0.05f, duration);
            _velocity = displacement / duration;
            _timer = duration;
        }

        /// <summary>Velocity to add this physics step.</summary>
        public Vector2 Step(float dt)
        {
            if (_timer <= 0f) return Vector2.zero;
            _timer -= dt;
            return _velocity;
        }

        public void Clear()
        {
            _timer = 0f;
        }
    }
}
