using Cellstorm.Combat;
using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Debugging;
using Cellstorm.Enemies;
using Cellstorm.Meta;
using Cellstorm.Progression;
using Cellstorm.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.UI
{
    /// <summary>
    /// The in-run HUD from the GDD: menu button (top left), heart with cell count and a blue DEF shield
    /// (bottom middle), DEF number to its right, 2 weapon slots to its left and 2 skill slots to its right with
    /// pie overlays for cooldown and execution, an EXP arrow (top right) and the boss bar (top middle).
    /// F12 toggles the debug text overlays, F11 unlocks everything for testing.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        static readonly Color ExecutionPie = new Color(1.00f, 0.75f, 0.20f, 0.65f);
        static readonly Color CooldownPie = new Color(0.10f, 0.15f, 0.30f, 0.78f);

        readonly string[] _keys = new string[AbilityController.SlotCount];
        float _nextKeyRefresh;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f12Key.wasPressedThisFrame) DebugVisibility.Show = !DebugVisibility.Show;
                if (kb.f11Key.wasPressedThisFrame) UnlockState.UnlockEverything = !UnlockState.UnlockEverything;
            }

            if (Time.unscaledTime >= _nextKeyRefresh)
            {
                _nextKeyRefresh = Time.unscaledTime + 0.5f;
                _keys[0] = KeyLabel(InputService.Attack1);
                _keys[1] = KeyLabel(InputService.Attack2);
                _keys[2] = KeyLabel(InputService.Skill1);
                _keys[3] = KeyLabel(InputService.Skill2);
            }
        }

        static string KeyLabel(InputAction a)
        {
            return a != null ? a.GetBindingDisplayString() : "";
        }

        void OnGUI()
        {
            var gm = GameManager.Instance;
            var build = PlayerBuild.Instance;
            if (gm == null || build == null) return;
            if (gm.State == GameState.LoadoutSelect) return;

            GUI.depth = 5; // behind the level-up, pause and summary screens
            UiStyles.Ensure();

            bool swap = gm.State == GameState.SwapMode;
            var stats = build.Stats;
            var abilities = build.Abilities;

            if (swap) UiDraw.Dim(0.35f);

            DrawHeart(stats);
            DrawDef(stats);
            DrawSlots(abilities);
            DrawExp();
            bool boss = DrawBossBar();
            DrawTimer(boss ? 62f : 14f);
            DrawTopLeft(gm);

            if (swap) DrawSwapOverlay(abilities);
        }

        // ---------------- Heart, DEF ----------------

        void DrawHeart(SwarmStats stats)
        {
            var r = HudLayout.Heart();

            // Blue shield layer around the heart (brighter while DEF is boosted).
            bool boosted = stats.Defense > stats.BaseDefense + 0.01f;
            var halo = new Rect(r.center.x - r.width * 0.64f, r.center.y - r.height * 0.62f, r.width * 1.28f, r.height * 1.28f);
            GUI.color = new Color(0.25f, 0.55f, 1f, boosted ? 0.95f : 0.55f);
            GUI.DrawTexture(halo, HudTextures.Heart);

            // Empty heart, then the filled part from the bottom (cells / max cells).
            GUI.color = new Color(0.22f, 0.05f, 0.08f, 1f);
            GUI.DrawTexture(r, HudTextures.Heart);

            float f = Mathf.Clamp01(stats.Cells / (float)Mathf.Max(1, stats.MaxCells));
            var fill = new Rect(r.x, r.yMax - r.height * f, r.width, r.height * f);
            GUI.color = new Color(0.90f, 0.15f, 0.22f, 1f);
            GUI.DrawTextureWithTexCoords(fill, HudTextures.Heart, new Rect(0f, 0f, 1f, f));
            GUI.color = Color.white;

            GUI.Label(r, stats.Cells.ToString(), UiStyles.HeartText);
            GUI.Label(new Rect(r.x, r.y - 22f, r.width, 20f), "max " + stats.MaxCells, UiStyles.SmallCenter);
        }

        void DrawDef(SwarmStats stats)
        {
            float d = stats.Defense;
            string text = Mathf.Approximately(d, Mathf.Round(d)) ? ((int)d).ToString() : d.ToString("0.#");
            GUI.Label(HudLayout.DefLabel(), "DEF " + text, UiStyles.DefText);
        }

        // ---------------- Slots ----------------

        void DrawSlots(AbilityController abilities)
        {
            for (int i = 0; i < AbilityController.SlotCount; i++)
            {
                var r = HudLayout.Slot(i);
                UiDraw.Fill(r, new Color(0.08f, 0.09f, 0.12f, 0.9f));

                var a = abilities != null ? abilities.GetSlot(i) : null;
                if (a != null) DrawAbilityTile(r, a);

                UiDraw.Border(r, 2f, new Color(0.45f, 0.50f, 0.60f, 1f));
                GUI.Label(new Rect(r.x + 4f, r.y, r.width - 8f, 16f), _keys[i], UiStyles.Small);
            }
        }

        static string GlyphFor(WeaponType t)
        {
            switch (t)
            {
                case WeaponType.Ram: return "RAM";
                case WeaponType.Jaw: return "JAW";
                case WeaponType.Gun: return "GUN";
                default: return "HRD";
            }
        }

        static void DrawAbilityTile(Rect r, Ability a)
        {
            Color baseColor = a.Data.kind == AbilityKind.Weapon
                ? new Color(0.75f, 0.30f, 0.30f)
                : new Color(0.55f, 0.35f, 0.80f);
            var inner = new Rect(r.x + 3f, r.y + 3f, r.width - 6f, r.height - 6f);
            UiDraw.Fill(inner, new Color(baseColor.r * 0.65f, baseColor.g * 0.65f, baseColor.b * 0.65f, 1f));

            float fill = 0f;
            Color pie = ExecutionPie;
            switch (a.Phase)
            {
                case AbilityPhase.Shaping:
                    fill = a.Progress;
                    break;
                case AbilityPhase.Primed:
                    fill = 1f;
                    pie.a = 0.35f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f));
                    break;
                case AbilityPhase.Executing:
                    fill = 1f - a.Progress;
                    break;
                case AbilityPhase.Cooldown:
                    fill = 1f - a.Progress;
                    pie = CooldownPie;
                    break;
            }

            if (fill > 0.001f)
            {
                var old = GUI.color;
                GUI.color = pie;
                GUI.DrawTexture(inner, HudTextures.Pie(fill));
                GUI.color = old;
            }

            GUI.Label(new Rect(inner.x, inner.y + 6f, inner.width, inner.height * 0.5f), GlyphFor(a.Data.type), UiStyles.Glyph);
            if (a.Phase == AbilityPhase.Cooldown)
                GUI.Label(new Rect(inner.x, inner.y + inner.height * 0.5f, inner.width, inner.height * 0.5f),
                    a.CooldownRemaining.ToString("0.0"), UiStyles.Glyph);
        }

        // ---------------- EXP, boss, timer, menu ----------------

        void DrawExp()
        {
            var pp = PlayerProgress.Instance;
            if (pp == null) return;

            var r = HudLayout.ExpArrow();
            GUI.color = new Color(0.10f, 0.14f, 0.12f, 0.9f);
            GUI.DrawTexture(r, HudTextures.Arrow);

            float f = Mathf.Clamp01(pp.Exp / (float)Mathf.Max(1, pp.ExpToNext));
            if (f > 0.001f)
            {
                var fill = new Rect(r.x, r.yMax - r.height * f, r.width, r.height * f);
                GUI.color = new Color(0.40f, 1f, 0.80f, 1f);
                GUI.DrawTextureWithTexCoords(fill, HudTextures.Arrow, new Rect(0f, 0f, 1f, f));
            }
            GUI.color = Color.white;
        }

        bool DrawBossBar()
        {
            Enemy boss = null;
            for (int i = 0; i < Enemy.Active.Count; i++)
            {
                if (Enemy.Active[i] != null && Enemy.Active[i].IsBoss)
                {
                    boss = Enemy.Active[i];
                    break;
                }
            }
            if (boss == null) return false;

            float w = 440f, h = 20f;
            float x = (Screen.width - w) * 0.5f;
            float y = 12f;
            float f = Mathf.Clamp01((float)boss.Stats.Cells / Mathf.Max(1, boss.Stats.MaxCells));

            UiDraw.Fill(new Rect(x - 3f, y - 3f, w + 6f, h + 6f), new Color(0f, 0f, 0f, 0.65f));
            UiDraw.Fill(new Rect(x, y, w * f, h), boss.Spec.color);

            string label = boss.name + (boss.Spec.weapon != null ? "  [" + boss.Spec.weapon.displayName + "]" : "");
            GUI.Label(new Rect(x, y + h + 2f, w, 22f), label + "    DEF " + boss.Stats.Defense.ToString("0"), UiStyles.SmallCenter);
            return true;
        }

        void DrawTimer(float y)
        {
            if (!GameSettings.ShowRunTimer || RunDirector.Instance == null) return;
            GUI.Label(new Rect(Screen.width * 0.5f - 60f, y, 120f, 26f), UiDraw.Time(RunDirector.Instance.Elapsed), UiStyles.LabelCenter);
        }

        void DrawTopLeft(GameManager gm)
        {
            var btn = HudLayout.MenuButton();
            if (gm.State == GameState.Playing)
            {
                if (GUI.Button(btn, "Menu", UiStyles.Button)) gm.SetState(GameState.Paused);
            }
            else
            {
                GUI.enabled = false;
                GUI.Button(btn, "Menu", UiStyles.Button);
                GUI.enabled = true;
            }

            string note = "F12: debug text";
            if (UnlockState.UnlockEverything) note += "   [ALL UNLOCKED - F11]";
            GUI.Label(new Rect(14f, 52f, 400f, 20f), note, UiStyles.Small);
        }

        // ---------------- Swap mode ----------------

        void DrawSwapOverlay(AbilityController abilities)
        {
            if (abilities == null) return;
            int sel = SwapModeController.Selected;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
            var selected = sel >= 0 ? abilities.GetSlot(sel) : null;

            for (int i = 0; i < AbilityController.SlotCount; i++)
            {
                var r = HudLayout.Slot(i);
                Color c = new Color(1f, 0.90f, 0.30f, 0.45f + 0.55f * pulse);

                if (selected != null)
                {
                    bool fits = (selected.Data.kind == AbilityKind.Weapon) == (i < 2);
                    c = fits ? new Color(0.35f, 1f, 0.45f, 0.5f + 0.5f * pulse) : new Color(0.5f, 0.5f, 0.5f, 0.7f);
                }
                UiDraw.Border(new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f), 3f, c);
            }

            if (selected != null)
            {
                // The picked-up item pops up above its slot (and shakes when a wrong key is pressed).
                var from = HudLayout.Slot(sel);
                float shake = Time.unscaledTime < SwapModeController.ShakeUntil ? Mathf.Sin(Time.unscaledTime * 70f) * 7f : 0f;
                float size = HudLayout.SlotSize * 1.35f;
                var pop = new Rect(from.center.x - size * 0.5f + shake, from.y - size - 36f, size, size);
                UiDraw.Fill(new Rect(pop.x - 4f, pop.y - 4f, pop.width + 8f, pop.height + 8f), new Color(0.08f, 0.09f, 0.12f, 0.95f));
                DrawAbilityTile(pop, selected);
                UiDraw.Border(pop, 3f, Color.white);
            }

            string text = selected == null
                ? "SWAP MODE - click a weapon or skill to pick it up.   F or Esc to leave."
                : "Press " + _keys[0] + " / " + _keys[1] + " to place a weapon, or " + _keys[2] + " / " + _keys[3] +
                  " to place a skill. An occupied slot swaps with it.";
            GUI.Label(new Rect(0f, HudLayout.Heart().y - 150f, Screen.width, 30f), text, UiStyles.LabelCenter);
        }
    }
}
