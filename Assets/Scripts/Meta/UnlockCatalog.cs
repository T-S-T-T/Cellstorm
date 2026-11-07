using System.Collections.Generic;

namespace Cellstorm.Meta
{
    public enum UnlockKind
    {
        /// <summary>Unlocks when the lifetime score reaches the threshold (never spent).</summary>
        Score,
        /// <summary>Unlocks when a recorded run meets the goal.</summary>
        Achievement
    }

    /// <summary>
    /// What unlocks and how (GDD V0.2 section 9.2). Score thresholds compare against lifetime score. Achievements are
    /// checked when a run is recorded, using that run's results, so they also count when the run ends in defeat.
    /// All thresholds and goals are placeholders to tune.
    /// </summary>
    public static class UnlockCatalog
    {
        public struct Entry
        {
            public string id;
            public string name;
            public UnlockKind kind;
            public int threshold;       // Score kind
            public string goal;         // Achievement kind
        }

        static Entry S(string id, string name, int threshold)
        {
            return new Entry { id = id, name = name, kind = UnlockKind.Score, threshold = threshold };
        }

        static Entry A(string id, string name, string goal)
        {
            return new Entry { id = id, name = name, kind = UnlockKind.Achievement, goal = goal };
        }

        public static readonly Entry[] Entries =
        {
            S("jaw", "Jaw", 100),
            S("hardening", "Hardening", 150),
            S("gun", "Gun", 200),
            S("spread_shot", "Spread Shot", 300),
            S("minion", "Minion", 400),
            S("spore_cloud", "Spore Cloud", 600),
            S("shockwave", "Shockwave", 800),
            A("whip", "Whip", "Defeat Boss 2 in any run"),
            A("mine", "Mine", "Defeat 200 enemies in a single run"),
            A("convert", "Convert", "Defeat Boss 3 in any run"),
            S("loadout_swarmer", "Swarmer loadout", 2000),
            S("loadout_speedy", "Speedy loadout", 3000),
            S("loadout_giant", "Giant loadout", 5000),
            A("loadout_sporer", "Sporer loadout", "Reach milestone 4 (20:00) in any run"),
            S("loadout_tank", "Tank loadout", 10000)
        };

        public static bool TryGet(string id, out Entry entry)
        {
            foreach (var e in Entries)
            {
                if (e.id == id)
                {
                    entry = e;
                    return true;
                }
            }
            entry = default;
            return false;
        }

        public static int ThresholdFor(string id)
        {
            return TryGet(id, out var e) ? e.threshold : 0;
        }

        public static string NameFor(string id)
        {
            return TryGet(id, out var e) ? e.name : id;
        }

        /// <summary>"300 lifetime score" or the achievement goal.</summary>
        public static string RequirementFor(string id)
        {
            if (!TryGet(id, out var e)) return "";
            return e.kind == UnlockKind.Score ? e.threshold + " lifetime score" : e.goal;
        }

        /// <summary>Does this run meet the goal of an achievement?</summary>
        public static bool AchievementMet(string id, RunRecord run)
        {
            if (run == null) return false;
            switch (id)
            {
                case "whip": return run.DefeatedBoss(2);
                case "convert": return run.DefeatedBoss(3);
                case "mine": return run.enemiesDefeated >= 200;
                case "loadout_sporer": return run.milestone >= 4;
                default: return false;
            }
        }

        /// <summary>Adds everything newly reached to 'unlocked' and returns the new ids (scores only).</summary>
        public static List<string> Evaluate(int totalScore, List<string> unlocked)
        {
            return Evaluate(totalScore, unlocked, null);
        }

        /// <summary>Score unlocks from the lifetime score and achievements from the run just recorded.</summary>
        public static List<string> Evaluate(int totalScore, List<string> unlocked, RunRecord run)
        {
            var added = new List<string>();
            foreach (var e in Entries)
            {
                if (unlocked.Contains(e.id)) continue;
                bool reached = e.kind == UnlockKind.Score ? totalScore >= e.threshold : AchievementMet(e.id, run);
                if (!reached) continue;
                unlocked.Add(e.id);
                added.Add(e.id);
            }
            return added;
        }

        /// <summary>The cheapest locked score unlock, or an empty id when every score unlock is done.</summary>
        public static Entry NextLocked(List<string> unlocked)
        {
            Entry best = default;
            bool found = false;
            foreach (var e in Entries)
            {
                if (e.kind != UnlockKind.Score || unlocked.Contains(e.id)) continue;
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
