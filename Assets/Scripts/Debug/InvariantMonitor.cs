using Cellstorm.Progression;
using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// Watches the V0.1.1 rules while you play: 0 &lt;= HeldHp &lt;= Hp &lt;= MaxHp, the weapon cells' HP adding up to
    /// HeldHp, and never more weapon cells than the swarm's cell count. Violations are logged (once a second at most)
    /// and counted on the F12 overlay. A clean run shows 0 violations.
    /// </summary>
    public class InvariantMonitor : MonoBehaviour
    {
        public static int Violations { get; private set; }
        public static string Last { get; private set; } = "";

        float _nextLog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Violations = 0;
            Last = "";
        }

        void FixedUpdate()
        {
            var build = PlayerBuild.Instance;
            if (build == null || build.Stats == null || build.Stats.IsDead) return;

            var s = build.Stats;
            if (!s.InvariantsHold()) Fail($"HeldHp {s.HeldHp:F2} Hp {s.Hp:F2} Max {s.MaxHp:F2}");

            var ab = build.Abilities;
            if (ab == null) return;

            if (ab.LiveWeaponCells() > ab.WeaponCellCount) Fail($"{ab.LiveWeaponCells()} weapon cells > {ab.WeaponCellCount}");

            for (int i = 0; i < AbilityController.SlotCount; i++)
            {
                if (!(ab.GetSlot(i) is ShapedAbility sa) || sa.Group == null || !sa.Group.Deployed) continue;
                float sum = sa.Group.ComputeCellHpSum();
                if (Mathf.Abs(sum - sa.Group.HeldSum) > 0.02f || Mathf.Abs(sum - s.HeldHp) > 0.02f)
                    Fail($"cells hold {sum:F3}, group {sa.Group.HeldSum:F3}, stats {s.HeldHp:F3}");
            }
        }

        void Fail(string what)
        {
            Violations++;
            Last = what;
            if (Time.unscaledTime < _nextLog) return;
            _nextLog = Time.unscaledTime + 1f;
            Debug.LogError("Cellstorm invariant violated: " + what);
        }

        void OnGUI()
        {
            if (!DebugVisibility.Show) return;
            GUI.Label(new Rect(10, 165, 800, 22),
                "Invariants: " + (Violations == 0 ? "ok" : Violations + " violations, last: " + Last));
        }
    }
}
