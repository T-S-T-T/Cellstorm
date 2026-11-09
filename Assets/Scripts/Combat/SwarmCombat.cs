using Cellstorm.Core;
using Cellstorm.Swarm;
using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>
    /// Contact combat for one swarm. Each swarm handles only the damage IT deals: while touching a hostile
    /// swarm it deals ContactDps (HP per second, applied every physics tick). The receiver applies its own DEF.
    /// Same faction: nothing happens. There is no DEF comparison and no bounce any more; physical collisions stay.
    /// The player's body deals no contact damage: the player hurts enemies with Ram, Jaw, Gun and (V0.2) skills.
    /// Enemy contact also damages the player's deployed weapon cells (they have no colliders, so it is a cell query).
    /// </summary>
    [RequireComponent(typeof(SwarmStats), typeof(Rigidbody2D))]
    public class SwarmCombat : MonoBehaviour
    {
        public const float DefaultEnemyContactDps = 10f;

        [SerializeField] Faction faction = Faction.Player;
        [Tooltip("Strength of the visual dent when this swarm is hit (player swarm only).")]
        [SerializeField] float dentStrength = 4f;

        public Faction Faction => faction;
        public SwarmStats Stats { get; private set; }

        /// <summary>V0.2: a minion is a player-faction swarm whose death does not end the run.</summary>
        public bool IsMinion { get; set; }

        /// <summary>HP per second this swarm deals to a hostile swarm it touches (before the target's DEF).</summary>
        public float ContactDps { get; set; }

        Rigidbody2D _rb;
        CircleCollider2D _col;
        IHitVisual _visual;
        float _nextDentTime;

        void Awake()
        {
            Stats = GetComponent<SwarmStats>();
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CircleCollider2D>();
            _visual = GetComponent<IHitVisual>();
            ContactDps = DefaultDpsFor(faction);

            // OnCollisionStay2D is not called for sleeping bodies, which would stop contact damage
            // whenever two swarms stand still against each other.
            _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            Stats.Died += OnDied;
        }

        void OnDestroy()
        {
            if (Stats != null) Stats.Died -= OnDied;
        }

        static float DefaultDpsFor(Faction f)
        {
            return f == Faction.Player ? 0f : DefaultEnemyContactDps;
        }

        public void SetFaction(Faction f)
        {
            faction = f;
            ContactDps = DefaultDpsFor(f);
        }

        void FixedUpdate()
        {
            if (ContactDps <= 0f || Stats.IsDead) return;
            if (!WeaponCellGroup.AnyHostileTo(faction)) return;
            float r = _col != null ? _col.radius : 0.5f;
            WeaponCellGroup.DamageCircle(transform.position, r, ContactDps * Time.fixedDeltaTime, faction);
        }

        float _nextSquashTime;

        /// <summary>Soft-ball feel: any bump (swarm against swarm, or a hard shove) squashes the ball toward the contact.</summary>
        void OnCollisionEnter2D(Collision2D c)
        {
            SquashFrom(c, 1.6f);
        }

        void SquashFrom(Collision2D c, float gain)
        {
            if (_visual == null) _visual = GetComponent<IHitVisual>();     // enemies add their visual after this component
            if (_visual == null || Stats == null || Stats.IsDead || Time.time < _nextSquashTime) return;
            float speed = c.relativeVelocity.magnitude;
            if (speed < 0.6f) return;
            _nextSquashTime = Time.time + 0.12f;
            _visual.Hit(c.GetContact(0).point, Mathf.Clamp(speed * gain, 1.5f, 6f));
        }

        void OnCollisionStay2D(Collision2D c)
        {
            if (!TryGetOpponent(c, out var other)) return;
            if (Stats.IsDead || ContactDps <= 0f) return;

            Vector2 point = c.GetContact(0).point;
            var hit = new DamageInfo(ContactDps * Time.fixedDeltaTime, DamageType.Contact, faction, point);
            other.Stats.TakeDamage(hit);
            other.ReactToHit(point);
        }

        bool TryGetOpponent(Collision2D c, out SwarmCombat other)
        {
            if (c.gameObject.TryGetComponent<SwarmCombat>(out other)
                && other != this
                && other.faction != faction
                && !other.Stats.IsDead)
            {
                return true;
            }
            other = null;
            return false;
        }

        public void ReactToHit(Vector2 worldPoint)
        {
            if (_visual == null) _visual = GetComponent<IHitVisual>();
            if (_visual == null || Time.time < _nextDentTime) return;
            _nextDentTime = Time.time + 0.25f;
            _visual.Hit(worldPoint, dentStrength);
        }

        void OnDied()
        {
            if (faction == Faction.Player && !IsMinion)
            {
                if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Dead);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
