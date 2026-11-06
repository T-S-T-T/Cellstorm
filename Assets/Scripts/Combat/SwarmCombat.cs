using Cellstorm.Core;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>
    /// Contact combat for one swarm. Each swarm handles only the damage IT deals: while touching a hostile
    /// swarm it deals ContactDps (HP per second, applied every physics tick). The receiver applies its own DEF.
    /// Same faction: nothing happens. There is no DEF comparison and no bounce any more; physical collisions stay.
    ///
    /// TEMPORARY (V0.1.1 Phase 1): the player's body deals TempPlayerContactDps to anything it touches so the game
    /// stays playable. Phase 2 replaces it with the Ram dash window and removes this.
    /// </summary>
    [RequireComponent(typeof(SwarmStats), typeof(Rigidbody2D))]
    public class SwarmCombat : MonoBehaviour
    {
        /// <summary>TEMPORARY (removed in Phase 2): HP per second the player's body deals by touching.</summary>
        public const float TempPlayerContactDps = 30f;
        public const float DefaultEnemyContactDps = 10f;

        [SerializeField] Faction faction = Faction.Player;
        [Tooltip("Strength of the visual dent when this swarm is hit (player swarm only).")]
        [SerializeField] float dentStrength = 4f;

        public Faction Faction => faction;
        public SwarmStats Stats { get; private set; }

        /// <summary>HP per second this swarm deals to a hostile swarm it touches (before the target's DEF).</summary>
        public float ContactDps { get; set; }

        Rigidbody2D _rb;
        SwarmVisual _visual;
        float _nextDentTime;

        void Awake()
        {
            Stats = GetComponent<SwarmStats>();
            _rb = GetComponent<Rigidbody2D>();
            _visual = GetComponent<SwarmVisual>();
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
            return f == Faction.Player ? TempPlayerContactDps : DefaultEnemyContactDps;
        }

        public void SetFaction(Faction f)
        {
            faction = f;
            ContactDps = DefaultDpsFor(f);
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
            if (_visual == null || Time.time < _nextDentTime) return;
            _nextDentTime = Time.time + 0.25f;
            _visual.Dent(worldPoint, dentStrength);
        }

        void OnDied()
        {
            if (faction == Faction.Player)
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
