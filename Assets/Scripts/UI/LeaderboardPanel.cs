using Cellstorm.Meta;
using UnityEngine;

namespace Cellstorm.UI
{
    /// <summary>Top 20 runs by score, plus lifetime statistics.</summary>
    public static class LeaderboardPanel
    {
        static Vector2 _scroll;

        /// <summary>Draws the leaderboard inside 'area'. Returns true when Back is pressed.</summary>
        public static bool Draw(Rect area)
        {
            var data = SaveService.Data;
            var st = data.stats;

            GUI.Label(new Rect(area.x, area.y, area.width, 44f), "LEADERBOARD", UiStyles.Heading);

            string stats = "High score " + data.highScore + "     Lifetime score " + data.totalScore +
                           "     Runs " + st.runs + "     Wins " + st.wins +
                           "     Enemies defeated " + st.enemiesDefeated + "     Time played " + UiDraw.Time(st.timePlayed);
            GUI.Label(new Rect(area.x + 10f, area.y + 50f, area.width - 20f, 28f), stats, UiStyles.Small);

            float y = area.y + 86f;
            DrawRow(area.x + 10f, y, area.width - 20f, "#", "Score", "Time", "Result", "Loadout", "Cells", "DEF", "Spd", "Weapons", true);
            y += 30f;

            var listRect = new Rect(area.x + 10f, y, area.width - 20f, area.yMax - y - 60f);
            int n = data.leaderboard.Count;
            _scroll = GUI.BeginScrollView(listRect, _scroll, new Rect(0f, 0f, listRect.width - 20f, Mathf.Max(30f, n * 28f)));
            for (int i = 0; i < n; i++)
            {
                var r = data.leaderboard[i];
                string result = r.won ? (r.endless ? "Won + endless" : "Won") : "Defeated";
                DrawRow(0f, i * 28f, listRect.width - 20f, (i + 1).ToString(), r.score.ToString(), UiDraw.Time(r.runTime),
                    result, r.loadout, r.maxCells.ToString(), r.defense.ToString("0.#"), r.speed.ToString("0.#"), r.weapons, false);
            }
            if (n == 0) GUI.Label(new Rect(0f, 0f, listRect.width, 28f), "No runs yet. Go play!", UiStyles.Label);
            GUI.EndScrollView();

            return GUI.Button(new Rect(area.xMax - 190f, area.yMax - 50f, 170f, 40f), "Back");
        }

        static void DrawRow(float x, float y, float w, string rank, string score, string time, string result,
            string loadout, string cells, string def, string spd, string weapons, bool header)
        {
            var style = header ? UiStyles.Small : UiStyles.Label;
            float[] cx = { 0f, 40f, 130f, 210f, 330f, 430f, 510f, 580f, 650f };
            string[] text = { rank, score, time, result, loadout, cells, def, spd, weapons };
            float[] cw = { 36f, 86f, 76f, 116f, 96f, 76f, 66f, 66f, Mathf.Max(100f, w - 650f) };
            for (int i = 0; i < text.Length; i++)
                GUI.Label(new Rect(x + cx[i], y, cw[i], 28f), text[i], style);
        }
    }
}
