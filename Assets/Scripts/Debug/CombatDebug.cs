using Cellstorm.Combat;
using Cellstorm.Swarm;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// TEMPORARY test harness for Phase 2: spawns debug enemies, tweaks player stats, and shows the numbers.
    /// Z single cell   X multi cell   C brute (DEF 3)   V mirror (DEF equal to yours)
    /// B toggle chase  K clear enemies   H heal   , and . base DEF -1/+1   T "hardening" x2 DEF for 3s
    /// R restart the scene
    /// </summary>
    public class CombatDebug : MonoBehaviour
    {
        [SerializeField] SwarmBody player;

        bool _chase = true;
        float _nextScan;
        SwarmStats _nearest;
        string _nearestName = "-";
        int _lastDamage;
        float _lastDamageTime = -99f;
        int _totalTaken;
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

        void OnPlayerDamaged(int amount)
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

            if (kb.zKey.wasPressedThisFrame) Spawn("Single", 1, 0f, 1f, 1f, new Color(0.95f, 0.35f, 0.35f));
            if (kb.xKey.wasPressedThisFrame) Spawn("Multi", 10, 1f, 1f, 1f, new Color(0.95f, 0.60f, 0.30f));
            if (kb.cKey.wasPressedThisFrame) Spawn("Brute", 30, 1f, 3f, 1f, new Color(0.70f, 0.20f, 0.80f));
            if (kb.vKey.wasPressedThisFrame) Spawn("Mirror", 5, 1f, stats.Defense, 1f, new Color(0.35f, 0.60f, 0.95f));

            if (kb.bKey.wasPressedThisFrame)
            {
                _chase = !_chase;
                foreach (var b in FindObjectsByType<DebugEnemyBrain>()) b.Chase = _chase;
            }

            if (kb.kKey.wasPressedThisFrame)
            {
                foreach (var b in FindObjectsByType<DebugEnemyBrain>()) Destroy(b.gameObject);
            }

            if (kb.hKey.wasPressedThisFrame) stats.SetCells(stats.MaxCells);
            if (kb.commaKey.wasPressedThisFrame) stats.AddToBase(StatType.Defense, -1f);
            if (kb.periodKey.wasPressedThisFrame) stats.AddToBase(StatType.Defense, 1f);
            if (kb.tKey.wasPressedThisFrame) stats.AddModifier(StatType.Defense, ModifierOp.Multiply, 2f, 3f, this);

            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.25f;
                FindNearestEnemy();
            }
        }

        void Spawn(string label, int cells, float regen, float def, float speed, Color color)
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
            stats.Configure(cells, regen, def, speed);

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

            string line1 = $"PLAYER  Cells {s.Cells}/{s.MaxCells}   Regen {s.CellRegen:F1}/s   " +
                           $"DEF {s.Defense:F1} (base {s.BaseDefense:F1})   Speed {s.Speed:F1}   Modifiers {s.ModifierCount}";

            string line2 = "Nearest enemy: none";
            if (_nearest != null)
            {
                float pToE = DamageCalculator.ContactDps(s.Cells, s.Defense, _nearest.Cells, _nearest.Defense);
                float eToP = DamageCalculator.ContactDps(_nearest.Cells, _nearest.Defense, s.Cells, s.Defense);
                string rel = Mathf.Abs(s.Defense - _nearest.Defense) < 0.001f ? "equal DEF -> bounce" : "";
                line2 = $"Nearest: {_nearestName}  Cells {_nearest.Cells}/{_nearest.MaxCells}  DEF {_nearest.Defense:F1}   " +
                        $"you -> it {pToE:F0} cells/s   it -> you {eToP:F0} cells/s   {rel}";
            }

            float ago = Time.time - _lastDamageTime;
            string line3 = $"Last damage taken: {_lastDamage} ({(ago < 90f ? ago.ToString("F1") + "s ago" : "never")})   Total taken: {_totalTaken}";

            GUI.Label(new Rect(10, 85, 1100, 25), line1);
            GUI.Label(new Rect(10, 105, 1100, 25), line2);
            GUI.Label(new Rect(10, 125, 1100, 25), line3);
            GUI.Label(new Rect(10, 145, 1100, 25),
                "Z single  X multi  C brute(DEF3)  V mirror(equal DEF)  B chase on/off  K clear  H heal  , . DEF -/+  T DEF x2 for 3s  R restart");

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
