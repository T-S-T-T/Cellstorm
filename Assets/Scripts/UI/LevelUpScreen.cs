using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Progression;
using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.UI
{
    /// <summary>
    /// Level-up selection: 3 icon cards, a tooltip for the hovered one, a reroll button (the only place the
    /// reroll count is shown) and no skip. Choosing a weapon or skill with no free slot asks which one to replace.
    /// TEMPORARY look (IMGUI): Phase 6 restyles it.
    /// </summary>
    public class LevelUpScreen : MonoBehaviour
    {
        UpgradeSO _replacing;
        GUIStyle _title, _glyph, _name, _small, _tooltip;

        void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _glyph = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _name = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _tooltip = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.UpperLeft, wordWrap = true };
        }

        void OnGUI()
        {
            var gm = GameManager.Instance;
            var pp = PlayerProgress.Instance;
            var build = PlayerBuild.Instance;
            if (gm == null || pp == null || build == null || gm.State != GameState.LevelUp)
            {
                _replacing = null;
                return;
            }
            EnsureStyles();

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float sw = Screen.width, sh = Screen.height;
            GUI.Label(new Rect(0, sh * 0.08f, sw, 50), "LEVEL UP!", _title);
            if (pp.PendingLevelUps > 1)
                GUI.Label(new Rect(0, sh * 0.08f + 44, sw, 24), pp.PendingLevelUps + " level-ups waiting", _small);

            if (_replacing != null) DrawReplace(pp, build, sw, sh);
            else DrawOptions(pp, build, sw, sh);
        }

        void DrawOptions(PlayerProgress pp, PlayerBuild build, float sw, float sh)
        {
            var options = pp.Options;
            int n = options.Count;
            float cardW = 200f, cardH = 230f, gap = 28f;
            float total = n * cardW + (n - 1) * gap;
            float x0 = (sw - total) * 0.5f;
            float y = sh * 0.22f;
            Vector2 mouse = Event.current.mousePosition;
            UpgradeSO hovered = null;
            UpgradeSO clicked = null;

            for (int i = 0; i < n; i++)
            {
                var u = options[i];
                var card = new Rect(x0 + i * (cardW + gap), y, cardW, cardH);
                bool hover = card.Contains(mouse);
                if (hover) hovered = u;

                Color baseColor = u.iconColor.a > 0.01f ? u.iconColor : UpgradeDefaults.CategoryColor(u.category);
                GUI.color = hover ? Color.Lerp(baseColor, Color.white, 0.25f) : baseColor;
                GUI.DrawTexture(card, Texture2D.whiteTexture);
                GUI.color = new Color(0f, 0f, 0f, 0.3f);
                GUI.DrawTexture(new Rect(card.x, card.y + 120f, cardW, cardH - 120f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                var iconRect = new Rect(card.x, card.y, cardW, 120f);
                if (u.icon != null) GUI.DrawTexture(iconRect, u.icon.texture, ScaleMode.ScaleToFit);
                else GUI.Label(iconRect, u.iconGlyph, _glyph);

                GUI.Label(new Rect(card.x + 8, card.y + 126, cardW - 16, 56), u.displayName, _name);
                string owned = build.Stacks(u) > 0 ? "  (owned x" + build.Stacks(u) + ")" : "";
                GUI.Label(new Rect(card.x + 8, card.y + 190, cardW - 16, 28), u.category + owned, _small);

                if (GUI.Button(card, GUIContent.none, GUIStyle.none)) clicked = u;
            }

            // Tooltip for the hovered option.
            float ty = y + cardH + 24f;
            var tip = new Rect((sw - 640f) * 0.5f, ty, 640f, 90f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(tip, Texture2D.whiteTexture);
            GUI.color = Color.white;
            string tipText = hovered != null
                ? hovered.displayName + "\n" + hovered.description
                : "Hover an upgrade to see what it does.";
            GUI.Label(new Rect(tip.x + 10, tip.y + 6, tip.width - 20, tip.height - 12), tipText, _tooltip);

            // Reroll (count is only shown here).
            GUI.enabled = pp.Rerolls > 0;
            if (GUI.Button(new Rect((sw - 200f) * 0.5f, ty + 104f, 200f, 38f), "Reroll (" + pp.Rerolls + ")"))
                pp.Reroll();
            GUI.enabled = true;

            if (clicked != null)
            {
                if (build.NeedsReplaceTarget(clicked)) _replacing = clicked;
                else pp.Choose(clicked, -1);
            }
        }

        void DrawReplace(PlayerProgress pp, PlayerBuild build, float sw, float sh)
        {
            var u = _replacing;
            bool skill = u.weapon != null && u.weapon.kind == AbilityKind.Skill;
            GUI.Label(new Rect(0, sh * 0.25f, sw, 40),
                "All " + (skill ? "skill" : "weapon") + " slots are full. Replace which one with " + u.displayName + "?", _small);

            int[] slots = PlayerBuild.SlotsFor(u);
            float w = 260f, h = 60f, gap = 24f;
            float total = slots.Length * w + (slots.Length - 1) * gap;
            float x0 = (sw - total) * 0.5f;
            float y = sh * 0.35f;

            for (int i = 0; i < slots.Length; i++)
            {
                var a = build.Abilities != null ? build.Abilities.GetSlot(slots[i]) : null;
                string label = AbilityController.SlotNames[slots[i]] + ": " + (a != null ? a.Data.displayName : "-");
                if (GUI.Button(new Rect(x0 + i * (w + gap), y, w, h), label))
                {
                    if (pp.Choose(u, slots[i])) _replacing = null;
                }
            }

            if (GUI.Button(new Rect((sw - 160f) * 0.5f, y + h + 24f, 160f, 36f), "Back")) _replacing = null;
        }
    }
}
