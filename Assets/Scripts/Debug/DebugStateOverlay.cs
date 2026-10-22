using Cellstorm.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// TEMPORARY. Shows the game state on screen and lets you force states with F1-F4:
    /// F1 LevelUp, F2 Won, F3 Dead, F4 back to Playing.
    /// Delete once the real UI exists.
    /// </summary>
    public class DebugStateOverlay : MonoBehaviour
    {
        void Update()
        {
            var gm = GameManager.Instance;
            var kb = Keyboard.current;
            if (gm == null || kb == null) return;

            if (kb.f1Key.wasPressedThisFrame) gm.SetState(GameState.LevelUp);
            if (kb.f2Key.wasPressedThisFrame) gm.SetState(GameState.Won);
            if (kb.f3Key.wasPressedThisFrame) gm.SetState(GameState.Dead);
            if (kb.f4Key.wasPressedThisFrame) gm.SetState(GameState.Playing);
        }

        void OnGUI()
        {
            if (!DebugVisibility.Show) return;
            var gm = GameManager.Instance;
            string state = gm != null ? gm.State.ToString() : "No GameManager";
            GUI.Label(new Rect(10, 10, 500, 25),
                $"State: {state}   timeScale: {Time.timeScale}   (ESC pause, F1-F4 force states)");
        }
    }
}
