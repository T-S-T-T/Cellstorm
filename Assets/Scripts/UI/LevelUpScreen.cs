using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Progression;
using Cellstorm.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.UI
{
    /// <summary>
    /// Level-up selection: option cards in a grid (3 in a row in V0.1.1, 3 x 2 in V0.2), a tooltip for the hovered
    /// one, a reroll button (the only place the reroll count is shown) and no skip.
    ///
    /// Weapons and skills always ask for their slot: after clicking the card the player presses the slot key
    /// (LMB / RMB for weapons, Q / E for skills). A wrong key shakes the prompt and does nothing, Back returns to the
    /// options. With the debug "show all" switch the grid lists every eligible upgrade and scrolls.
    /// </summary>
    public class LevelUpScreen : MonoBehaviour
    {
        UpgradeSO _placing;
        int _placingFrame;
        float _shakeUntil;
        Vector2 _scroll;
        GUIStyle _title, _glyph, _name, _small, _tooltip;

        const float CardW = 200f, CardH = 214f, Gap = 20f;

        static Rect BackRect(float sw, float sh)
        {
            return new Rect((sw - 160f) * 0.5f, sh * 0.35f + 100f, 160f, 36f);
        }

        void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _glyph = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _name = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _tooltip = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.UpperLeft, wordWrap = true };
        }

        static bool IsPlaced(UpgradeSO u)
        {
            return u != null && u.effect == UpgradeEffect.GrantWeapon;
        }

        // ---------------- Slot key prompt (input is read in Update) ----------------

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.LevelUp)
            {
                _placing = null;
                return;
            }
            if (_placing == null || Time.frameCount <= _placingFrame + 1) return;

            if (InputService.Pause.WasPressedThisFrame())
            {
                _placing = null;
                return;
            }

            // Clicking the Back button must not count as pressing LMB.
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 p = mouse.position.ReadValue();
                var gui = new Vector2(p.x, Screen.height - p.y);
                if (BackRect(Screen.width, Screen.height).Contains(gui)) return;
            }

            int slot = -1;
            if (InputService.Attack1.WasPressedThisFrame()) slot = 0;
            else if (InputService.Attack2.WasPressedThisFrame()) slot = 1;
            else if (InputService.Skill1.WasPressedThisFrame()) slot = 2;
            else if (InputService.Skill2.WasPressedThisFrame()) slot = 3;

            if (slot >= 0)
            {
                bool ok = System.Array.IndexOf(PlayerBuild.SlotsFor(_placing), slot) >= 0;
                var pp = PlayerProgress.Instance;
                if (ok && pp != null && pp.Choose(_placing, slot))
                {
                    AudioHooks.Play(AudioCue.Assign);
                    _placing = null;
                    return;
                }
                Wrong();
            }
            else if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                Wrong();
            }
        }

        void Wrong()
        {
            AudioHooks.Play(AudioCue.Error);
            _shakeUntil = Time.unscaledTime + 0.4f;
        }

        // ---------------- Drawing ----------------

        void OnGUI()
        {
            var gm = GameManager.Instance;
            var pp = PlayerProgress.Instance;
            var build = PlayerBuild.Instance;
            if (gm == null || pp == null || build == null || gm.State != GameState.LevelUp)
            {
                _placing = null;
                return;
            }
            EnsureStyles();

            GUI.depth = 0;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float sw = Screen.width, sh = Screen.height;
            GUI.Label(new Rect(0, sh * 0.05f, sw, 50), "LEVEL UP!", _title);
            if (pp.PendingLevelUps > 1)
                GUI.Label(new Rect(0, sh * 0.05f + 44, sw, 24), pp.PendingLevelUps + " level-ups waiting", _small);

            if (_placing != null) DrawPlacing(sw, sh);
            else DrawOptions(pp, build, sw, sh);
        }

        void DrawOptions(PlayerProgress pp, PlayerBuild build, float sw, float sh)
        {
            var options = pp.Options;
            int n = options.Count;

            int cols = n <= 3 ? Mathf.Max(1, n) : (n == 4 ? 2 : 3);
            if (PlayerProgress.DebugShowAll) cols = Mathf.Max(1, Mathf.FloorToInt((sw - 80f) / (CardW + Gap)));
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)cols));

            float contentW = cols * CardW + (cols - 1) * Gap;
            float contentH = rows * CardH + (rows - 1) * Gap;
            float top = sh * 0.14f;
            float footer = 210f;                       // tooltip + reroll
            float viewH = Mathf.Min(contentH, sh - top - footer);
            bool scrolls = contentH > viewH + 0.5f;
            float viewW = contentW + (scrolls ? 20f : 0f);
            var view = new Rect((sw - viewW) * 0.5f, top, viewW, viewH);

            Vector2 mouse = Event.current.mousePosition;
            UpgradeSO hovered = null;
            UpgradeSO clicked = null;

            if (scrolls) _scroll = GUI.BeginScrollView(view, _scroll, new Rect(0f, 0f, contentW, contentH));
            else GUI.BeginGroup(view);

            for (int i = 0; i < n; i++)
            {
                var u = options[i];
                var card = new Rect((i % cols) * (CardW + Gap), (i / cols) * (CardH + Gap), CardW, CardH);
                bool hover = card.Contains(mouse);
                if (hover) hovered = u;

                Color baseColor = u.iconColor.a > 0.01f ? u.iconColor : UpgradeDefaults.CategoryColor(u.category);
                GUI.color = hover ? Color.Lerp(baseColor, Color.white, 0.25f) : baseColor;
                GUI.DrawTexture(card, Texture2D.whiteTexture);
                GUI.color = new Color(0f, 0f, 0f, 0.3f);
                GUI.DrawTexture(new Rect(card.x, card.y + 110f, CardW, CardH - 110f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                var iconRect = new Rect(card.x, card.y, CardW, 110f);
                if (u.icon != null) GUI.DrawTexture(iconRect, u.icon.texture, ScaleMode.ScaleToFit);
                else GUI.Label(iconRect, u.iconGlyph, _glyph);

                GUI.Label(new Rect(card.x + 8, card.y + 114, CardW - 16, 56), u.displayName, _name);
                string owned = build.Stacks(u) > 0 ? "  (x" + build.Stacks(u) + ")" : "";
                string tag = u.category == UpgradeCategory.Evolution ? "EVOLUTION" : u.category.ToString();
                GUI.Label(new Rect(card.x + 8, card.y + 180, CardW - 16, 28), tag + owned, _small);

                if (GUI.Button(card, GUIContent.none, GUIStyle.none)) clicked = u;
            }

            if (scrolls) GUI.EndScrollView();
            else GUI.EndGroup();

            // Tooltip for the hovered option.
            float ty = view.yMax + 14f;
            var tip = new Rect((sw - 700f) * 0.5f, ty, 700f, 100f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(tip, Texture2D.whiteTexture);
            GUI.color = Color.white;
            string tipText = hovered != null
                ? hovered.displayName + "\n" + hovered.description + "\n" + hovered.StackLine(build.Stacks(hovered))
                : "Hover an upgrade to see what it does.";
            GUI.Label(new Rect(tip.x + 10, tip.y + 4, tip.width - 20, tip.height - 8), tipText, _tooltip);

            // Reroll (count is only shown here).
            GUI.enabled = pp.Rerolls > 0 && !PlayerProgress.DebugShowAll;
            if (GUI.Button(new Rect((sw - 200f) * 0.5f, ty + 108f, 200f, 34f), "Reroll (" + pp.Rerolls + ")"))
                pp.Reroll();
            GUI.enabled = true;

            if (Debug.isDebugBuild)
            {
                bool all = GUI.Toggle(new Rect(16f, sh - 40f, 360f, 28f), PlayerProgress.DebugShowAll, " Debug: show every eligible upgrade");
                if (all != PlayerProgress.DebugShowAll)
                {
                    PlayerProgress.DebugShowAll = all;
                    _scroll = Vector2.zero;
                    pp.RefreshOptions();
                }
            }

            if (clicked != null)
            {
                if (IsPlaced(clicked))
                {
                    _placing = clicked;
                    _placingFrame = Time.frameCount;
                }
                else
                {
                    pp.Choose(clicked, -1);
                }
            }
        }

        void DrawPlacing(float sw, float sh)
        {
            var u = _placing;
            bool skill = u.weapon != null && u.weapon.kind == AbilityKind.Skill;
            string keyA = skill ? KeyName(InputService.Skill1) : KeyName(InputService.Attack1);
            string keyB = skill ? KeyName(InputService.Skill2) : KeyName(InputService.Attack2);

            float shake = Time.unscaledTime < _shakeUntil ? Mathf.Sin(Time.unscaledTime * 70f) * 8f : 0f;
            bool wrong = Time.unscaledTime < _shakeUntil;

            GUI.Label(new Rect(shake, sh * 0.22f, sw, 44), u.displayName, _title);
            var old = GUI.color;
            GUI.color = wrong ? new Color(1f, 0.45f, 0.4f) : Color.white;
            GUI.Label(new Rect(shake, sh * 0.22f + 56f, sw, 40),
                "Press " + keyA + " or " + keyB + " to choose the " + (skill ? "skill" : "weapon") + " slot", _name);
            GUI.color = old;

            // Show what is in the two slots.
            var abilities = PlayerBuild.Instance != null ? PlayerBuild.Instance.Abilities : null;
            int[] slots = PlayerBuild.SlotsFor(u);
            float w = 260f, h = 56f, gap = 24f;
            float total = slots.Length * w + (slots.Length - 1) * gap;
            float x0 = (sw - total) * 0.5f + shake;
            float y = sh * 0.35f;
            for (int i = 0; i < slots.Length; i++)
            {
                var a = abilities != null ? abilities.GetSlot(slots[i]) : null;
                string label = AbilityController.SlotNames[slots[i]] + ": " + (a != null ? a.Data.displayName + "  (will be replaced)" : "empty");
                GUI.Box(new Rect(x0 + i * (w + gap), y, w, h), label);
            }

            if (GUI.Button(BackRect(sw, sh), "Back")) _placing = null;
        }

        static string KeyName(UnityEngine.InputSystem.InputAction a)
        {
            return a != null ? a.GetBindingDisplayString() : "?";
        }
    }
}
