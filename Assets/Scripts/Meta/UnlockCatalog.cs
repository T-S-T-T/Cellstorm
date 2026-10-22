using System.Collections.Generic;

namespace Cellstorm.Meta
{
    /// <summary>
    /// What score unlocks. Thresholds compare against lifetime score and are not spent.
    /// Jaw 100, Hardening 150 and Gun 200 are from the GDD. The two loadout thresholds are PLACEHOLDERS.
    /// </summary>
    public static class UnlockCatalog
    {
        public struct Entry
        {
            public string id;
            public string name;
            public int threshold;
        }

        public static readonly Entry[] Entries =
        {
            new Entry { id = "jaw", name = "Jaw", threshold = 100 },
            new Entry { id = "hardening", name = "Hardening", threshold = 150 },
            new Entry { id = "gun", name = "Gun", threshold = 200 },
            new Entry { id = "loadout_speedy", name = "Speedy loadout", threshold = 3000 },  // placeholder, see README balance table
            new Entry { id = "loadout_tank", name = "Tank loadout", threshold = 10000 }  // placeholder, see README balance table
        };

        public static int ThresholdFor(string id)
        {
            foreach (var e in Entries)
                if (e.id == id) return e.threshold;
            return 0;
        }

        public static string NameFor(string id)
        {
            foreach (var e in Entries)
                if (e.id == id) return e.name;
            return id;
        }

        /// <summary>Adds everything newly reached to 'unlocked' and returns the new ids.</summary>
        public static List<string> Evaluate(int totalScore, List<string> unlocked)
        {
            var added = new List<string>();
            foreach (var e in Entries)
            {
                if (totalScore >= e.threshold && !unlocked.Contains(e.id))
                {
                    unlocked.Add(e.id);
                    added.Add(e.id);
                }
            }
            return added;
        }

        /// <summary>The cheapest locked entry, or null id when everything is unlocked.</summary>
        public static Entry NextLocked(List<string> unlocked)
        {
            Entry best = default;
            bool found = false;
            foreach (var e in Entries)
            {
                if (unlocked.Contains(e.id)) continue;
                if (!found || e.threshold < best.threshold)
                {
                    best = e;
                    found = true;
                }
            }
            return best;
        }
    }
}
