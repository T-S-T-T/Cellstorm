using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>
    /// GDD V0.1.1 section 4.1: every swarm has a fixed size that never reads HP.
    /// Player: 0.335 x Scale x the loadout's size multiplier (Giant = 2). Enemy: 0.15 x Scale x sqrt(maxHp / 20), clamped, set once at spawn.
    /// The collider is the visual radius x 1.1.
    /// </summary>
    public static class SwarmSize
    {
        /// <summary>
        /// One knob for how big every body is on screen (player, enemies, minions). The V0.1.1 radii are multiplied by
        /// this, so they stay in proportion to each other. 1 = the old tiny size.
        /// </summary>
        public const float Scale = 2.5f;

        public const float PlayerRadius = 0.335f * Scale;
        public const float ColliderFactor = 1.1f;
        public const float EnemyMin = 0.15f * Scale;
        public const float EnemyMax = 7f;

        public static float ForPlayer(float sizeMultiplier)
        {
            return PlayerRadius * Mathf.Max(0.1f, sizeMultiplier);
        }

        public static float ForEnemy(float maxHp)
        {
            return Mathf.Clamp(0.15f * Scale * Mathf.Sqrt(Mathf.Max(0f, maxHp) / 20f), EnemyMin, EnemyMax);
        }

        public static float Collider(float visualRadius)
        {
            return visualRadius * ColliderFactor;
        }
    }
}
