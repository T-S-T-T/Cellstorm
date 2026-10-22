using System.Collections.Generic;

namespace Cellstorm.Core
{
    /// <summary>
    /// Which weapons, skills and loadouts the player has unlocked with score.
    /// PLACEHOLDER: everything is unlocked until Phase 6 adds score-based unlocks and saving.
    /// Ids used so far: "jaw", "gun", "hardening", "loadout_speedy", "loadout_tank".
    /// </summary>
    public static class UnlockState
    {
        static readonly HashSet<string> Unlocked = new HashSet<string>();

        /// <summary>Phase 6 sets this to false and fills the set from the save file.</summary>
        public static bool UnlockEverything { get; set; } = true;

        public static bool IsUnlocked(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            return UnlockEverything || Unlocked.Contains(id);
        }

        public static void Unlock(string id)
        {
            if (!string.IsNullOrEmpty(id)) Unlocked.Add(id);
        }
    }
}
