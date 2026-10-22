using Cellstorm.Combat;
using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Swarm;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Weapons
{
    /// <summary>How weapon cells look for this swarm (player: merged into the metaball blob).</summary>
    public struct CellStyle
    {
        public bool metaball;
        public int layer;
        public Color color;
        public Material material;
    }

    /// <summary>
    /// Holds the 4 slots (LMB, RMB weapons; Q, E skills) of a swarm and ticks them.
    /// The player's slots read the input actions; enemies are driven through SetAiHeld.
    /// </summary>
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(SwarmStats))]
    public class AbilityController : MonoBehaviour
    {
        public const int SlotCount = 4;
        /// <summary>World units per point of the Speed stat (same value the movement code uses).</summary>
        public const float UnitsPerSpeedPoint = 2.5f;
        public static readonly string[] SlotNames = { "LMB", "RMB", "Q", "E" };

        [Tooltip("Slots 0-1 weapons (LMB, RMB), slots 2-3 skills (Q, E).")]
        [SerializeField] WeaponSO[] startingLoadout = new WeaponSO[SlotCount];
        [Tooltip("Player only: put Ram on LMB if the loadout leaves it empty (GDD: every run starts with Ram).")]
        [SerializeField] bool equipRamIfEmpty = true;

        readonly Ability[] _slots = new Ability[SlotCount];
        readonly bool[] _aiHeld = new bool[SlotCount];
        readonly bool[] _aiPrev = new bool[SlotCount];

        SwarmStats _stats;
        SwarmCombat _combat;
        CircleCollider2D _col;
        IAbilityHost _host;
        bool _isPlayer;
        CellStyle _style;
        bool _styleBuilt;
        Material _material;

        public SwarmStats Stats => _stats;
        public IAbilityHost Host
        {
            get
            {
                if (_host == null) _host = GetComponent<IAbilityHost>();
                return _host;
            }
        }
        public Faction Faction => _combat != null ? _combat.Faction : (_isPlayer ? Faction.Player : Faction.Enemy);
        public Vector2 Position => transform.position;
        public float Radius => _col != null ? _col.radius : 0.5f;
        public float WeaponCellRadius => Mathf.Clamp(Radius * 0.25f, 0.12f, 0.45f);

        public CellStyle CellStyle
        {
            get
            {
                if (!_styleBuilt)
                {
                    _styleBuilt = true;
                    if (_material == null) _material = new Material(Shader.Find("Sprites/Default"));
                    _style = new CellStyle { color = Color.white, material = _material };

                    if (TryGetComponent<SwarmVisual>(out var sv))
                    {
                        _style.metaball = true;
                        _style.layer = sv.MetaballLayerIndex;
                        _style.color = sv.Color;
                    }
                    else if (TryGetComponent<CircleSwarmVisual>(out var cv))
                    {
                        _style.color = cv.Color;
                    }
                }
                return _style;
            }
        }

        public Ability GetSlot(int slot)
        {
            return slot >= 0 && slot < SlotCount ? _slots[slot] : null;
        }

        void Awake()
        {
            _stats = GetComponent<SwarmStats>();
            _combat = GetComponent<SwarmCombat>();
            _col = GetComponent<CircleCollider2D>();
            _isPlayer = GetComponent<SwarmBody>() != null;
            _stats.Died += CancelAll;
        }

        void Start()
        {
            if (Host == null)
            {
                Debug.LogError("AbilityController: no IAbilityHost (SwarmBody or Enemy) on " + name);
                enabled = false;
                return;
            }

            if (startingLoadout != null)
            {
                for (int i = 0; i < SlotCount && i < startingLoadout.Length; i++)
                    if (startingLoadout[i] != null) Equip(i, startingLoadout[i]);
            }

            if (_isPlayer && equipRamIfEmpty && _slots[0] == null)
                Equip(0, WeaponDefaults.Ram);
        }

        void OnDestroy()
        {
            if (_stats != null) _stats.Died -= CancelAll;
            CancelAll();
            if (_material != null) Destroy(_material);
        }

        /// <summary>Put a weapon or skill in a slot. A weapon already in another slot is moved here.</summary>
        public bool Equip(int slot, WeaponSO data)
        {
            if (slot < 0 || slot >= SlotCount || data == null) return false;

            bool weaponSlot = slot < 2;
            if ((data.kind == AbilityKind.Weapon) != weaponSlot) return false;

            for (int i = 0; i < SlotCount; i++)
            {
                if (i != slot && _slots[i] != null && _slots[i].Data.type == data.type) Unequip(i);
            }

            Unequip(slot);
            _slots[slot] = AbilityFactory.Create(this, data);
            return _slots[slot] != null;
        }

        public void Unequip(int slot)
        {
            if (slot < 0 || slot >= SlotCount) return;
            if (_slots[slot] != null) _slots[slot].Cancel();
            _slots[slot] = null;
        }

        /// <summary>
        /// Swap mode: move the ability in 'from' to 'to'. If 'to' is occupied the two trade places.
        /// Weapons only fit weapon slots (0-1) and skills only skill slots (2-3). Returns false if not allowed.
        /// Putting an ability back in its own slot is allowed.
        /// </summary>
        public bool SwapSlots(int from, int to)
        {
            if (from < 0 || from >= SlotCount || to < 0 || to >= SlotCount) return false;
            var a = _slots[from];
            if (a == null) return false;
            if (from == to) return true;

            bool toIsWeaponSlot = to < 2;
            if ((a.Data.kind == AbilityKind.Weapon) != toIsWeaponSlot) return false;

            var b = _slots[to];
            if (b != null && (b.Data.kind == AbilityKind.Weapon) != (from < 2)) return false;

            CancelIfShaping(a);
            CancelIfShaping(b);
            _slots[to] = a;
            _slots[from] = b;
            return true;
        }

        static void CancelIfShaping(Ability x)
        {
            if (x != null && (x.Phase == AbilityPhase.Shaping || x.Phase == AbilityPhase.Primed)) x.Cancel();
        }

        public void CancelAll()
        {
            for (int i = 0; i < SlotCount; i++)
                if (_slots[i] != null) _slots[i].Cancel();
            if (Host != null) Host.TurnRateMultiplier = 1f;
        }

        /// <summary>AI input for enemies: whether the slot's key is "held" this frame.</summary>
        public void SetAiHeld(int slot, bool held)
        {
            if (slot >= 0 && slot < SlotCount) _aiHeld[slot] = held;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (_stats.IsDead) return;

            float turn = 1f;
            for (int i = 0; i < SlotCount; i++)
            {
                var ability = _slots[i];
                var input = ReadInput(i);
                if (ability == null) continue;

                ability.Tick(dt, input);
                turn = Mathf.Min(turn, ability.TurnPenalty);
            }
            Host.TurnRateMultiplier = turn;
        }

        AbilityInput ReadInput(int slot)
        {
            if (_isPlayer)
            {
                InputAction a;
                switch (slot)
                {
                    case 0: a = InputService.Attack1; break;
                    case 1: a = InputService.Attack2; break;
                    case 2: a = InputService.Skill1; break;
                    default: a = InputService.Skill2; break;
                }
                return new AbilityInput
                {
                    pressed = a.WasPressedThisFrame(),
                    held = a.IsPressed(),
                    released = a.WasReleasedThisFrame()
                };
            }

            bool held = _aiHeld[slot];
            bool prev = _aiPrev[slot];
            _aiPrev[slot] = held;
            return new AbilityInput { pressed = held && !prev, held = held, released = !held && prev };
        }
    }
}
