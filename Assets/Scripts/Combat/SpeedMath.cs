using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>GDD V0.1.1 section 4.4: what the Speed stat does.</summary>
    public static class SpeedMath
    {
        /// <summary>World units per second for each point of Speed (V0.1 used 2.5).</summary>
        public const float UnitsPerSpeedPoint = 1f;
        public const float MinTime = 0.1f;

        /// <summary>Moving speed in units per second.</summary>
        public static float MoveSpeed(float speed)
        {
            return Mathf.Max(0f, speed) * UnitsPerSpeedPoint;
        }

        /// <summary>
        /// Multiplier for weapon deploy time and cooldown: 0.98 ^ (Speed - 2). Speed 2 = 1.0, Speed 5 = about 0.94.
        /// </summary>
        public static float TimeFactor(float speed)
        {
            return Mathf.Pow(0.98f, speed - 2f);
        }

        /// <summary>Base time scaled by Speed, never below MinTime (0.1 s) unless the base itself is shorter.</summary>
        public static float Scaled(float baseSeconds, float speed)
        {
            if (baseSeconds <= 0f) return 0f;
            return Mathf.Max(Mathf.Min(baseSeconds, MinTime), baseSeconds * TimeFactor(speed));
        }
    }
}
