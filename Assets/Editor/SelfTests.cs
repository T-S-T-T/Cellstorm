using Cellstorm.Combat;
using UnityEditor;
using UnityEngine;

namespace Cellstorm.EditorTools
{
    /// <summary>
    /// Small checks of the V0.1.1 rules (DEF curve, Speed math, fixed sizes, HP model).
    /// Run from Cellstorm > Tests > Run self tests. Results go to the Console.
    /// (Proper NUnit tests come later, when the scripts move into an assembly definition.)
    /// </summary>
    public static class SelfTests
    {
        static int _passed;
        static int _failed;

        [MenuItem("Cellstorm/Tests/Run self tests")]
        public static void Run()
        {
            _passed = 0;
            _failed = 0;

            DefCurveTable();
            SpeedMathValues();
            SizeRules();
            HpModel();

            string summary = "Cellstorm self tests: " + _passed + " passed, " + _failed + " failed.";
            if (_failed == 0) Debug.Log(summary);
            else Debug.LogError(summary);
        }

        static void Near(string name, float actual, float expected, float tolerance = 0.005f)
        {
            if (Mathf.Abs(actual - expected) <= tolerance)
            {
                _passed++;
            }
            else
            {
                _failed++;
                Debug.LogError("FAIL " + name + ": expected " + expected + ", got " + actual);
            }
        }

        static void True(string name, bool condition)
        {
            if (condition)
            {
                _passed++;
            }
            else
            {
                _failed++;
                Debug.LogError("FAIL " + name);
            }
        }

        // GDD 4.3: DEF 1, 2, 5, 10, 25, 50 -> 5%, 20%, 40%, 55%, 75%, 90%.
        static void DefCurveTable()
        {
            Near("DEF 0", DefCurve.Reduction(0f), 0f);
            Near("DEF 0.5", DefCurve.Reduction(0.5f), 0f);
            Near("DEF 1", DefCurve.Reduction(1f), 0.05f);
            Near("DEF 2", DefCurve.Reduction(2f), 0.20f, 0.01f);
            Near("DEF 5", DefCurve.Reduction(5f), 0.40f, 0.01f);
            Near("DEF 10", DefCurve.Reduction(10f), 0.55f, 0.01f);
            Near("DEF 25", DefCurve.Reduction(25f), 0.75f, 0.01f);
            Near("DEF 50", DefCurve.Reduction(50f), 0.90f, 0.01f);
            Near("DEF 500 is capped", DefCurve.Reduction(500f), 0.9f);
            Near("Apply 100 at DEF 2", DefCurve.Apply(100f, 2f), 79.9f, 0.2f);
        }

        // GDD 4.4: Speed 2 -> factor 1, Speed 5 -> about 0.94, never below 0.1 s.
        static void SpeedMathValues()
        {
            Near("time factor Speed 2", SpeedMath.TimeFactor(2f), 1f);
            Near("time factor Speed 5", SpeedMath.TimeFactor(5f), 0.941f);
            Near("move speed 2", SpeedMath.MoveSpeed(2f), 2f);
            Near("move speed 5", SpeedMath.MoveSpeed(5f), 5f);
            Near("scaled 3 s at Speed 2", SpeedMath.Scaled(3f, 2f), 3f);
            Near("scaled never below 0.1", SpeedMath.Scaled(0.2f, 200f), 0.1f);
            Near("scaled 0 stays 0", SpeedMath.Scaled(0f, 2f), 0f);
        }

        // GDD 4.1: fixed sizes.
        static void SizeRules()
        {
            Near("player radius", SwarmSize.ForPlayer(1f), 0.335f, 0.0001f);
            Near("giant radius", SwarmSize.ForPlayer(2f), 0.67f, 0.0001f);
            Near("collider = visual x 1.1", SwarmSize.Collider(0.335f), 0.3685f, 0.0001f);
            Near("single cell (10 HP) is clamped to 0.15", SwarmSize.ForEnemy(10f), 0.15f, 0.0001f);
            Near("20 HP", SwarmSize.ForEnemy(20f), 0.15f, 0.0001f);
            Near("multi cell (100 HP)", SwarmSize.ForEnemy(100f), 0.335f, 0.001f);
            Near("ranged cell (500 HP)", SwarmSize.ForEnemy(500f), 0.75f, 0.001f);
            Near("huge boss clamps to 6", SwarmSize.ForEnemy(1000000f), 6f, 0.0001f);
        }

        static void HpModel()
        {
            var go = new GameObject("SelfTest Stats");
            try
            {
                var stats = go.AddComponent<SwarmStats>();
                stats.Configure(100f, 1f, 2f, 2f);

                Near("starts at full HP", stats.Hp, 100f);
                True("invariants at start", stats.InvariantsHold());

                float lost = stats.TakeDamage(new DamageInfo(100f, DamageType.Contact, Faction.Enemy, Vector2.zero));
                Near("DEF 2 reduces a 100 hit to about 80", lost, 79.9f, 0.2f);
                Near("HP after the hit", stats.Hp, 20.1f, 0.2f);

                stats.TakeTrueDamage(5f);
                Near("true damage ignores DEF", stats.Hp, 15.1f, 0.2f);

                stats.SetHeldHp(50f);
                True("held HP is clamped to HP", stats.HeldHp <= stats.Hp + 0.001f);
                True("invariants after holding HP", stats.InvariantsHold());

                stats.SetHeldHp(0f);
                stats.Heal(1000f);
                Near("heal stops at max HP", stats.Hp, 100f);

                stats.AddToBase(StatType.MaxHp, 20f);
                Near("Expand: max HP +20", stats.MaxHp, 120f);
                stats.Heal(20f);
                Near("Expand: heals 20", stats.Hp, 120f);

                stats.TakeTrueDamage(1000f);
                True("dies at 0 HP", stats.IsDead);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
