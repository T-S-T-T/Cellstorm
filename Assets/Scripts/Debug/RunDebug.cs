using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Core;
using Cellstorm.Enemies;
using Cellstorm.Swarm;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// TEMPORARY test harness for Phase 3: run clock, milestone and spawn info, boss bar, win overlay.
    /// N next milestone (spawns the next boss)   J +60 seconds   G god mode   Y +5 base DEF
    /// L kill every enemy (awards score; killing Boss 6 wins)   E endless (after winning)
    /// </summary>
    public class RunDebug : MonoBehaviour
    {
        SwarmBody _player;
        float _fps = 60f;
        GUIStyle _big;
        GUIStyle _center;

        void Start()
        {
            _player = FindAnyObjectByType<SwarmBody>();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) _fps = Mathf.Lerp(_fps, 1f / dt, 0.05f);

            var kb = Keyboard.current;
            if (kb == null) return;

            var run = RunDirector.Instance;
            if (run != null)
            {
                if (kb.nKey.wasPressedThisFrame) run.DebugJumpToNextMilestone();
                if (kb.jKey.wasPressedThisFrame) run.DebugAddTime(60f);
                if (kb.eKey.wasPressedThisFrame) run.StartEndless();
            }

            if (_player != null)
            {
                if (kb.gKey.wasPressedThisFrame) _player.Stats.Invulnerable = !_player.Stats.Invulnerable;
                if (kb.yKey.wasPressedThisFrame) _player.Stats.AddToBase(StatType.Defense, 5f);
            }

            if (kb.lKey.wasPressedThisFrame)
            {
                var all = new List<Enemy>(Enemy.Active);
                foreach (var e in all)
                    if (e != null) e.Stats.TakeDamage(int.MaxValue);
            }
        }

        static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }

        void OnGUI()
        {
            var run = RunDirector.Instance;
            var spawner = EnemySpawner.Instance;
            if (run == null) return;

            string next = run.AtFinalMilestone
                ? "Boss " + run.FinalMilestone + " is out - defeat it to win"
                : "next boss in " + FormatTime(run.SecondsToNextMilestone);

            int bosses = Enemy.Active.Count - Enemy.CountNonBoss();
            string line1 = $"Time {FormatTime(run.Elapsed)}   Milestone {run.Milestone}/{run.FinalMilestone}   {next}   " +
                           $"Score {run.Score}   Enemies {Enemy.CountNonBoss()}/{(spawner != null ? spawner.MaxEnemies : 0)} (+{bosses} boss)" +
                           (run.Endless ? "   [ENDLESS]" : "") +
                           (_player != null && _player.Stats.Invulnerable ? "   [GOD MODE]" : "") +
                           $"   FPS {_fps:F0}";
            string line2 = "Spawn/s: " + (spawner != null ? spawner.DescribeRates() : "-");

            GUI.Label(new Rect(10, 170, 1300, 25), line1);
            GUI.Label(new Rect(10, 190, 1300, 25), line2);
            GUI.Label(new Rect(10, 210, 1300, 25),
                "N next milestone   J +60s   G god mode   Y +5 DEF   L kill all enemies   E endless (after win)");

            DrawBossBar();

            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Won)
            {
                if (_big == null) _big = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 60), "YOU WIN!", _big);
                GUI.Label(new Rect(0, Screen.height * 0.35f + 55, Screen.width, 40),
                    "E = endless mode     R = new run", _big);
            }
        }

        void DrawBossBar()
        {
            Enemy boss = null;
            for (int i = 0; i < Enemy.Active.Count; i++)
            {
                if (Enemy.Active[i] != null && Enemy.Active[i].IsBoss) { boss = Enemy.Active[i]; break; }
            }
            if (boss == null) return;

            if (_center == null)
                _center = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            float w = 420f, h = 18f;
            float x = (Screen.width - w) * 0.5f;
            float y = 12f;
            float frac = Mathf.Clamp01((float)boss.Stats.Cells / boss.Stats.MaxCells);

            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, w + 4f, h + 4f), Texture2D.whiteTexture);
            GUI.color = boss.Spec.color;
            GUI.DrawTexture(new Rect(x, y, w * frac, h), Texture2D.whiteTexture);
            GUI.color = old;

            GUI.Label(new Rect(x, y + h + 2f, w, 22f),
                $"{boss.name}   {boss.Stats.Cells}/{boss.Stats.MaxCells}   DEF {boss.Stats.Defense:F0}", _center);
        }
    }
}
