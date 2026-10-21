using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>Anything that can be shoved by a bounce. Velocity is in units/s and decays over time.</summary>
    public interface IKnockbackable
    {
        void AddKnockback(Vector2 velocity);
    }
}
