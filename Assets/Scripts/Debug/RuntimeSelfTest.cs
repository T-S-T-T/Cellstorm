using System.Collections;
using System.Text;
using Cellstorm.Combat;
using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Enemies;
using Cellstorm.Progression;
using Cellstorm.Swarm;
using Cellstorm.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// F6 in the Game scene runs a scripted check of the V0.1.1 combat rules on the live player (it takes about 25
    /// seconds, do not touch the keyboard): Ram damage once per dash, Jaw / Gun deploy, the HP the cells hold,
    /// cells dying, the HP threshold, spam clicking, and the one-HP-weapon rule. Results go to the Console and the
    /// F12 overlay. It changes your run (HP, weapons) so use it on a throwaway run.
    /// </summary>
    public class RuntimeSelfTest : MonoBehaviour
    {
        public static bool Running { get; private set; }
        public static string Report { get; private set; } = "";

        SwarmBody _body;
        AbilityController _ab;
        int _pass, _fail;
        readonly StringBuilder _log = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Running = false;
            Report = "";
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f6Key.wasPressedThisFrame && !Running) StartCoroutine(Run());
        }

        void OnGUI()
        {
            if (!DebugVisibility.Show && !Running) return;
            if (Report.Length == 0) return;
            GUI.Label(new Rect(10, 185, 900, 22), Report);
        }

        void Check(string name, bool ok, string detail = "")
        {
            if (ok) _pass++;
            else
            {
                _fail++;
                Debug.LogError("RUNTIME TEST FAIL: " + name + " " + detail);
            }
            _log.AppendLine((ok ? "ok   " : "FAIL ") + name + " " + detail);
        }

        IEnumerator Run()
        {
            _body = FindAnyObjectByType<SwarmBody>();
            var gm = GameManager.Instance;
            if (_body == null || gm == null || gm.State != GameState.Playing)
            {
                Report = "Runtime test: start a run first (state must be Playing).";
                yield break;
            }

            Running = true;
            _pass = _fail = 0;
            _log.Clear();
            Report = "Runtime test running... hands off the keyboard";
            _ab = _body.GetComponent<AbilityController>();
            _ab.BotControlled = true;
            var stats = _body.Stats;
            stats.Invulnerable = false;

            // Clean slate: only Ram, Jaw on slot 0 later.
            DestroyEnemies();
            _ab.Unequip(0); _ab.Unequip(1);
            _ab.Equip(0, WeaponDefaults.Ram);
            _ab.Equip(1, WeaponDefaults.Jaw);
            stats.DebugSetMaxAndHp(100f);
            stats.SetHeldHp(0f);
            _body.BotMove = Vector2.zero;
            _body.BotAimPoint = (Vector2)_body.transform.position + Vector2.right * 5f;
            yield return new WaitForSeconds(0.5f);

            // ---- 1. Ram: 30 damage once per dash, reduced by the enemy's DEF.
            var dummy = Spawn(500f, 1f, 1.4f);
            float before = dummy.Stats.Hp;
            _ab.SetAiHeld(0, true);
            yield return null; yield return null;
            _ab.SetAiHeld(0, false);
            yield return new WaitForSeconds(0.6f);
            float lost = before - dummy.Stats.Hp;
            float expected = DefCurve.Apply(30f, 1f);
            Check("Ram hits once for 30 minus DEF", Mathf.Abs(lost - expected) < 0.5f, $"lost {lost:F1}, expected {expected:F1}");
            yield return new WaitForSeconds(1.2f);
            DestroyEnemies();

            // ---- 2. Jaw deploys 100 cells holding 10% of max HP.
            Teleport();
            int n = _ab.WeaponCellCount;
            _ab.SetAiHeld(1, true);
            yield return new WaitForSeconds(0.6f);
            var jaw = _ab.GetSlot(1) as ShapedAbility;
            Check("Jaw is shaping", jaw != null && jaw.Phase == AbilityPhase.Shaping, jaw?.Phase.ToString());
            Check("Jaw uses the fixed cell count", jaw != null && jaw.CellCount == n, $"{jaw?.CellCount} vs {n}");
            Check("Jaw holds 10% of max HP", Mathf.Abs(stats.HeldHp - 10f) < 0.05f, $"held {stats.HeldHp:F2}");
            Check("Invariants while shaping", stats.InvariantsHold());

            // ---- 3. Killing cells drops progress and keeps the invariants.
            float progressBefore = jaw.Progress;
            WeaponCellGroup.DamageCircle(_body.transform.position, 3f, 1f, Faction.Enemy);
            yield return null;
            Check("Cells died and progress dropped", jaw.Progress < progressBefore || jaw.Phase != AbilityPhase.Shaping,
                $"{progressBefore:F2} -> {jaw.Progress:F2}");
            Check("Invariants after cells died", stats.InvariantsHold() && Mathf.Abs(jaw.Group.ComputeCellHpSum() - stats.HeldHp) < 0.05f || jaw.Phase == AbilityPhase.Cooldown,
                $"held {stats.HeldHp:F2}, cells {jaw.Group.ComputeCellHpSum():F2}");

            // ---- 4. Finish: fully primed after the deploy time, release returns the HP.
            yield return new WaitForSeconds(3.5f);
            Check("Jaw primed after the deploy time", jaw.Phase == AbilityPhase.Primed, jaw.Phase.ToString());
            float hpBeforeRelease = stats.Hp;
            _ab.SetAiHeld(1, false);
            yield return new WaitForSeconds(0.5f);
            Check("HeldHp returns to 0 after the bite", stats.HeldHp < 0.01f, $"held {stats.HeldHp:F2}");
            Check("Cells keep their HP in the body", stats.Hp >= hpBeforeRelease - 0.5f, $"{hpBeforeRelease:F1} -> {stats.Hp:F1}");
            yield return new WaitForSeconds(3.4f);

            // ---- 5. Threshold: below the cost the weapon releases itself.
            _ab.SetAiHeld(1, true);
            yield return new WaitForSeconds(0.5f);
            stats.SetHp(5f);
            yield return new WaitForSeconds(0.3f);
            Check("Weapon released when HP < cost", jaw.Phase == AbilityPhase.Cooldown || jaw.Phase == AbilityPhase.Ready, jaw.Phase.ToString());
            _ab.SetAiHeld(1, false);
            stats.DebugSetMaxAndHp(100f);
            yield return new WaitForSeconds(1.2f);

            // ---- 6. Spam clicking never leaves extra cells.
            int maxCells = 0;
            for (int i = 0; i < 100; i++)
            {
                _ab.SetAiHeld(1, true);
                yield return null;
                _ab.SetAiHeld(1, false);
                yield return null;
                maxCells = Mathf.Max(maxCells, _ab.LiveWeaponCells());
            }
            yield return new WaitForSeconds(1.5f);
            Check("100 rapid clicks never exceed the cell count", maxCells <= n, $"max {maxCells}, limit {n}");
            Check("No cells left behind", _ab.LiveWeaponCells() == 0, $"{_ab.LiveWeaponCells()} left");

            // ---- 7. Only one HP-cost weapon at a time (Jaw + Gun).
            _ab.Unequip(0);
            _ab.Equip(0, WeaponDefaults.Gun);
            yield return new WaitForSeconds(3.2f);
            stats.DebugSetMaxAndHp(100f);
            _ab.SetAiHeld(1, true);
            yield return new WaitForSeconds(0.4f);
            _ab.SetAiHeld(0, true);
            yield return new WaitForSeconds(0.4f);
            var gun = _ab.GetSlot(0) as ShapedAbility;
            Check("Gun cannot start while Jaw is deployed", gun != null && gun.Phase == AbilityPhase.Ready, gun?.Phase.ToString());
            _ab.SetAiHeld(1, false);
            _ab.SetAiHeld(0, false);
            yield return new WaitForSeconds(4f);

            // ---- Done
            _ab.SetAiHeld(0, false); _ab.SetAiHeld(1, false);
            _ab.BotControlled = false;
            _body.BotMove = null;
            _body.BotAimPoint = null;
            DestroyEnemies();
            Running = false;
            Report = $"Runtime test finished: {_pass} passed, {_fail} failed (see Console).";
            Debug.Log(Report + "\n" + _log);
        }

        void Teleport()
        {
            _body.BotAimPoint = (Vector2)_body.transform.position + Vector2.right * 5f;
        }

        Enemy Spawn(float hp, float def, float distance)
        {
            var spec = new EnemySpec
            {
                name = "TestDummy",
                color = Color.gray,
                maxHp = hp,
                defense = def,
                speed = 0f,
                contactDps = 0f,
                damageMultiplier = 1f,
                behavior = EnemyBehavior.Chase,
                scoreValue = 0,
                expValue = 0,
                milestone = 1
            };
            Vector2 p = (Vector2)_body.transform.position + Vector2.right * distance;
            return Enemy.Create(spec, p);
        }

        static void DestroyEnemies()
        {
            for (int i = Enemy.Active.Count - 1; i >= 0; i--)
                if (Enemy.Active[i] != null) Enemy.Active[i].Despawn();
        }
    }
}
