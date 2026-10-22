using Cellstorm.Core;
using Cellstorm.Meta;
using Cellstorm.Progression;
using UnityEngine;

namespace Cellstorm.UI
{
    /// <summary>
    /// Shown when the swarm is defeated, or after beating the last boss (continue in endless mode, start a new run,
    /// or go to the menu). Defeat records the run immediately; a win is recorded when you leave the run.
    /// </summary>
    public class RunSummaryScreen : MonoBehaviour
    {
        RunSummary _preview;

        void OnGUI()
        {
            var gm = GameManager.Instance;
            var rec = RunRecorder.Instance;
            if (gm == null || rec == null) return;

            bool won = gm.State == GameState.Won;
            bool dead = gm.State == GameState.Dead;
            if (!won && !dead)
            {
                _preview = null;
                return;
            }
            UiStyles.Ensure();
            UiDraw.Dim(0.7f);

            RunSummary summary;
            if (dead)
            {
                summary = rec.Summary ?? rec.FinalizeRun();
            }
            else
            {
                if (_preview == null) _preview = rec.Preview();
                summary = _preview;
            }
            var r = summary.record;

            float sw = Screen.width, sh = Screen.height;
            float panelW = 640f;
            float x = (sw - panelW) * 0.5f;
            float y = sh * 0.12f;

            GUI.Label(new Rect(0f, y, sw, 64f), won ? "VICTORY!" : "YOUR SWARM WAS DEFEATED", UiStyles.Title);
            y += 80f;

            Line(x, ref y, panelW, "Score", r.score.ToString());
            if (summary.saved && r.score > 0 && r.score == SaveService.Data.highScore)
                GUI.Label(new Rect(x, y - 6f, panelW, 24f), "New high score!", UiStyles.DefText);
            Line(x, ref y, panelW, "Run time", UiDraw.Time(r.runTime));
            Line(x, ref y, panelW, "Milestone reached", r.milestone + (r.endless ? " (endless)" : ""));
            Line(x, ref y, panelW, "Loadout", r.loadout);
            Line(x, ref y, panelW, "Enemies defeated", r.enemiesDefeated + "   (bosses " + r.bossesDefeated + ")");
            Line(x, ref y, panelW, "Final swarm", r.maxCells + " max cells   DEF " + r.defense.ToString("0.#") +
                                                  "   speed " + r.speed.ToString("0.#") + "   regen " + r.regen.ToString("0.#") + "/s");
            Line(x, ref y, panelW, "Weapons and skills", string.IsNullOrEmpty(r.weapons) ? "-" : r.weapons);

            if (summary.unlockedNames.Count > 0)
            {
                y += 6f;
                GUI.Label(new Rect(x, y, panelW, 30f), "UNLOCKED: " + string.Join(", ", summary.unlockedNames), UiStyles.DefText);
                y += 36f;
            }
            if (won)
            {
                GUI.Label(new Rect(x, y, panelW, 26f), "Your score is saved when you leave this run.", UiStyles.Small);
                y += 30f;
            }

            y += 16f;
            float bw = 190f, bh = 52f, gap = 14f;
            int count = won ? 3 : 2;
            float bx = (sw - (count * bw + (count - 1) * gap)) * 0.5f;

            if (won)
            {
                if (UiDraw.Button(new Rect(bx, y, bw, bh), "Endless mode"))
                {
                    if (RunDirector.Instance != null) RunDirector.Instance.StartEndless();
                }
                bx += bw + gap;
            }
            if (UiDraw.Button(new Rect(bx, y, bw, bh), "New run")) SceneLoader.LoadGame();
            bx += bw + gap;
            if (UiDraw.Button(new Rect(bx, y, bw, bh), "Main menu")) SceneLoader.LoadMainMenu();
        }

        static void Line(float x, ref float y, float w, string label, string value)
        {
            GUI.Label(new Rect(x, y, 230f, 28f), label, UiStyles.Small);
            GUI.Label(new Rect(x + 240f, y, w - 240f, 28f), value, UiStyles.Label);
            y += 30f;
        }
    }
}
