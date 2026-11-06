using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>
    /// GDD V0.1.1 section 4.1: every swarm has a fixed size that never reads HP.
    /// Player: 0.335 x the loadout's size multiplier (Giant = 2). Enemy: 0.15 x sqrt(maxHp / 20), clamped, set once at spawn.
    /// The collider is the visual radius x 1.1.
    /// </summary>
    public static class SwarmSize
    {
        public const float PlayerRadius = 0.335f;
        public const float ColliderFactor = 1.1f;
        public const float EnemyMin = 0.15f;
        public const float EnemyMax = 6f;

        public static float ForPlayer(float sizeMultiplier)
        {
            return PlayerRadius * Mathf.Max(0.1f, sizeMultiplier);
        }

        public static float ForEnemy(float maxHp)
        {
            return Mathf.Clamp(0.15f * Mathf.Sqrt(Mathf.Max(0f, maxHp) / 20f), EnemyMin, EnemyMax);
        }

        public static float Collider(float visualRadius)
        {
            return visualRadius * ColliderFactor;
        }
    }
}
