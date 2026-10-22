using Cellstorm.Core;
using Cellstorm.Meta;
using UnityEngine;

namespace Cellstorm.UI
{
    /// <summary>Pause menu (ESC or the Menu button): resume, settings, leaderboard, quit to the main menu.</summary>
    public class PauseMenu : MonoBehaviour
    {
        enum View { Main, Settings, Leaderboard }

        View _view = View.Main;
        readonly SettingsPanel _settings = new SettingsPanel();

        void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Paused)
            {
                if (_view != View.Main) _settings.CancelRebind();
                _view = View.Main;
                return;
            }
            UiStyles.Ensure();
            UiDraw.Dim(0.65f);

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

            GUI.Label(new Rect(0f, sh * 0.2f, sw, 70f), "PAUSED", UiStyles.Title);

            float w = 300f, h = 52f, gap = 14f;
            float x = (sw - w) * 0.5f;
            float y = sh * 0.2f + 100f;

            if (UiDraw.Button(new Rect(x, y, w, h), "Resume")) gm.SetState(GameState.Playing);
            y += h + gap;
            if (UiDraw.Button(new Rect(x, y, w, h), "Settings")) _view = View.Settings;
            y += h + gap;
            if (UiDraw.Button(new Rect(x, y, w, h), "Leaderboard")) _view = View.Leaderboard;
            y += h + gap;
            if (UiDraw.Button(new Rect(x, y, w, h), "Quit to main menu"))
            {
                if (RunRecorder.Instance != null) RunRecorder.Instance.FinalizeRun();
                SceneLoader.LoadMainMenu();
            }
        }
    }
}
