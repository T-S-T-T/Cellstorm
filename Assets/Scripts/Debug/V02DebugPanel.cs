using Cellstorm.Combat;
using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Enemies;
using Cellstorm.Progression;
using Cellstorm.Swarm;
using Cellstorm.Weapons;
using Cellstorm.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// V0.2 test panel (editor and development builds only). Press the backquote key (`) to show or hide it.
    /// Gives any weapon or skill, adds card stacks, applies status effects to the player, spawns every enemy and boss
    /// weapon, resource points, hazards and pickups. All mouse driven so it never clashes with the F-key tools.
    /// </summary>
    public class V02DebugPanel : MonoBehaviour
    {
        bool _open;
        Vector2 _scroll;
        int _tab;
        int _bossNumber = 1;
        System.Collections.Generic.List<string> _testLog;
        static readonly string[] Tabs = { "Items", "Cards", "Status", "Spawn", "World", "Misc" };
        static readonly string[] SlotNames = { "LMB", "RMB", "Q", "E" };

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.backquoteKey.wasPressedThisFrame) _open = !_open;
        }

        void OnGUI()
        {
            if (!DebugGate.Allowed) return;
            var gm = GameManager.Instance;
            if (!_open)
            {
                GUI.Label(new Rect(8f, Screen.height - 24f, 360f, 22f), "` = V0.2 debug panel");
                return;
            }
            var build = PlayerBuild.Instance;
            if (build == null || gm == null) return;

            GUI.depth = -10;
            var area = new Rect(8f, 40f, 420f, Screen.height - 90f);
            GUI.Box(area, "V0.2 debug panel (` to close)");
            GUILayout.BeginArea(new Rect(area.x + 8f, area.y + 26f, area.width - 16f, area.height - 34f));
            _tab = GUILayout.Toolbar(_tab, Tabs);
            _scroll = GUILayout.BeginScrollView(_scroll);

            switch (_tab)
            {
                case 0: Items(build); break;
                case 1: Cards(build); break;
                case 2: Status(build); break;
                case 3: Spawn(build); break;
                case 4: WorldTab(build); break;
                default: Misc(build); break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ---------------- Tabs ----------------

        static readonly string[] ItemKeys =
        {
            WeaponDefaults.RamKey, WeaponDefaults.JawKey, WeaponDefaults.GunKey, WeaponDefaults.WhipKey,
            WeaponDefaults.SpreadShotKey, WeaponDefaults.SporeCloudKey, WeaponDefaults.MineKey,
            WeaponDefaults.HardeningKey, WeaponDefaults.MinionKey, WeaponDefaults.ConvertKey, WeaponDefaults.ShockwaveKey
        };

        void Items(PlayerBuild build)
        {
            var ac = build.Abilities;
            GUILayout.Label("Equip into a slot (works even if locked):");
            foreach (var key in ItemKeys)
            {
                var w = WeaponDefaults.ByKey(key);
                GUILayout.BeginHorizontal();
                GUILayout.Label(w.displayName, GUILayout.Width(130f));
                int first = w.kind == AbilityKind.Weapon ? 0 : 2;
                for (int s = first; s < first + 2; s++)
                    if (GUILayout.Button(SlotNames[s], GUILayout.Width(60f))) ac.Equip(s, w);
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(6f);
            for (int s = 0; s < AbilityController.SlotCount; s++)
            {
                var a = ac.GetSlot(s);
                GUILayout.Label(SlotNames[s] + ": " + (a != null ? a.Data.displayName + " - " + a.Status : "empty"));
            }
        }

        void Cards(PlayerBuild build)
        {
            var pp = PlayerProgress.Instance;
            if (pp == null) return;
            GUILayout.Label("+1 stack ignores eligibility. (e) = eligible now.");
            foreach (var u in pp.AllUpgrades)
            {
                if (u == null || u.effect == UpgradeEffect.GrantWeapon) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label((build.IsEligible(u) ? "(e) " : "    ") + u.displayName + "  x" + build.Stacks(u), GUILayout.Width(300f));
                if (GUILayout.Button("+1", GUILayout.Width(40f))) build.Apply(u);
                GUILayout.EndHorizontal();
            }
        }

        void Status(PlayerBuild build)
        {
            var st = build.Stats.Status;
            Vector2 p = build.transform.position;
            if (GUILayout.Button("Toxic 5 HP/s for 3 s")) st.ApplyToxic(5f, 3f, Faction.Enemy, false);
            if (GUILayout.Button("Toxic 15 HP/s for 3 s (stronger replaces)")) st.ApplyToxic(15f, 3f, Faction.Enemy, false);
            if (GUILayout.Button("Slow 40% for 3 s")) st.ApplySlow(0.4f, 3f);
            if (GUILayout.Button("Slow 95% for 3 s (floor: never below 20% speed)")) st.ApplySlow(0.95f, 3f);
            if (GUILayout.Button("Stun 1.5 s (1 s immunity afterwards)")) st.ApplyStun(1.5f);
            if (GUILayout.Button("Shove away 3 units")) st.Shove(p + Vector2.left, 3f);
            if (GUILayout.Button("Pull toward a point 3 units away")) st.Shove(p + Vector2.right * 3f, -3f);
            if (GUILayout.Button("Clear all effects")) st.ClearAll();
            GUILayout.Label("Toxic " + st.ToxicActive + " (" + st.ToxicDps.ToString("0.#") + " HP/s, " + st.ToxicRemaining.ToString("0.0") + " s)\n" +
                            "Slow " + st.SlowActive + " (" + Mathf.RoundToInt(st.SlowPct * 100f) + "%, " + st.SlowRemaining.ToString("0.0") + " s)\n" +
                            "Stun " + st.Stunned + " (" + st.StunRemaining.ToString("0.0") + " s, immune " + st.ImmunityRemaining.ToString("0.0") + " s)\n" +
                            "Speed factor " + st.SpeedFactor.ToString("0.00"));
        }

        void Spawn(PlayerBuild build)
        {
            var spawner = EnemySpawner.Instance;
            if (spawner == null) { GUILayout.Label("No EnemySpawner."); return; }
            Vector2 p = build.transform.position;
            Vector2 at = p + new Vector2(5f, 0f);

            GUILayout.Label("Enemy types:");
            foreach (var t in spawner.AllTypes())
                if (GUILayout.Button(t.displayName)) spawner.DebugSpawn(t, at + Random.insideUnitCircle);

            GUILayout.Space(6f);
            GUILayout.Label("Boss number: " + _bossNumber);
            _bossNumber = Mathf.RoundToInt(GUILayout.HorizontalSlider(_bossNumber, 1, 8));
            foreach (var w in WeaponDefaults.BossPool())
                if (GUILayout.Button("Boss " + _bossNumber + " with " + w.displayName)) spawner.SpawnBoss(_bossNumber, w, at);

            GUILayout.Space(6f);
            if (GUILayout.Button("Spawn 10 Single Cells nearby"))
            {
                foreach (var t in spawner.AllTypes())
                    if (t.displayName == "Single Cell")
                        for (int i = 0; i < 10; i++) spawner.DebugSpawn(t, p + Random.insideUnitCircle * 6f);
            }
        }

        void WorldTab(PlayerBuild build)
        {
            var rp = ResourcePointDirector.Instance;
            var hz = HazardDirector.Instance;
            Vector2 p = build.transform.position;

            GUILayout.Label("Resource points (" + ResourcePoint.Active.Count + " active):");
            foreach (GuardKind k in System.Enum.GetValues(typeof(GuardKind)))
                if (GUILayout.Button("Spawn " + k + " point") && rp != null) rp.Spawn(k);
            if (GUILayout.Button("Capture nearest point"))
            {
                ResourcePoint best = null;
                float bd = float.MaxValue;
                foreach (var r in ResourcePoint.Active)
                {
                    float d = ((Vector2)r.transform.position - p).sqrMagnitude;
                    if (d < bd) { bd = d; best = r; }
                }
                if (best != null) best.DebugCapture();
            }

            GUILayout.Space(6f);
            GUILayout.Label("Hazards (" + HazardPatch.Active.Count + " active):");
            if (GUILayout.Button("Toxic zone") && hz != null) hz.Spawn(HazardKind.Toxic);
            if (GUILayout.Button("Current") && hz != null) hz.Spawn(HazardKind.Current);
            if (GUILayout.Button("Toxic zone ON the player"))
                HazardPatch.Create(p, HazardKind.Toxic, 2.5f, Vector2.right);
            if (GUILayout.Button("Current ON the player"))
                HazardPatch.Create(p, HazardKind.Current, 2.5f, Vector2.right);

            GUILayout.Space(6f);
            GUILayout.Label("Pickups at the player:");
            foreach (PickupType t in System.Enum.GetValues(typeof(PickupType)))
                if (GUILayout.Button(t.ToString()))
                {
                    if (t == PickupType.HpOrb) Pickup.SpawnHeal(t, p + Vector2.up, 10f);
                    else Pickup.Spawn(t, p + Vector2.up, t == PickupType.Exp ? 10 : 1);
                }
        }

        void Misc(PlayerBuild build)
        {
            var stats = build.Stats;
            GUILayout.Label("Level-up options per screen: " + PlayerProgress.OptionCountSetting);
            PlayerProgress.OptionCountSetting = Mathf.RoundToInt(GUILayout.HorizontalSlider(PlayerProgress.OptionCountSetting, 1, 12));
            PlayerProgress.DebugShowAll = GUILayout.Toggle(PlayerProgress.DebugShowAll, " Show every eligible upgrade");
            Core.UnlockState.UnlockEverything = GUILayout.Toggle(Core.UnlockState.UnlockEverything, " Treat everything as unlocked");
            stats.Invulnerable = GUILayout.Toggle(stats.Invulnerable, " God mode");

            if (GUILayout.Button("Give a level-up")) PlayerProgress.Instance?.AddExp(PlayerProgress.Instance.ExpToNext);
            if (GUILayout.Button("Add 5 rerolls")) PlayerProgress.Instance?.AddRerolls(5);
            if (GUILayout.Button("Jump to next milestone (spawns the boss)")) RunDirector.Instance?.DebugJumpToNextMilestone();
            if (GUILayout.Button("DEF +10 (hazard immunity needs DEF 50)")) stats.AddToBase(StatType.Defense, 10f);
            if (GUILayout.Button("Set DEF to 50")) stats.AddToBase(StatType.Defense, 50f - stats.Defense);
            if (GUILayout.Button("Heal to full")) stats.Heal(stats.MaxHp);
            if (GUILayout.Button("Max HP +100 and heal")) { stats.AddToBase(StatType.MaxHp, 100f); stats.Heal(100f); }

            GUILayout.Space(6f);
            if (GUILayout.Button("Run V0.2 self-test")) _testLog = V02SelfTest.Run(build);
            if (_testLog != null) foreach (var l in _testLog) GUILayout.Label(l);

            GUILayout.Space(6f);
            GUILayout.Label("Minions alive: " + Minion.Count + "    Enemies: " + Enemy.Active.Count +
                            "    Weapon cells: " + WeaponCellGroup.TotalCells);
            GUILayout.Label("Stacks: " + build.Describe());
            GUILayout.Label("Evolutions: " + (build.Evolutions.Count == 0 ? "-" : build.DescribeEvolutions()) + "   Bosses defeated: " + build.BossesDefeated);
        }
    }
}
