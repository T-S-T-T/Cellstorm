using Cellstorm.Core;
using Cellstorm.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.UI
{
    /// <summary>
    /// Swap mode (GDD): F freezes the game and highlights the slots. Click an occupied slot to pick its item up,
    /// then press a slot key to place it: the item's own key puts it back, the other key of the same kind moves it
    /// (swapping with whatever is there). Any other key shakes it with an error cue and does nothing.
    /// Drawing is done by GameHud.
    /// </summary>
    public class SwapModeController : MonoBehaviour
    {
        public static int Selected { get; private set; } = -1;
        public static float ShakeUntil { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Selected = -1;
            ShakeUntil = 0f;
        }

        void OnDestroy()
        {
            Selected = -1;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var build = PlayerBuild.Instance;
            if (gm == null || build == null || build.Abilities == null) return;
            var abilities = build.Abilities;

            if (gm.State == GameState.Playing)
            {
                Selected = -1;
                if (!build.Stats.IsDead && InputService.Swap.WasPressedThisFrame()) gm.SetState(GameState.SwapMode);
                return;
            }
            if (gm.State != GameState.SwapMode)
            {
                Selected = -1;
                return;
            }

            if (InputService.Swap.WasPressedThisFrame() || InputService.Pause.WasPressedThisFrame())
            {
                Selected = -1;
                gm.SetState(GameState.Playing);
                return;
            }

            if (Selected < 0)
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    Vector2 p = mouse.position.ReadValue();
                    var gui = new Vector2(p.x, Screen.height - p.y);
                    for (int i = 0; i < Cellstorm.Weapons.AbilityController.SlotCount; i++)
                    {
                        if (abilities.GetSlot(i) != null && HudLayout.Slot(i).Contains(gui))
                        {
                            Selected = i;
                            AudioHooks.Play(AudioCue.Select);
                            break;
                        }
                    }
                }
                return;
            }

            int target = PressedSlot();
            if (target >= 0)
            {
                if (abilities.SwapSlots(Selected, target))
                {
                    AudioHooks.Play(AudioCue.Assign);
                    Selected = -1;
                }
                else
                {
                    Wrong();
                }
            }
            else if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                Wrong(); // anything else indicates "wrong"
            }
        }

        static int PressedSlot()
        {
            if (InputService.Attack1.WasPressedThisFrame()) return 0;
            if (InputService.Attack2.WasPressedThisFrame()) return 1;
            if (InputService.Skill1.WasPressedThisFrame()) return 2;
            if (InputService.Skill2.WasPressedThisFrame()) return 3;
            return -1;
        }

        static void Wrong()
        {
            AudioHooks.Play(AudioCue.Error);
            ShakeUntil = Time.unscaledTime + 0.4f;
        }
    }
}
