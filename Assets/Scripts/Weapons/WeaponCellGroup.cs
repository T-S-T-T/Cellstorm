using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// The pooled weapon cells of one deployed HP-cost weapon (Jaw or Gun). GDD V0.1.1 section 4.5.
    ///
    /// - N cells (100, Giant 200, never tied to HP or body size) appear inside the body and fly to their slots.
    /// - Together they hold the weapon's cost: every cell holds cost / N HP. SwarmStats.HeldHp is the sum.
    /// - Cells are damageable (DEF of the owner applies). Damage lowers the cell's HP and the owner's Hp.
    ///   A dead cell is replaced from the body, or the weapon is released if the body cannot pay.
    /// - Cells have no colliders: hostile sources (contact, bullets, bites, rams, explosions) use the static
    ///   queries below, which keeps 100 to 200 cells per weapon cheap.
    /// - Only one group per ability exists. Starting again clears the old cells first, so spamming the button
    ///   can never create extra cells.
    /// </summary>
    public class WeaponCellGroup
    {
        // ---------------- Registry of hittable groups ----------------

        public static readonly List<WeaponCellGroup> Active = new List<WeaponCellGroup>();
        static readonly List<WeaponCellGroup> Scratch = new List<WeaponCellGroup>();

        /// <summary>Total live cell objects (deployed and returning). Used by the self tests and the perf monitor.</summary>
        public static int TotalCells { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active.Clear();
            Scratch.Clear();
            SoftPool.Clear();
            HardPool.Clear();
            TotalCells = 0;
        }

        /// <summary>Is any hostile (to this faction) deployed group around? Cheap guard for contact checks.</summary>
        public static bool AnyHostileTo(Faction f)
        {
            for (int i = 0; i < Active.Count; i++)
                if (Active[i].Faction != f) return true;
            return false;
        }

        /// <summary>Damages every hostile cell inside a circle. Returns how many cells died.</summary>
        public static int DamageCircle(Vector2 center, float radius, float raw, Faction attacker)
        {
            if (Active.Count == 0) return 0;
            Snapshot();
            int killed = 0;
            for (int g = 0; g < Scratch.Count; g++)
            {
                var group = Scratch[g];
                if (group.Faction == attacker || !group._deployed) continue;
                if ((group._boundsCenter - center).sqrMagnitude > Sq(group._boundsRadius + radius)) continue;
                killed += group.HitCircle(center, radius, raw, attacker);
            }
            Scratch.Clear();
            return killed;
        }

        /// <summary>Damages the first hostile cell touching a small circle (bullets). True if one was hit.</summary>
        public static bool DamageFirstTouching(Vector2 center, float radius, float raw, Faction attacker)
        {
            if (Active.Count == 0) return false;
            Snapshot();
            bool hit = false;
            for (int g = 0; g < Scratch.Count && !hit; g++)
            {
                var group = Scratch[g];
                if (group.Faction == attacker || !group._deployed) continue;
                if ((group._boundsCenter - center).sqrMagnitude > Sq(group._boundsRadius + radius)) continue;
                hit = group.HitFirst(center, radius, raw, attacker);
            }
            Scratch.Clear();
            return hit;
        }

        /// <summary>Damages every hostile cell inside a cone (Jaw bite). Returns how many cells died.</summary>
        public static int DamageCone(Vector2 origin, Vector2 aim, float halfAngle, float reach, float raw, Faction attacker)
        {
            if (Active.Count == 0) return 0;
            Snapshot();
            int killed = 0;
            for (int g = 0; g < Scratch.Count; g++)
            {
                var group = Scratch[g];
                if (group.Faction == attacker || !group._deployed) continue;
                if ((group._boundsCenter - origin).sqrMagnitude > Sq(group._boundsRadius + reach)) continue;
                killed += group.HitCone(origin, aim, halfAngle, reach, raw, attacker);
            }
            Scratch.Clear();
            return killed;
        }

        static void Snapshot()
        {
            Scratch.Clear();
            Scratch.AddRange(Active);
        }

        static float Sq(float v) { return v * v; }

        // ---------------- Cell pool ----------------

        class Cell
        {
            public Transform t;
            public SpriteRenderer sr;
            public bool soft;
            public Vector2 pos;
            public float hp;
        }

        static readonly Stack<Cell> SoftPool = new Stack<Cell>();
        static readonly Stack<Cell> HardPool = new Stack<Cell>();
        const float MetaballOverlapCompensation = 0.62f;

        static Cell Rent(bool soft, int layer, Material material)
        {
            var pool = soft ? SoftPool : HardPool;
            Cell c = null;
            while (pool.Count > 0 && c == null)
            {
                var p = pool.Pop();
                if (p.t != null) c = p;
            }
            if (c == null)
            {
                var go = new GameObject("WeaponCell");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = soft ? ProceduralSprites.Soft : ProceduralSprites.Hard;
                sr.sortingOrder = soft ? 0 : 6;
                c = new Cell { t = go.transform, sr = sr, soft = soft };
            }
            c.t.gameObject.layer = soft ? layer : 0;
            c.sr.sharedMaterial = material;
            c.t.gameObject.SetActive(true);
            TotalCells++;
            return c;
        }

        static void Return(Cell c)
        {
            if (c == null || c.t == null) return;
            c.t.gameObject.SetActive(false);
            (c.soft ? SoftPool : HardPool).Push(c);
            TotalCells--;
        }

        // ---------------- Instance ----------------

        readonly ShapedAbility _ability;
        readonly AbilityController _owner;
        readonly List<Cell> _cells = new List<Cell>();

        bool _deployed;      // hittable and holding HP
        bool _returning;     // flying back, not hittable
        float _returnTimer;
        float _perCell;
        float _heldSum;
        float _cellRadius;
        Vector2 _boundsCenter;
        float _boundsRadius;

        public WeaponCellGroup(ShapedAbility ability, AbilityController owner)
        {
            _ability = ability;
            _owner = owner;
        }

        public Faction Faction => _owner.Faction;
        public int Count => _cells.Count;
        public bool Deployed => _deployed;
        public bool Returning => _returning;
        public float HeldSum => _heldSum;
        public float PerCell => _perCell;

        /// <summary>Sum of the cells' own HP (must equal HeldSum; checked by the self tests).</summary>
        public float ComputeCellHpSum()
        {
            float s = 0f;
            if (!_deployed) return 0f;
            for (int i = 0; i < _cells.Count; i++) s += _cells[i].hp;
            return s;
        }

        /// <summary>Spawn 'count' cells inside the body, holding 'cost' HP in total.</summary>
        public void Begin(int count, float cost, float cellRadius)
        {
            ClearImmediate();

            count = Mathf.Max(1, count);
            _cellRadius = cellRadius;
            _perCell = cost / count;
            _deployed = true;
            _returning = false;

            var style = _owner.CellStyle;
            Vector2 center = _owner.Position;
            float spawnR = Mathf.Max(0.05f, _owner.Radius * 0.6f);

            for (int i = 0; i < count; i++)
            {
                var c = Rent(style.metaball, style.layer, style.material);
                c.sr.color = style.color;
                c.hp = _perCell;
                c.pos = center + Random.insideUnitCircle * spawnR;
                c.t.position = new Vector3(c.pos.x, c.pos.y, 0f);
                _cells.Add(c);
            }

            _heldSum = cost;
            _owner.Stats.SetHeldHp(_heldSum);
            _boundsCenter = center;
            _boundsRadius = _owner.Radius + cellRadius;
            if (!Active.Contains(this)) Active.Add(this);
        }

        /// <summary>
        /// Move the cells. 'ease' 0..1 is how far along the formation they are, 'pulse' makes primed cells throb.
        /// </summary>
        public void UpdateCells(float dt, float ease, bool primed, float cellSpeed)
        {
            if (_cells.Count == 0) return;

            Vector2 center = _owner.Position;
            int n = _cells.Count;
            float follow = 1f - Mathf.Exp(-16f * dt);
            float pulse = primed ? 1f + 0.12f * Mathf.Sin(Time.time * 14f) : 1f;
            bool soft = _cells[0].soft;
            float diameter = 2f * _cellRadius * pulse *
                             (soft ? MetaballOverlapCompensation / ProceduralSprites.SoftVisibleRadiusFactor : 1f);
            float maxDist = 0f;

            for (int i = 0; i < n; i++)
            {
                var c = _cells[i];
                if (_returning)
                {
                    c.pos = Vector2.MoveTowards(c.pos, center, cellSpeed * dt);
                }
                else
                {
                    Vector2 formation = _ability.GetFormationPoint(i, n);
                    Vector2 target = Vector2.Lerp(center, formation, ease);
                    c.pos = Vector2.Lerp(c.pos, target, follow);
                }
                c.t.position = new Vector3(c.pos.x, c.pos.y, 0f);
                c.t.localScale = new Vector3(diameter, diameter, 1f);

                float d = (c.pos - center).sqrMagnitude;
                if (d > maxDist) maxDist = d;
            }

            _boundsCenter = center;
            _boundsRadius = Mathf.Sqrt(maxDist) + _cellRadius;

            if (_returning)
            {
                _returnTimer -= dt;
                if (_returnTimer <= 0f) ClearImmediate();
            }
        }

        /// <summary>The weapon finished or was cancelled: the cells fly back, the HP stays in the body.</summary>
        public void Release()
        {
            if (!_deployed && !_returning) return;
            _deployed = false;
            _returning = true;
            _returnTimer = 0.6f;
            _heldSum = 0f;
            Active.Remove(this);
            _owner.Stats.SetHeldHp(0f);
        }

        /// <summary>Despawn everything at once (death, re-deploy, unequip).</summary>
        public void ClearImmediate()
        {
            for (int i = 0; i < _cells.Count; i++) Return(_cells[i]);
            _cells.Clear();
            bool had = _deployed;
            _deployed = false;
            _returning = false;
            _heldSum = 0f;
            Active.Remove(this);
            if (had && _owner != null && _owner.Stats != null) _owner.Stats.SetHeldHp(0f);
        }

        /// <summary>Drop cells that are only flying back (a new weapon starts, so extra cells vanish).</summary>
        public void PurgeReturning()
        {
            if (_returning) ClearImmediate();
        }

        // ---------------- Damage ----------------

        int HitCircle(Vector2 center, float radius, float raw, Faction attacker)
        {
            int killed = 0;
            float reach = radius + _cellRadius;
            float reachSq = reach * reach;
            for (int i = 0; i < _cells.Count && _deployed; i++)
            {
                var c = _cells[i];
                if ((c.pos - center).sqrMagnitude > reachSq) continue;
                if (Damage(c, raw, attacker)) killed++;
            }
            return killed;
        }

        bool HitFirst(Vector2 center, float radius, float raw, Faction attacker)
        {
            float reach = radius + _cellRadius;
            float reachSq = reach * reach;
            for (int i = 0; i < _cells.Count && _deployed; i++)
            {
                var c = _cells[i];
                if ((c.pos - center).sqrMagnitude > reachSq) continue;
                Damage(c, raw, attacker);
                return true;
            }
            return false;
        }

        int HitCone(Vector2 origin, Vector2 aim, float halfAngle, float reach, float raw, Faction attacker)
        {
            int killed = 0;
            float reachSq = reach * reach;
            for (int i = 0; i < _cells.Count && _deployed; i++)
            {
                var c = _cells[i];
                Vector2 to = c.pos - origin;
                if (to.sqrMagnitude > reachSq) continue;
                if (to.sqrMagnitude > 0.0001f && Vector2.Angle(aim, to) > halfAngle) continue;
                if (Damage(c, raw, attacker)) killed++;
            }
            return killed;
        }

        /// <summary>One cell takes a hit. Returns true if it died.</summary>
        bool Damage(Cell c, float raw, Faction attacker)
        {
            if (!_deployed || raw <= 0f) return false;
            var stats = _owner.Stats;
            if (stats.IsDead) return false;

            float final = DefCurve.Apply(raw, stats.Defense);
            float lost = Mathf.Min(final, c.hp);
            if (lost <= 0f) return false;

            c.hp -= lost;
            _heldSum = Mathf.Max(0f, _heldSum - lost);
            stats.TakeTrueDamage(lost);          // the cell's HP was part of the owner's Hp
            if (stats.IsDead) return true;
            stats.SetHeldHp(_heldSum);

            if (c.hp > 0.00001f) return false;

            // Cell died: replace it from the body, reset the deploy progress, or give up.
            _ability.NotifyCellLost();
            if (!_deployed) return true;

            float free = stats.Hp - _heldSum;
            if (free >= _perCell)
            {
                c.hp = _perCell;
                c.pos = _owner.Position;
                _heldSum += _perCell;
                stats.SetHeldHp(_heldSum);
            }
            else
            {
                _ability.NotifyCannotPay();
            }
            return true;
        }
    }
}
