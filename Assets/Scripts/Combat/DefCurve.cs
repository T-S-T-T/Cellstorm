using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>
    /// GDD V0.1.1 section 4.3: DEF reduces incoming damage on a logarithmic curve.
    /// reduction = clamp(0.218 * ln(DEF) + 0.05, 0, 0.9), and 0 below DEF 1.
    /// DEF 1 = 5%, 2 = 20%, 5 = 40%, 10 = 55%, 25 = 75%, 50 = 90% (cap).
    /// </summary>
    public static class DefCurve
    {
        public const float MaxReduction = 0.9f;

        /// <summary>Fraction of damage removed (0..0.9).</summary>
        public static float Reduction(float defense)
        {
            if (defense < 1f) return 0f;
            return Mathf.Clamp(0.218f * Mathf.Log(defense) + 0.05f, 0f, MaxReduction);
        }

        /// <summary>Damage after DEF.</summary>
        public static float Apply(float rawDamage, float defense)
        {
            return rawDamage * (1f - Reduction(defense));
        }
    }
}
