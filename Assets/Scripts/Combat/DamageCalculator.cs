using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>Pure functions for the GDD damage rules (section 4.3). No scene dependencies.</summary>
    public static class DamageCalculator
    {
        public const float MinDps = 5f;

        /// <summary>
        /// Cells per second that A removes from B while touching. Zero unless A's DEF is higher than B's.
        /// </summary>
        public static float ContactDps(int attackerCells, float attackerDef, int defenderCells, float defenderDef)
        {
            if (attackerDef <= defenderDef || attackerDef <= 0f) return 0f;

            float sizeRatio = attackerCells / Mathf.Max(1f, defenderCells);
            float sizeFactor = Mathf.Clamp01((sizeRatio - 0.5f) / 1.5f);
            float defFactor = Mathf.Clamp01((attackerDef - defenderDef) / attackerDef);
            float t = sizeFactor * 0.5f + defFactor * 0.5f;

            return Mathf.Floor(Mathf.Lerp(MinDps, defenderCells, t));
        }
    }
}
