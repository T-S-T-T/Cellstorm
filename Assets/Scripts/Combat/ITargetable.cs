using System.Collections.Generic;
using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>The player and its minions: the things enemies can chase and hurt.</summary>
    public interface ITargetable
    {
        Vector2 Position { get; }
        float Radius { get; }
        bool IsDead { get; }
        SwarmStats Stats { get; }
    }

    /// <summary>
    /// GDD V0.2 section 7.4: every enemy chases whichever is nearest, the player or one of the player's minions,
    /// measured between the edges of the bodies. Callers re-check about 4 times a second (staggered), so a plain loop
    /// over the targets is cheap enough. Minions use Enemies the same way through Enemy.Active.
    /// </summary>
    public static class TargetingService
    {
        static readonly List<ITargetable> Targets = new List<ITargetable>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Targets.Clear();
        }

        public static int Count => Targets.Count;

        /// <summary>All registered targets (the player and its minions). Read-only; do not modify while looping.</summary>
        public static IReadOnlyList<ITargetable> All => Targets;

        public static void Register(ITargetable t)
        {
            if (t != null && !Targets.Contains(t)) Targets.Add(t);
        }

        public static void Unregister(ITargetable t)
        {
            Targets.Remove(t);
        }

        /// <summary>True for a living target whose object still exists.</summary>
        public static bool IsValid(ITargetable t)
        {
            if (t == null) return false;
            if (t is UnityEngine.Object o && o == null) return false;
            return !t.IsDead;
        }

        /// <summary>Nearest living target by edge distance, or null.</summary>
        public static ITargetable Nearest(Vector2 from, float fromRadius)
        {
            ITargetable best = null;
            float bestGap = float.MaxValue;
            for (int i = 0; i < Targets.Count; i++)
            {
                var t = Targets[i];
                if (!IsValid(t)) continue;
                float gap = (t.Position - from).magnitude - t.Radius - fromRadius;
                if (gap < bestGap)
                {
                    bestGap = gap;
                    best = t;
                }
            }
            return best;
        }
    }
}
