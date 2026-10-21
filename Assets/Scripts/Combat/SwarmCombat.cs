using Cellstorm.Core;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>
    /// Contact combat for one swarm (GDD section 4.2). Each swarm handles only the damage IT deals:
    /// while touching an opponent with lower DEF it removes cells from it every physics step.
    /// Equal DEF: both bounce apart. Same faction: nothing happens.
    /// </summary>
    [RequireComponent(typeof(SwarmStats), typeof(Rigidbody2D))]
    public class SwarmCombat : MonoBehaviour
    {
        [SerializeField] Faction faction = Faction.Player;
        [Tooltip("Speed given to each swarm when two equal-DEF swarms collide (units/s).")]
        [SerializeField] float bounceSpeed = 6f;
        [Tooltip("Strength of the visual dent when this swarm is hit (player swarm only).")]
        [SerializeField] float dentStrength = 4f;

        public Faction Faction => faction;
        public SwarmStats Stats { get; private set; }

        Rigidbody2D _rb;
        IKnockbackable _knockbackable;
        SwarmVisual _visual;
        float _nextDentTime;

        void Awake()
        {
            Stats = GetComponent<SwarmStats>();
            _rb = GetComponent<Rigidbody2D>();
            _knockbackable = GetComponent<IKnockbackable>();
            _visual = GetComponent<SwarmVisual>();

            // OnCollisionStay2D is not called for sleeping bodies, which would stop contact damage
            // whenever two swarms stand still against each other.
            _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            Stats.Died += OnDied;
        }

        void OnDestroy()
        {
            if (Stats != null) Stats.Died -= OnDied;
        }

        public void SetFaction(Faction f)
        {
            faction = f;
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (!TryGetOpponent(c, out var other)) return;

            if (Mathf.Abs(Stats.Defense - other.Stats.Defense) < 0.001f)
            {
                Vector2 away = (Vector2)transform.position - (Vector2)other.transform.position;
                if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle;
                Knockback(away.normalized * bounceSpeed);
            }
        }

        void OnCollisionStay2D(Collision2D c)
        {
            if (!TryGetOpponent(c, out var other)) return;
            if (Stats.IsDead) return;

            float dps = DamageCalculator.ContactDps(
                Stats.Cells, Stats.Defense, other.Stats.Cells, other.Stats.Defense);
            if (dps <= 0f) return;

            other.Stats.ApplyDamageOverTime(dps, Time.fixedDeltaTime);
            other.ReactToHit(c.GetContact(0).point);
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

        void Knockback(Vector2 velocity)
        {
            if (_knockbackable != null) _knockbackable.AddKnockback(velocity);
            else _rb.linearVelocity += velocity;
        }

        void ReactToHit(Vector2 worldPoint)
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
                // Phase 3: drop EXP, add score, spawn effects.
                Destroy(gameObject);
            }
        }
    }
}
