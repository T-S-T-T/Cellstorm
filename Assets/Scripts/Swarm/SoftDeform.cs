using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>Anything that reacts visually when its swarm is hit (soft ball player and enemies).</summary>
    public interface IHitVisual
    {
        void Hit(Vector2 worldPoint, float strength);
    }

    /// <summary>
    /// Squash and stretch for a soft ball (GDD V0.1.1 section 4.4, option A). Three springs are blended into one
    /// ellipse (stretch along an axis, squeeze across it, area roughly preserved):
    ///  1. a stretch along the direction of travel that grows with speed,
    ///  2. a squash along the direction of the last hit that rings back to rest,
    ///  3. a tiny idle wobble so a standing swarm still feels alive.
    /// Deformations are stored as "doubled angle" vectors, so blending two axes is smooth and never flips.
    /// </summary>
    public class SoftDeform
    {
        const float K = 140f;
        const float Damping = 0.32f;
        const float MaxStretch = 0.55f;

        float _s1, _v1, _a1;
        float _s2, _v2, _a2;
        float _seed = -1f;   // set on the first Tick: Random must not run in a field initializer

        /// <summary>Current rotation (degrees) and stretch (0..0.55) of the ellipse.</summary>
        public float AngleDeg { get; private set; }
        public float Stretch { get; private set; }

        public void Hit(Vector2 direction, float amount)
        {
            if (direction.sqrMagnitude < 1e-6f) direction = Vector2.right;
            _a2 = Mathf.Atan2(direction.y, direction.x);
            _s2 = -Mathf.Clamp(amount, 0f, 0.4f);   // negative: squashed along the hit direction
            _v2 = 0f;
        }

        public void Tick(float dt, Vector2 velocity, float time)
        {
            if (_seed < 0f) _seed = Random.value * 50f;
            dt = Mathf.Min(dt, 0.033f);
            if (dt <= 0f) return;

            float speed = velocity.magnitude;
            if (speed > 0.2f)
            {
                float target = Mathf.Atan2(velocity.y, velocity.x);
                _a1 = Mathf.LerpAngle(_a1 * Mathf.Rad2Deg, target * Mathf.Rad2Deg, 1f - Mathf.Exp(-12f * dt)) * Mathf.Deg2Rad;
            }
            float t1 = Mathf.Clamp(speed * 0.07f, 0f, 0.35f);
            Spring(ref _s1, ref _v1, t1, dt);
            Spring(ref _s2, ref _v2, 0f, dt);

            float wob = 0.035f * Mathf.Sin(time * 2.3f + _seed);
            float aw = time * 0.35f + _seed;

            float x = _s1 * Mathf.Cos(2f * _a1) + _s2 * Mathf.Cos(2f * _a2) + wob * Mathf.Cos(2f * aw);
            float y = _s1 * Mathf.Sin(2f * _a1) + _s2 * Mathf.Sin(2f * _a2) + wob * Mathf.Sin(2f * aw);

            Stretch = Mathf.Min(MaxStretch, Mathf.Sqrt(x * x + y * y));
            AngleDeg = 0.5f * Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        }

        static void Spring(ref float s, ref float v, float target, float dt)
        {
            float c = 2f * Mathf.Sqrt(K) * Damping;
            v += ((target - s) * K - v * c) * dt;
            s += v * dt;
        }

        /// <summary>Apply to a sprite transform whose sprite is a unit circle of the given diameter.</summary>
        public void Apply(Transform t, float diameter)
        {
            float k = 1f + Stretch;
            t.localRotation = Quaternion.Euler(0f, 0f, AngleDeg);
            t.localScale = new Vector3(diameter * k, diameter / k, 1f);
        }
    }
}
