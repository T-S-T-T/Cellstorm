using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Meta;
using Cellstorm.Swarm;
using Cellstorm.Weapons;
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
        /// <summary>Cells spawned by Jaw and Gun for the chosen loadout (100, Giant 200). Used from Phase 4.</summary>
        public static int WeaponCellCount { get; private set; } = 100;

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
            _active = MergeWithDefaults(loadouts);
            if (_player == null) return;

            if (skipPicker)
            {
                Apply(_active[0]);
                return;
            }
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.LoadoutSelect);
        }

        /// <summary>Scene assets from before V0.2 lack Swarmer and Sporer: add the built-in ones until the data is regenerated.</summary>
        static StatBlockSO[] MergeWithDefaults(StatBlockSO[] fromScene)
        {
            var list = new System.Collections.Generic.List<StatBlockSO>();
            if (fromScene != null)
                foreach (var b in fromScene)
                    if (b != null) list.Add(b);

            foreach (var d in LoadoutDefaults.CreateAll())
            {
                bool have = false;
                foreach (var b in list)
                    if (b.name == d.name) { have = true; break; }
                if (!have) list.Add(d);
            }
            // Order: Default first, then the defaults' order (Default, Speedy, Tank, Giant, Swarmer, Sporer).
            var order = new System.Collections.Generic.List<string>();
            foreach (var d in LoadoutDefaults.CreateAll()) order.Add(d.name);
            list.Sort((a, b) => order.IndexOf(a.name).CompareTo(order.IndexOf(b.name)));
            return list.ToArray();
        }

        void Apply(StatBlockSO block)
        {
            SelectedLoadout = block.name;
            WeaponCellCount = block.weaponCellCount;
            _player.Stats.Configure(block.maxHp, block.hpRegen, block.defense, block.speed);
            _player.SetSizeMultiplier(block.sizeMultiplier);

            // A loadout can change the starting weapon or skill (Swarmer: Minion on Q, Sporer: Spore Cloud on LMB).
            // These work for the run even if the item itself is not unlocked yet.
            var abilities = _player.GetComponent<AbilityController>();
            if (abilities != null)
            {
                var weapon = WeaponDefaults.ByKey(block.startWeaponKey);
                var skill = WeaponDefaults.ByKey(block.startSkillKey);
                if (weapon != null) abilities.Equip(0, weapon);
                if (skill != null) abilities.Equip(2, skill);
            }
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

            float cardW = 200f, cardH = 230f, gap = 16f;
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
                string stats = "HP " + b.maxHp + "   Regen " + b.hpRegen + "/s\nDEF " + b.defense + "   Speed " + b.speed;
                if (!Mathf.Approximately(b.sizeMultiplier, 1f)) stats += "\nBody x" + b.sizeMultiplier + "   Weapon cells " + b.weaponCellCount;
                if (!string.IsNullOrEmpty(b.startWeaponKey)) stats += "\nStarts with " + WeaponDefaults.ByKey(b.startWeaponKey).displayName;
                if (!string.IsNullOrEmpty(b.startSkillKey)) stats += "\nStarts with " + WeaponDefaults.ByKey(b.startSkillKey).displayName + " (skill)";
                string text = unlocked
                    ? stats
                    : "Locked\n" + UnlockCatalog.RequirementFor(unlockId) + "\n(you have " + lifetime + " lifetime score)";
                GUI.Label(new Rect(rect.x + 8, rect.y + 60, rect.width - 16, 100), text, _small);

                if (unlocked && GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    Apply(b);
                    gm.SetState(GameState.Playing);
                }
            }

            GUI.Label(new Rect(0, y + cardH + 20, sw, 30), "Every run starts with the Ram attack unless the loadout says otherwise.", _small);
        }
    }
}
