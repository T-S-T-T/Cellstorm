using Cellstorm.Core;
using Cellstorm.Progression;
using Cellstorm.Swarm;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// TEMPORARY test harness for Phase 5.
    /// F6 gain a level   F7 +1 reroll   F8 spawn shells   F9 spawn EXP orbs   F10 take Dense Cells x5
    /// </summary>
    public class ProgressDebug : MonoBehaviour
    {
        SwarmBody _player;

        void Start()
        {
            _player = FindAnyObjectByType<SwarmBody>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            var pp = PlayerProgress.Instance;
            if (kb == null || pp == null || _player == null) return;

            Vector2 p = _player.transform.position;

            if (kb.f6Key.wasPressedThisFrame) pp.AddExp(Mathf.Max(1, pp.ExpToNext - pp.Exp));
            if (kb.f7Key.wasPressedThisFrame) pp.AddRerolls(1);
            if (kb.f8Key.wasPressedThisFrame) Pickup.Spawn(PickupType.Shells, p + Random.insideUnitCircle.normalized * 3f);
            if (kb.f9Key.wasPressedThisFrame)
            {
                for (int i = 0; i < 5; i++)
                    Pickup.Spawn(PickupType.Exp, p + Random.insideUnitCircle.normalized * Random.Range(2f, 5f), 3);
            }
            if (kb.f10Key.wasPressedThisFrame)
            {
                var dense = pp.FindUpgrade("dense_cells");
                var build = PlayerBuild.Instance;
                if (dense != null && build != null)
                    for (int i = 0; i < 5; i++) build.Apply(dense);
            }
        }

        void OnGUI()
        {
            if (!DebugVisibility.Show) return;
            var pp = PlayerProgress.Instance;
            var build = PlayerBuild.Instance;
            if (pp == null || build == null) return;

            GUI.Label(new Rect(10, 300, 1400, 25),
                "EXP " + pp.Exp + "/" + pp.ExpToNext + "   Level-ups gained " + pp.LevelsGained +
                "   Pending " + pp.PendingLevelUps + "   Rerolls " + pp.Rerolls +
                "   Pickups on map " + Pickup.Active.Count + "   Loadout " + RunSetup.SelectedLoadout +
                (build.TankEvolved ? "   [TANK]" : ""));
            GUI.Label(new Rect(10, 320, 1400, 25), "Build: " + build.Describe());
            GUI.Label(new Rect(10, 340, 1400, 25),
                "F6 gain a level   F7 +1 reroll   F8 spawn shells   F9 spawn EXP orbs   F10 take Dense Cells x5 (then level up to find the evolution)");
        }
    }
}
