using System.IO;
using System.Text;
using Cellstorm.Combat;
using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Enemies;
using Cellstorm.Meta;
using Cellstorm.Progression;
using Cellstorm.Swarm;
using Cellstorm.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// F5 starts an automatic balance test: a simple bot plays several full runs at high speed and appends one CSV
    /// line per run to balance_log.csv in the persistent data folder. It never touches your save file (runs are not
    /// recorded) and treats everything as unlocked. F5 again stops it.
    /// The bot hunts enemies it can beat, avoids ones that out-DEF it, collects pickups, uses Ram / Jaw / Gun /
    /// Hardening in a basic way and picks upgrades with one of three policies. It is a rough measuring stick,
    /// not a good player: use it to compare numbers between tuning changes.
    /// </summary>
    public class BalanceBot : MonoBehaviour
    {
        public static bool Enabled { get; private set; }

        static int _runIndex;
        static int _runsRemaining;
        static int _sessionRuns;

        [SerializeField] float speedMultiplier = 8f;
        [SerializeField] int runsPerSession = 9;
        [SerializeField] float maxRunMinutes = 40f;

        static readonly string[] PolicyNames = { "DefFirst", "Balanced", "Random" };
        static readonly string[] LoadoutNames = { "Default", "Speedy", "Tank" };
        static readonly string[][] Priorities =
        {
            new[] { "evo_tank", "defense", "dense_cells", "max_cells", "regen", "speed", "gun", "jaw", "hardening" },
            new[] { "max_cells", "defense", "regen", "speed", "gun", "jaw", "hardening", "dense_cells", "evo_tank" },
            null
        };

        SwarmBody _body;
        AbilityController _abilities;
        int _kills;
        bool _logged;
        GUIStyle _style;

        public static string LogPath => Path.Combine(Application.persistentDataPath, "balance_log.csv");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Enabled = false;
            _runIndex = 0;
            _runsRemaining = 0;
            SaveService.Suspended = false;
            GameManager.SpeedMultiplier = 1f;
        }

        void OnEnable()
        {
            Enemy.Killed += OnKilled;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnKilled;
        }

        void OnKilled(Enemy e)
        {
            _kills++;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f5Key.wasPressedThisFrame)
            {
                if (Enabled) Stop("stopped by you");
                else Begin();
                return;
            }
            if (!Enabled) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            if (_body == null) _body = FindAnyObjectByType<SwarmBody>();
            if (_body != null && _abilities == null)
            {
                _abilities = _body.GetComponent<AbilityController>();
                if (_abilities != null) _abilities.BotControlled = true;
            }

            switch (gm.State)
            {
                case GameState.LoadoutSelect:
                    PickLoadout();
                    break;
                case GameState.LevelUp:
                    ChooseUpgrade();
                    break;
                case GameState.Playing:
                    Drive();
                    break;
                case GameState.Dead:
                    EndRun("Died");
                    break;
                case GameState.Won:
                    EndRun("Won");
                    break;
                default:
                    gm.SetState(GameState.Playing); // menus, pause, swap mode
                    break;
            }
        }

        // ---------------- Session control ----------------

        void Begin()
        {
            Enabled = true;
            _runIndex = 0;
            _sessionRuns = runsPerSession;
            _runsRemaining = runsPerSession;

            SaveService.Suspended = true;
            UnlockState.UnlockEverything = true;
            GameManager.SpeedMultiplier = speedMultiplier;
            Debug.Log("BalanceBot: starting " + runsPerSession + " runs at " + speedMultiplier + "x. Log: " + LogPath);
            SceneLoader.LoadGame();
        }

        void Stop(string reason)
        {
            Enabled = false;
            SaveService.Suspended = false;
            UnlockState.UnlockEverything = false;
            GameManager.SpeedMultiplier = 1f;

            if (_body != null)
            {
                _body.BotMove = null;
                _body.BotAimPoint = null;
            }
            if (_abilities != null) _abilities.BotControlled = false;
            if (GameManager.Instance != null) GameManager.Instance.RefreshTimeScale();
            Debug.Log("BalanceBot: " + reason + ". Results: " + LogPath);
        }

        int Policy => _runIndex % PolicyNames.Length;
        int LoadoutIndex => (_runIndex / PolicyNames.Length) % LoadoutNames.Length;

        void PickLoadout()
        {
            if (RunSetup.Instance != null) RunSetup.Instance.BotChoose(LoadoutIndex);
        }

        // ---------------- Upgrades ----------------

        void ChooseUpgrade()
        {
            var pp = PlayerProgress.Instance;
            var build = PlayerBuild.Instance;
            if (pp == null || build == null || pp.Options.Count == 0) return;

            UpgradeSO best = null;
            int bestRank = int.MaxValue;
            for (int i = 0; i < pp.Options.Count; i++)
            {
                var u = pp.Options[i];
                int rank = RankOf(u);
                if (build.NeedsReplaceTarget(u)) rank += 100; // avoid replacing weapons
                if (rank < bestRank)
                {
                    best = u;
                    bestRank = rank;
                }
            }
            if (best == null) best = pp.Options[0];

            int slot = -1;
            if (build.NeedsReplaceTarget(best)) slot = PlayerBuild.SlotsFor(best)[1];
            if (!pp.Choose(best, slot)) pp.Choose(pp.Options[0], -1);
        }

        int RankOf(UpgradeSO u)
        {
            var order = Priorities[Policy];
            if (order == null) return Random.Range(0, 100);
            for (int i = 0; i < order.Length; i++)
                if (order[i] == u.id) return i;
            return 50;
        }

        // ---------------- Playing ----------------

        static bool Threatens(Enemy e, float myDef)
        {
            float d = e.Stats.Defense;
            if (d > myDef) return true;
            if (e.Spec.behavior == EnemyBehavior.Explode) return true;
            var w = e.Spec.weapon;
            return w != null && (w.type == WeaponType.Gun || w.type == WeaponType.Jaw) && d * 2f > myDef;
        }

        void Drive()
        {
            if (_body == null || _body.IsDead) return;

            var run = RunDirector.Instance;
            if (run != null && run.Elapsed > maxRunMinutes * 60f)
            {
                EndRun("Timeout");
                return;
            }

            Vector2 p = _body.transform.position;
            float myDef = _body.Stats.Defense;

            Vector2 flee = Vector2.zero;
            Enemy target = null;
            float targetDist = float.MaxValue;
            Vector2 toTarget = Vector2.zero;
            float nearestThreat = float.MaxValue;

            var enemies = Enemy.Active;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null) continue;

                Vector2 d = (Vector2)e.transform.position - p;
                float dist = d.magnitude;
                if (Threatens(e, myDef))
                {
                    nearestThreat = Mathf.Min(nearestThreat, dist);
                    if (dist < 9f && dist > 0.001f) flee -= d / (dist * dist);
                }
                else if (dist < targetDist)
                {
                    target = e;
                    targetDist = dist;
                    toTarget = d;
                }
            }

            float pickupDist = 10f;
            Vector2 toPickup = Vector2.zero;
            bool hasPickup = false;
            var pickups = Pickup.Active;
            for (int i = 0; i < pickups.Count; i++)
            {
                var k = pickups[i];
                if (k == null) continue;
                Vector2 d = (Vector2)k.transform.position - p;
                float dist = d.magnitude;
                if (dist < pickupDist)
                {
                    pickupDist = dist;
                    toPickup = d;
                    hasPickup = true;
                }
            }

            Vector2 desire;
            if (flee.sqrMagnitude > 0.0001f) desire = flee.normalized;
            else if (hasPickup) desire = toPickup.normalized;
            else if (target != null) desire = toTarget.normalized;
            else desire = p.sqrMagnitude > 25f ? (-p).normalized : Vector2.zero;

            _body.BotMove = desire;
            _body.BotAimPoint = target != null
                ? (Vector2)target.transform.position
                : p + (desire.sqrMagnitude > 0.01f ? desire : Vector2.up) * 5f;

            DriveAbilities(target != null, targetDist, toTarget, nearestThreat);
        }

        void DriveAbilities(bool hasTarget, float targetDist, Vector2 toTarget, float nearestThreat)
        {
            if (_abilities == null) return;

            float hp = _body.Stats.Cells / (float)Mathf.Max(1, _body.Stats.MaxCells);
            bool aligned = hasTarget && Vector2.Angle(_body.AimDirection, toTarget) < 10f;

            for (int slot = 0; slot < AbilityController.SlotCount; slot++)
            {
                var a = _abilities.GetSlot(slot);
                bool held = false;

                if (a != null)
                {
                    switch (a.Data.type)
                    {
                        case WeaponType.Gun:
                        {
                            bool want = hasTarget && targetDist < 9f;
                            held = want && !(a.Phase == AbilityPhase.Primed && aligned);
                            break;
                        }
                        case WeaponType.Jaw:
                        {
                            bool close = hasTarget && targetDist < _body.Radius + a.Data.coneRange + 1.2f;
                            bool inBite = hasTarget && targetDist < _body.Radius + a.Data.coneRange * 0.9f &&
                                          Vector2.Angle(_body.AimDirection, toTarget) < 25f;
                            held = close && !(a.Phase == AbilityPhase.Primed && inBite);
                            break;
                        }
                        case WeaponType.Ram:
                            held = a.Phase == AbilityPhase.Ready && hasTarget && targetDist > 4f && targetDist < 8f && aligned;
                            break;
                        case WeaponType.Hardening:
                            held = a.Phase == AbilityPhase.Ready && hp < 0.6f && nearestThreat < 6f;
                            break;
                    }
                }
                _abilities.SetAiHeld(slot, held);
            }
        }

        // ---------------- Logging ----------------

        void EndRun(string outcome)
        {
            if (_logged) return;
            _logged = true;

            var run = RunDirector.Instance;
            var pp = PlayerProgress.Instance;
            var build = PlayerBuild.Instance;
            var sb = new StringBuilder();

            if (!File.Exists(LogPath))
                File.AppendAllText(LogPath, "time,run,policy,loadout,outcome,seconds,milestone,score,levelups,kills,max_cells,defense,speed,regen,weapons\n");

            sb.Append(System.DateTime.Now.ToString("HH:mm:ss")).Append(',');
            sb.Append(_runIndex + 1).Append(',');
            sb.Append(PolicyNames[Policy]).Append(',');
            sb.Append(LoadoutNames[LoadoutIndex]).Append(',');
            sb.Append(outcome).Append(',');
            sb.Append(run != null ? run.Elapsed.ToString("0") : "0").Append(',');
            sb.Append(run != null ? run.Milestone : 0).Append(',');
            sb.Append(run != null ? run.Score : 0).Append(',');
            sb.Append(pp != null ? pp.LevelsGained : 0).Append(',');
            sb.Append(_kills).Append(',');

            if (build != null && build.Stats != null)
            {
                var s = build.Stats;
                sb.Append(s.MaxCells).Append(',').Append(s.Defense.ToString("0.#")).Append(',');
                sb.Append(s.Speed.ToString("0.#")).Append(',').Append(s.CellRegen.ToString("0.#")).Append(',');
                sb.Append(Describe(build.Abilities));
            }
            else
            {
                sb.Append(",,,,");
            }
            sb.Append('\n');
            File.AppendAllText(LogPath, sb.ToString());

            _runIndex++;
            _runsRemaining--;
            if (_runsRemaining > 0) SceneLoader.LoadGame();
            else Stop("finished " + _sessionRuns + " runs");
        }

        static string Describe(AbilityController ac)
        {
            if (ac == null) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < AbilityController.SlotCount; i++)
            {
                var a = ac.GetSlot(i);
                if (a == null) continue;
                if (sb.Length > 0) sb.Append('+');
                sb.Append(a.Data.displayName);
            }
            return sb.ToString();
        }

        void OnGUI()
        {
            if (!Enabled) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperRight, fontStyle = FontStyle.Bold };
                _style.normal.textColor = new Color(1f, 0.8f, 0.3f);
            }
            GUI.Label(new Rect(Screen.width - 620f, 100f, 600f, 50f),
                "BALANCE BOT  run " + (_runIndex + 1) + "/" + _sessionRuns + "  policy " + PolicyNames[Policy] +
                "  loadout " + LoadoutNames[LoadoutIndex] + "  speed " + GameManager.SpeedMultiplier + "x\n(F5 stops)", _style);
        }
    }
}
