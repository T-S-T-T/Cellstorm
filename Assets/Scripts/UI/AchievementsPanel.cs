using Cellstorm.Meta;
using UnityEngine;

namespace Cellstorm.UI
{
    /// <summary>
    /// The achievements screen (GDD V0.2 section 10.3): every unlock with its goal and whether it is done.
    /// Achievements first, then the score unlocks. Locked items show their requirement.
    /// </summary>
    public static class AchievementsPanel
    {
        static Vector2 _scroll;

        /// <summary>Draws the list inside 'area'. Returns true when Back is pressed.</summary>
        public static bool Draw(Rect area)
        {
            var data = SaveService.Data;

            GUI.Label(new Rect(area.x, area.y, area.width, 44f), "ACHIEVEMENTS AND UNLOCKS", UiStyles.Heading);
            GUI.Label(new Rect(area.x + 10f, area.y + 50f, area.width - 20f, 28f),
                "Lifetime score " + data.totalScore + "   |   Achievements are checked when a run ends.", UiStyles.Small);

            var list = new Rect(area.x + 10f, area.y + 86f, area.width - 20f, area.height - 150f);
            int count = UnlockCatalog.Entries.Length;
            const float rowH = 34f;
            _scroll = GUI.BeginScrollView(list, _scroll, new Rect(0f, 0f, list.width - 20f, count * rowH + 80f));

            float y = 0f;
            y = Section(list.width, y, "Achievements", UnlockKind.Achievement, data);
            y += 10f;
            Section(list.width, y, "Score unlocks", UnlockKind.Score, data);
            GUI.EndScrollView();

            return GUI.Button(new Rect(area.xMax - 190f, area.yMax - 50f, 170f, 40f), "Back");
        }

        static float Section(float width, float y, string title, UnlockKind kind, SaveData data)
        {
            GUI.Label(new Rect(0f, y, width, 28f), title, UiStyles.Label);
            y += 32f;
            foreach (var e in UnlockCatalog.Entries)
            {
                if (e.kind != kind) continue;
                bool done = data.unlocked.Contains(e.id);

                var old = GUI.color;
                GUI.color = done ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 1f, 1f, 0.8f);
                GUI.Label(new Rect(10f, y, 260f, 28f), (done ? "[DONE]  " : "[    ]  ") + e.name, UiStyles.Small);
                GUI.Label(new Rect(280f, y, width - 300f, 28f), UnlockCatalog.RequirementFor(e.id), UiStyles.Small);
                GUI.color = old;
                y += 30f;
            }
            return y;
        }
    }
}
