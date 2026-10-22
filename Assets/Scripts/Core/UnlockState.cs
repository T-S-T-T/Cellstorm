using Cellstorm.Meta;

namespace Cellstorm.Core
{
    /// <summary>
    /// Which weapons, skills and loadouts are unlocked. Backed by the save file; see UnlockCatalog for the
    /// score thresholds. Ids: "jaw", "gun", "hardening", "loadout_speedy", "loadout_tank".
    /// </summary>
    public static class UnlockState
    {
        /// <summary>Testing only (F11 in the Game scene): treat everything as unlocked.</summary>
        public static bool UnlockEverything { get; set; }

        public static bool IsUnlocked(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            if (UnlockEverything) return true;
            return SaveService.Data.unlocked.Contains(id);
        }

        public static void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var list = SaveService.Data.unlocked;
            if (!list.Contains(id))
            {
                list.Add(id);
                SaveService.Save();
            }
        }
    }
}
