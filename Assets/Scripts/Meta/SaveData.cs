using System;
using System.Collections.Generic;

namespace Cellstorm.Meta
{
    /// <summary>One finished run (leaderboard row).</summary>
    [Serializable]
    public class RunRecord
    {
        public int score;
        public float runTime;
        public string loadout = "Default";
        public bool won;
        public bool endless;
        public int milestone;
        /// <summary>V0.1 rows only (cell count). New rows leave it 0 and fill maxHp.</summary>
        public int maxCells;
        public float maxHp;
        public float regen;
        public float defense;
        public float speed;
        public string weapons = "";
        public int enemiesDefeated;
        public int bossesDefeated;
        public string date = "";
        /// <summary>V0.2: bit N is set when Boss N was defeated in this run (achievements).</summary>
        public int bossMask;
        /// <summary>V0.2: evolutions taken, for example "Tank, Toxic".</summary>
        public string evolutions = "";

        public bool DefeatedBoss(int n)
        {
            return n >= 0 && n < 31 && (bossMask & (1 << n)) != 0;
        }
    }

    [Serializable]
    public class Statistics
    {
        public int runs;
        public int wins;
        public int enemiesDefeated;
        public int bossesDefeated;
        public float timePlayed;
        public float longestRun;
    }

    /// <summary>Everything saved to disk (GDD: leaderboard of the top 20 runs, unlocks, high score, statistics).</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 3;
        public int highScore;
        /// <summary>Lifetime score across all runs. Unlocks use this (not spent).</summary>
        public int totalScore;
        public List<string> unlocked = new List<string>();
        public List<RunRecord> leaderboard = new List<RunRecord>();
        public Statistics stats = new Statistics();
    }

    /// <summary>Shown on the run summary screen.</summary>
    public class RunSummary
    {
        public RunRecord record;
        public readonly List<string> unlockedNames = new List<string>();
        public bool saved;
    }
}
