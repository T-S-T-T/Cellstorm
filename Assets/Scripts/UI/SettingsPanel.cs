using Cellstorm.Core;
using Cellstorm.Meta;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.UI
{
    /// <summary>
    /// Settings list used by the main menu and the pause menu: run timer toggle, key rebinding
    /// (saved automatically), reset controls and reset progress.
    /// </summary>
    public class SettingsPanel
    {
        static readonly string[] ActionNames = { "Move", "Attack1", "Attack2", "Skill1", "Skill2", "Swap", "Pause" };
        static readonly string[] ActionLabels = { "Move", "Weapon 1", "Weapon 2", "Skill 1", "Skill 2", "Swap mode", "Pause" };

        InputActionRebindingExtensions.RebindingOperation _op;
        string _rebindText = "";
        Vector2 _scroll;
        bool _confirmReset;
        GUIStyle _toggle;

        public void CancelRebind()
        {
            if (_op != null) _op.Cancel();
            _confirmReset = false;
        }

        /// <summary>Draws the panel inside 'area'. Returns true when Back is pressed.</summary>
        public bool Draw(Rect area)
        {
            if (_toggle == null) _toggle = new GUIStyle(GUI.skin.toggle) { fontSize = 18 };
            bool back = false;

            GUI.Label(new Rect(area.x, area.y, area.width, 44f), "SETTINGS", UiStyles.Heading);

            float y = area.y + 56f;
            bool busy = _op != null;
            GUI.enabled = !busy;

            bool timer = GameSettings.ShowRunTimer;
            bool newTimer = GUI.Toggle(new Rect(area.x + 20f, y, area.width - 40f, 30f), timer,
                "  Show run timer (hidden by default, as in the GDD)", _toggle);
            if (newTimer != timer) GameSettings.ShowRunTimer = newTimer;
            y += 42f;

            GUI.Label(new Rect(area.x + 20f, y, area.width - 40f, 28f),
                busy ? "Press a key or mouse button for \"" + _rebindText + "\"   (Backspace cancels)" : "Controls (click Rebind, then press the new key)",
                UiStyles.Label);
            y += 32f;

            var map = InputService.Asset != null ? InputService.Asset.FindActionMap("Gameplay") : null;
            float listHeight = area.yMax - y - 70f;
            var listRect = new Rect(area.x + 20f, y, area.width - 40f, Mathf.Max(100f, listHeight));

            if (map != null)
            {
                float rowH = 38f;
                int rows = 0;
                foreach (var name in ActionNames)
                {
                    var a = map.FindAction(name);
                    if (a == null) continue;
                    for (int i = 0; i < a.bindings.Count; i++)
                        if (!a.bindings[i].isComposite) rows++;
                }

                _scroll = GUI.BeginScrollView(listRect, _scroll, new Rect(0f, 0f, listRect.width - 24f, rows * rowH));
                float ry = 0f;
                for (int n = 0; n < ActionNames.Length; n++)
                {
                    var action = map.FindAction(ActionNames[n]);
                    if (action == null) continue;

                    for (int i = 0; i < action.bindings.Count; i++)
                    {
                        var b = action.bindings[i];
                        if (b.isComposite) continue;

                        string label = b.isPartOfComposite ? ActionLabels[n] + " " + b.name : ActionLabels[n];
                        GUI.Label(new Rect(0f, ry, 220f, 32f), label, UiStyles.Label);
                        GUI.Label(new Rect(230f, ry, 200f, 32f), action.GetBindingDisplayString(i), UiStyles.Label);
                        if (GUI.Button(new Rect(440f, ry, 130f, 32f), "Rebind"))
                            StartRebind(action, i, label);
                        ry += rowH;
                    }
                }
                GUI.EndScrollView();
            }

            float by = area.yMax - 54f;
            if (GUI.Button(new Rect(area.x + 20f, by, 190f, 40f), "Reset controls"))
                InputService.ResetBindingOverrides();

            string resetLabel = _confirmReset ? "Click again to erase" : "Reset progress";
            if (GUI.Button(new Rect(area.x + 224f, by, 210f, 40f), resetLabel))
            {
                if (_confirmReset)
                {
                    SaveService.ResetAll();
                    UnlockState.UnlockEverything = false;
                    _confirmReset = false;
                }
                else
                {
                    _confirmReset = true;
                }
            }

            GUI.enabled = true;
            if (GUI.Button(new Rect(area.xMax - 190f, by, 170f, 40f), "Back"))
            {
                CancelRebind();
                back = true;
            }
            return back;
        }

        void StartRebind(InputAction action, int bindingIndex, string label)
        {
            if (_op != null) return;
            _rebindText = label;
            action.Disable(); // an action cannot be rebound while enabled

            _op = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithCancelingThrough("<Keyboard>/backspace")
                .OnComplete(op => FinishRebind(action))
                .OnCancel(op => FinishRebind(action))
                .Start();
        }

        void FinishRebind(InputAction action)
        {
            if (_op != null)
            {
                _op.Dispose();
                _op = null;
            }
            action.Enable();
            InputService.SaveBindingOverrides();
        }
    }
}
