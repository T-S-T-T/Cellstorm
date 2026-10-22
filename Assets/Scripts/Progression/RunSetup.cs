using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Meta;
using Cellstorm.Swarm;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Progression
{
    /// <summary>
    /// Start of a run: choose 1 starting swarm (stats only; every run starts with Ram), then play.
    /// Locked loadouts show their unlock score. Default is always available.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class RunSetup : MonoBehaviour
    {
        /// <summary>Name of the loadout chosen this run (leaderboard).</summary>
        public static string SelectedLoadout { get; private set; } = "Default";

        [SerializeField] StatBlockSO[] loadouts;
        [Tooltip("Testing: skip the picker and start with the first loadout.")]
        [SerializeField] bool skipPicker;

        SwarmBody _player;
        StatBlockSO[] _active;
        GUIStyle _title, _name, _small;

        public static RunSetup Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Balance bot: pick the loadout at 'index' (Default if it is locked).</summary>
        public bool BotChoose(int index)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.LoadoutSelect || _active == null || _player == null) return false;
            index = Mathf.Clamp(index, 0, _active.Length - 1);
            var b = _active[index];
            if (!UnlockState.IsUnlocked(UnlockIdFor(b))) b = _active[0];
            Apply(b);
            gm.SetState(GameState.Playing);
            return true;
        }

        void Start()
        {
            _player = FindAnyObjectByType<SwarmBody>();
            _active = loadouts != null && loadouts.Length > 0 ? loadouts : LoadoutDefaults.CreateAll();
            if (_player == null) return;

            if (skipPicker)
            {
                Apply(_active[0]);
                return;
            }
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.LoadoutSelect);
        }

        void Apply(StatBlockSO block)
        {
            SelectedLoadout = block.name;
            _player.Stats.Configure(block.maxCells, block.cellRegen, block.defense, block.speed);
        }

        static string UnlockIdFor(StatBlockSO b)
        {
            return b.name == "Default" ? null : "loadout_" + b.name.ToLower();
        }

        void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _name = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }

        void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.LoadoutSelect || _active == null) return;
            EnsureStyles();

            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float sw = Screen.width, sh = Screen.height;
            GUI.Label(new Rect(0, sh * 0.12f, sw, 50), "Choose your starting swarm", _title);

            float cardW = 230f, cardH = 210f, gap = 30f;
            float total = _active.Length * cardW + (_active.Length - 1) * gap;
            float x0 = (sw - total) * 0.5f;
            float y = sh * 0.30f;
            int lifetime = SaveService.Data.totalScore;

            for (int i = 0; i < _active.Length; i++)
            {
                var b = _active[i];
                var rect = new Rect(x0 + i * (cardW + gap), y, cardW, cardH);
                string unlockId = UnlockIdFor(b);
                bool unlocked = UnlockState.IsUnlocked(unlockId);
                bool hover = rect.Contains(Event.current.mousePosition);

                Color c = unlocked ? new Color(0.25f, 0.55f, 0.45f) : new Color(0.3f, 0.3f, 0.3f);
                GUI.color = hover && unlocked ? Color.Lerp(c, Color.white, 0.25f) : c;
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                GUI.Label(new Rect(rect.x, rect.y + 12, rect.width, 36), b.name, _name);
                string stats = "Cells " + b.maxCells + "   Regen " + b.cellRegen + "\nDEF " + b.defense + "   Speed " + b.speed;
                string text = unlocked
                    ? stats
                    : "Locked\nUnlocks at " + UnlockCatalog.ThresholdFor(unlockId) + " lifetime score\n(you have " + lifetime + ")";
                GUI.Label(new Rect(rect.x + 8, rect.y + 60, rect.width - 16, 100), text, _small);

                if (unlocked && GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    Apply(b);
                    gm.SetState(GameState.Playing);
                }
            }

            GUI.Label(new Rect(0, y + cardH + 20, sw, 30), "Every run starts with the Ram attack.", _small);
        }
    }
}
