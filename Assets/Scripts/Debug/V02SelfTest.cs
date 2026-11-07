using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Data;
using Cellstorm.Progression;
using UnityEngine;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// Logic checks for the V0.2 status rules, driven by StatusController.Tick on the player (no waiting).
    /// Run it from the V0.2 debug panel (Misc tab). The player's effects are cleared before and after.
    /// </summary>
    public static class V02SelfTest
    {
        public static List<string> Run(PlayerBuild build)
        {
            var log = new List<string>();
            if (build == null) { log.Add("FAIL  no player"); return log; }

            var stats = build.Stats;
            var st = stats.Status;
            bool god = stats.Invulnerable;
            stats.Invulnerable = true;      // only the status bookkeeping is checked, not the damage
            st.ClearAll();

            // Toxic: stronger replaces, equal/weaker refreshes the duration only.
            st.ApplyToxic(5f, 3f, Faction.Enemy, false);
            st.Tick(1f);
            st.ApplyToxic(3f, 3f, Faction.Enemy, false);
            Check(log, "Toxic weaker keeps 5 HP/s", Mathf.Approximately(st.ToxicDps, 5f));
            Check(log, "Toxic weaker refreshes to 3 s", Mathf.Approximately(st.ToxicRemaining, 3f));
            st.ApplyToxic(9f, 2f, Faction.Enemy, false);
            Check(log, "Toxic stronger replaces", Mathf.Approximately(st.ToxicDps, 9f) && Mathf.Approximately(st.ToxicRemaining, 2f));
            st.Tick(2.5f);
            Check(log, "Toxic ends", !st.ToxicActive);

            // Slow: floor at 20% speed, strongest wins.
            st.ClearAll();
            st.ApplySlow(0.95f, 2f);
            Check(log, "Slow floor 20%", Mathf.Approximately(st.SpeedFactor, 0.2f));
            st.ApplySlow(0.3f, 5f);
            Check(log, "Weaker slow does not replace", Mathf.Approximately(st.SlowPct, 0.8f));
            st.Tick(6f);
            Check(log, "Slow ends, full speed", Mathf.Approximately(st.SpeedFactor, 1f));

            // Stun: 1 s immunity afterwards.
            st.ClearAll();
            Check(log, "Stun starts", st.ApplyStun(0.5f));
            Check(log, "Stunned: speed factor 0", Mathf.Approximately(st.SpeedFactor, 0f));
            Check(log, "Second stun ignored while stunned", !st.ApplyStun(0.5f));
            st.Tick(0.6f);
            Check(log, "Stun over", !st.Stunned);
            Check(log, "Immune right after stun", !st.ApplyStun(0.5f));
            st.Tick(1.1f);
            Check(log, "Stun possible after immunity", st.ApplyStun(0.5f));

            // Boss scaling.
            st.ClearAll();
            st.IsBoss = true;
            st.ApplySlow(0.5f, 4f);
            Check(log, "Boss slow lasts half", Mathf.Approximately(st.SlowRemaining, 2f));
            st.ClearAll();
            st.ApplyStun(2f);
            Check(log, "Boss stun lasts half", Mathf.Approximately(st.StunRemaining, 1f));
            st.IsBoss = false;

            // Card catalog sanity.
            var cards = UpgradeDefaults.CreateAll();
            var ids = new HashSet<string>();
            bool dup = false;
            foreach (var u in cards) if (u != null && !ids.Add(u.id)) dup = true;
            Check(log, "Card ids are unique (" + cards.Length + " upgrades)", !dup);

            // HP invariant.
            Check(log, "HP invariant 0 <= held <= HP <= max", stats.HeldHp >= 0f && stats.HeldHp <= stats.Hp + 0.001f && stats.Hp <= stats.MaxHp + 0.001f);

            st.ClearAll();
            stats.Invulnerable = god;

            int fails = log.FindAll(l => l.StartsWith("FAIL")).Count;
            log.Insert(0, fails == 0 ? "ALL PASSED (" + (log.Count) + " checks)" : fails + " FAILED of " + log.Count);
            foreach (var l in log) if (l.StartsWith("FAIL")) Debug.LogWarning("V0.2 self-test: " + l);
            return log;
        }

        static void Check(List<string> log, string name, bool ok)
        {
            log.Add((ok ? "ok    " : "FAIL  ") + name);
        }
    }
}
