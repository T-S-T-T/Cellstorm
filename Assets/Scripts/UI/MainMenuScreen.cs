using Cellstorm.Core;
using Cellstorm.Meta;
using UnityEngine;

namespace Cellstorm.UI
{
    /// <summary>Main menu: play, leaderboard, settings, quit, plus high score and the next unlock.</summary>
    public class MainMenuScreen : MonoBehaviour
    {
        enum View { Main, Settings, Leaderboard }

        View _view = View.Main;
        readonly SettingsPanel _settings = new SettingsPanel();

        void OnGUI()
        {
            UiStyles.Ensure();
            float sw = Screen.width, sh = Screen.height;

            if (_view == View.Settings)
            {
                var area = new Rect(sw * 0.5f - 340f, 40f, 680f, sh - 80f);
                UiDraw.Fill(area, new Color(0.08f, 0.09f, 0.12f, 0.95f));
                if (_settings.Draw(area)) _view = View.Main;
                return;
            }
            if (_view == View.Leaderboard)
            {
                var area = new Rect(sw * 0.5f - 480f, 40f, 960f, sh - 80f);
                UiDraw.Fill(area, new Color(0.08f, 0.09f, 0.12f, 0.95f));
                if (LeaderboardPanel.Draw(area)) _view = View.Main;
                return;
            }

            var data = SaveService.Data;

            GUI.Label(new Rect(0f, sh * 0.12f, sw, 80f), "CELLSTORM", UiStyles.Title);
            GUI.Label(new Rect(0f, sh * 0.12f + 70f, sw, 30f), "Evolve. Swarm. Survive.", UiStyles.LabelCenter);

            float w = 300f, h = 54f, gap = 14f;
            float x = (sw - w) * 0.5f;
            float y = sh * 0.36f;

            if (UiDraw.Button(new Rect(x, y, w, h), "Play")) SceneLoader.LoadGame();
            y += h + gap;
            if (UiDraw.Button(new Rect(x, y, w, h), "Leaderboard")) _view = View.Leaderboard;
            y += h + gap;
            if (UiDraw.Button(new Rect(x, y, w, h), "Settings")) _view = View.Settings;
            y += h + gap;
            if (UiDraw.Button(new Rect(x, y, w, h), "Quit"))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            y += h + 30f;

            GUI.Label(new Rect(0f, y, sw, 28f),
                "High score " + data.highScore + "      Lifetime score " + data.totalScore + "      Runs " + data.stats.runs,
                UiStyles.LabelCenter);
            y += 30f;

            var next = UnlockCatalog.NextLocked(data.unlocked);
            string nextText = string.IsNullOrEmpty(next.id)
                ? "Everything is unlocked!"
                : "Next unlock: " + next.name + " at " + next.threshold + " lifetime score";
            GUI.Label(new Rect(0f, y, sw, 26f), nextText, UiStyles.SmallCenter);
        }
    }
}
