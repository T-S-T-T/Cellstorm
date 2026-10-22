using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cellstorm.Meta
{
    /// <summary>JSON save file in Application.persistentDataPath.</summary>
    public static class SaveService
    {
        public const int MaxLeaderboard = 20;

        /// <summary>While true, runs are not recorded (the balance bot plays without touching your save).</summary>
        public static bool Suspended { get; set; }

        static SaveData _data;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _data = null;
        }

        public static string FilePath => Path.Combine(Application.persistentDataPath, "cellstorm_save.json");

        public static SaveData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    _data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveService: could not read the save file, starting fresh. " + e.Message);
                try { File.Copy(FilePath, FilePath + ".bak", true); } catch (Exception) { }
                _data = null;
            }

            if (_data == null) _data = new SaveData();
            if (_data.unlocked == null) _data.unlocked = new List<string>();
            if (_data.leaderboard == null) _data.leaderboard = new List<RunRecord>();
            if (_data.stats == null) _data.stats = new Statistics();
        }

        public static void Save()
        {
            if (_data == null) return;
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(_data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveService: could not write the save file. " + e.Message);
            }
        }

        public static void ResetAll()
        {
            _data = new SaveData();
            Save();
        }

        /// <summary>
        /// Adds a finished run: leaderboard (top 20), high score, statistics and lifetime score, then unlocks.
        /// Returns the ids unlocked by this run. Empty runs (restarting right away) are ignored.
        /// </summary>
        public static List<string> RecordRun(RunRecord run)
        {
            var newlyUnlocked = new List<string>();
            if (run == null || Suspended) return newlyUnlocked;
            if (run.score <= 0 && run.runTime < 10f) return newlyUnlocked;

            var d = Data;

            d.stats.runs++;
            if (run.won) d.stats.wins++;
            d.stats.enemiesDefeated += run.enemiesDefeated;
            d.stats.bossesDefeated += run.bossesDefeated;
            d.stats.timePlayed += run.runTime;
            d.stats.longestRun = Mathf.Max(d.stats.longestRun, run.runTime);

            d.highScore = Mathf.Max(d.highScore, run.score);
            d.totalScore += run.score;

            d.leaderboard.Add(run);
            d.leaderboard.Sort((a, b) => b.score.CompareTo(a.score));
            if (d.leaderboard.Count > MaxLeaderboard)
                d.leaderboard.RemoveRange(MaxLeaderboard, d.leaderboard.Count - MaxLeaderboard);

            newlyUnlocked = UnlockCatalog.Evaluate(d.totalScore, d.unlocked);
            Save();
            return newlyUnlocked;
        }
    }
}
