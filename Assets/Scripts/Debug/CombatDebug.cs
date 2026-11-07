using Cellstorm.Combat;
using Cellstorm.Swarm;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// TEMPORARY test harness: spawns debug enemies, tweaks player stats, and shows the numbers.
    /// Z single (10 HP)   X multi (100 HP)   N real ranged   M real exploder   C brute (500 HP, DEF 3)   V mirror (100 HP, DEF equal to yours)
    /// B toggle chase  K clear enemies   H heal   , and . base DEF -1/+1   T "hardening" x2 DEF for 3s
    /// U take a 20 damage test hit (shows what your DEF does)   R restart the scene
    /// </summary>
    public class CombatDebug : MonoBehaviour
    {
        [SerializeField] SwarmBody player;

        bool _chase = true;
        float _nextScan;
        SwarmStats _nearest;
        string _nearestName = "-";
        float _lastDamage;
        float _lastDamageTime = -99f;
        float _totalTaken;
        GUIStyle _bigStyle;

        void Start()
        {
            if (player == null) player = FindAnyObjectByType<SwarmBody>();
            if (player != null) player.Stats.Damaged += OnPlayerDamaged;
        }

        void OnDestroy()
        {
            if (player != null && player.Stats != null) player.Stats.Damaged -= OnPlayerDamaged;
        }

        void OnPlayerDamaged(float amount)
        {
            _lastDamage = amount;
            _lastDamageTime = Time.time;
            _totalTaken += amount;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || player == null) return;

            var stats = player.Stats;

            if (kb.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }

            if (kb.zKey.wasPressedThisFrame) Spawn("Single", 10f, 1f, 1f, new Color(0.95f, 0.35f, 0.35f));
            if (kb.xKey.wasPressedThisFrame) Spawn("Multi", 100f, 1f, 1f, new Color(0.95f, 0.60f, 0.30f));
            if (kb.nKey.wasPressedThisFrame) SpawnReal(2);   // real Ranged Cell with its gun
            if (kb.mKey.wasPressedThisFrame) SpawnReal(3);   // real Exploding Cell
            if (kb.cKey.wasPressedThisFrame) Spawn("Brute", 500f, 3f, 1f, new Color(0.70f, 0.20f, 0.80f));
            if (kb.vKey.wasPressedThisFrame) Spawn("Mirror", 100f, stats.Defense, 1f, new Color(0.35f, 0.60f, 0.95f));
            if (kb.uKey.wasPressedThisFrame) stats.TakeDamage(20f, DamageType.Contact);

            if (kb.bKey.wasPressedThisFrame)
            {
                _chase = !_chase;
                foreach (var b in FindObjectsByType<DebugEnemyBrain>()) b.Chase = _chase;
            }

            if (kb.kKey.wasPressedThisFrame)
            {
                foreach (var b in FindObjectsByType<DebugEnemyBrain>()) Destroy(b.gameObject);
            }

            if (kb.hKey.wasPressedThisFrame) stats.SetHp(stats.MaxHp);
            if (kb.commaKey.wasPressedThisFrame) stats.AddToBase(StatType.Defense, -1f);
            if (kb.periodKey.wasPressedThisFrame) stats.AddToBase(StatType.Defense, 1f);
            if (kb.tKey.wasPressedThisFrame) stats.AddModifier(StatType.Defense, ModifierOp.Multiply, 2f, 3f, this);

            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.25f;
                FindNearestEnemy();
            }
        }

        void SpawnReal(int typeIndex)
        {
            var types = Cellstorm.Data.EnemyDefaults.CreateTypes();
            var spec = Cellstorm.Enemies.EnemySpec.FromType(types[typeIndex], 1);
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            Cellstorm.Enemies.Enemy.Create(spec, (Vector2)player.transform.position + dir * 8f);
        }

        void Spawn(string label, float hp, float def, float speed, Color color)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            Vector2 pos = (Vector2)player.transform.position + dir * (player.SmoothedRadius + 4f);

            var go = new GameObject("Debug " + label);
            go.transform.position = pos;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            go.AddComponent<CircleCollider2D>();

            var stats = go.AddComponent<SwarmStats>();
            stats.Configure(hp, 0f, def, speed);

            go.AddComponent<SwarmCombat>().SetFaction(Faction.Enemy);

            var visual = go.AddComponent<CircleSwarmVisual>();
            visual.SetColor(color);

            go.AddComponent<DebugEnemyBrain>().Chase = _chase;
        }

        void FindNearestEnemy()
        {
            _nearest = null;
            _nearestName = "-";
            float best = float.MaxValue;
            foreach (var b in FindObjectsByType<DebugEnemyBrain>())
            {
                float d = ((Vector2)b.transform.position - (Vector2)player.transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    _nearest = b.GetComponent<SwarmStats>();
                    _nearestName = b.name.Replace("Debug ", "");
                }
            }
        }

        void OnGUI()
        {
            if (!DebugVisibility.Show) return;
            if (player == null) return;
            var s = player.Stats;

            string line1 = $"PLAYER  HP {s.Hp:F1}/{s.MaxHp:F0}   Regen {s.HpRegen:F1}/s   " +
                           $"DEF {s.Defense:F1} (base {s.BaseDefense:F1}, takes {DefCurve.Reduction(s.Defense) * 100f:F0}% less)   " +
                           $"Speed {s.Speed:F1} ({SpeedMath.MoveSpeed(s.Speed):F1} u/s)   Modifiers {s.ModifierCount}   Invariants {(s.InvariantsHold() ? "ok" : "BROKEN")}";

            string line2 = "Nearest enemy: none";
            if (_nearest != null)
            {
                float eToP = DefCurve.Apply(SwarmCombat.DefaultEnemyContactDps, s.Defense);
                float ram = DefCurve.Apply(30f, _nearest.Defense);
                line2 = $"Nearest: {_nearestName}  HP {_nearest.Hp:F0}/{_nearest.MaxHp:F0}  DEF {_nearest.Defense:F1}   " +
                        $"your Ram hit {ram:F1}   its contact -> you {eToP:F1} HP/s";
            }

            float ago = Time.time - _lastDamageTime;
            string line3 = $"Last damage tick: {_lastDamage:F2} ({(ago < 90f ? ago.ToString("F1") + "s ago" : "never")})   Total taken: {_totalTaken:F1}";

            GUI.Label(new Rect(10, 85, 1100, 25), line1);
            GUI.Label(new Rect(10, 105, 1100, 25), line2);
            GUI.Label(new Rect(10, 125, 1100, 25), line3);
            GUI.Label(new Rect(10, 145, 1100, 25),
                "Z single  X multi  N ranged(real)  M exploder(real)  C brute(DEF3)  V mirror(same DEF)  B chase on/off  K clear  H heal  , . DEF -/+  T DEF x2 for 3s  U 20 dmg test hit  R restart");

            if (s.IsDead)
            {
                if (_bigStyle == null)
                {
                    _bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
                }
                GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 60), "YOU DIED - press R to restart", _bigStyle);
            }
        }
    }
}
