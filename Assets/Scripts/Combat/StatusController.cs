using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.Combat
{
    /// <summary>
    /// Status effects of one swarm (player, enemy or minion), GDD V0.2 section 4.4:
    ///  - Toxic: damage over time. One at a time: a stronger one replaces the current one, an equal or weaker one only
    ///    refreshes the duration. Every tick is reduced by DEF (it goes through SwarmStats.TakeDamage).
    ///  - Slow: moving speed reduced by a fraction, never below 20% of normal. The strongest applies, a new hit refreshes.
    ///  - Stun: cannot move or use weapons. A deployed cell weapon is released. After it ends, 1 second of stun immunity.
    ///  - Knockback / pull: an instant shove (0.2 s), not reduced by DEF.
    /// Bosses: stun and slow last half as long, shoves are half as far. Timers use scaled time (Update with
    /// Time.deltaTime), so pause and swap mode freeze them. Effects never touch weapon cells.
    /// </summary>
    [RequireComponent(typeof(SwarmStats))]
    public class StatusController : MonoBehaviour
    {
        public const float MinSpeedFactor = 0.2f;
        public const float StunImmunitySeconds = 1f;
        public const float BossScale = 0.5f;
        public const float ShoveSeconds = 0.2f;

        SwarmStats _stats;
        IKnockbackable _host;
        AbilityController _abilities;

        public bool IsBoss { get; set; }

        // Toxic
        public float ToxicDps { get; private set; }
        public float ToxicRemaining { get; private set; }
        public bool ToxicHazard { get; private set; }
        public Faction ToxicSource { get; private set; }
        public bool ToxicActive => ToxicRemaining > 0f && ToxicDps > 0f;

        // Slow
        public float SlowPct { get; private set; }
        public float SlowRemaining { get; private set; }
        public bool SlowActive => SlowRemaining > 0f && SlowPct > 0f;

        // Stun
        public float StunRemaining { get; private set; }
        public float ImmunityRemaining { get; private set; }
        public bool Stunned => StunRemaining > 0f;

        float _toxicTotal = 1f, _slowTotal = 1f, _stunTotal = 1f;

        /// <summary>Remaining share (1 to 0) of each effect, for the HUD timer rings.</summary>
        public float ToxicFraction => ToxicActive ? Mathf.Clamp01(ToxicRemaining / _toxicTotal) : 0f;
        public float SlowFraction => SlowActive ? Mathf.Clamp01(SlowRemaining / _slowTotal) : 0f;
        public float StunFraction => Stunned ? Mathf.Clamp01(StunRemaining / _stunTotal) : 0f;

        // Hazard current (pushed every few ticks by the hazard patch)
        Vector2 _external;
        float _externalTimer;

        /// <summary>Raised when a stun starts (abilities release their cells).</summary>
        public event System.Action Stunned_;

        /// <summary>Extra velocity from a hazard current while one is acting on this swarm.</summary>
        public Vector2 External => _externalTimer > 0f ? _external : Vector2.zero;

        /// <summary>Multiplier on moving speed: 0 while stunned, otherwise 1 - slow, never below 0.2.</summary>
        public float SpeedFactor
        {
            get
            {
                if (Stunned) return 0f;
                return SlowActive ? Mathf.Max(MinSpeedFactor, 1f - SlowPct) : 1f;
            }
        }

        void Awake()
        {
            _stats = GetComponent<SwarmStats>();
            _host = GetComponent<IKnockbackable>();
            _abilities = GetComponent<AbilityController>();
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Advances all timers by dt (scaled time). Public so the unit tests can drive it.</summary>
        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (_stats == null) _stats = GetComponent<SwarmStats>();
            if (_stats == null || _stats.IsDead) return;

            if (ToxicRemaining > 0f)
            {
                float step = Mathf.Min(dt, ToxicRemaining);
                ToxicRemaining -= dt;
                if (ToxicDps > 0f)
                {
                    _stats.TakeDamage(new DamageInfo(ToxicDps * step, DamageType.Toxic, ToxicSource,
                        transform.position, default, ToxicHazard));
                }
                if (ToxicRemaining <= 0f) { ToxicRemaining = 0f; ToxicDps = 0f; ToxicHazard = false; }
            }

            if (SlowRemaining > 0f)
            {
                SlowRemaining -= dt;
                if (SlowRemaining <= 0f) { SlowRemaining = 0f; SlowPct = 0f; }
            }

            if (StunRemaining > 0f)
            {
                StunRemaining -= dt;
                if (StunRemaining <= 0f)
                {
                    StunRemaining = 0f;
                    ImmunityRemaining = StunImmunitySeconds;
                }
            }
            else if (ImmunityRemaining > 0f)
            {
                ImmunityRemaining = Mathf.Max(0f, ImmunityRemaining - dt);
            }

            if (_externalTimer > 0f) _externalTimer -= dt;
        }

        public void ClearAll()
        {
            ToxicDps = 0f; ToxicRemaining = 0f; ToxicHazard = false;
            SlowPct = 0f; SlowRemaining = 0f;
            StunRemaining = 0f; ImmunityRemaining = 0f;
            _externalTimer = 0f;
        }

        // ---------------- Applying effects ----------------

        /// <summary>Applies everything a hit carries. 'hazard' marks hazard sources for the no-reward rule.</summary>
        public void Apply(StatusPayload p, Faction source, bool hazard)
        {
            if (_stats != null && _stats.IsDead) return;

            if (p.toxicDps > 0f) ApplyToxic(p.toxicDps, p.toxicDuration, source, hazard);
            if (p.slowPct > 0f) ApplySlow(p.slowPct, p.slowDuration);
            if (p.stunChance > 0f && Random.value < p.stunChance) ApplyStun(p.stunDuration);
            if (Mathf.Abs(p.knockDistance) > 0.0001f) Shove(p.sourcePos, p.knockDistance);
        }

        /// <summary>One Toxic at a time. Stronger replaces; equal or weaker only refreshes the duration.</summary>
        public void ApplyToxic(float dps, float duration, Faction source, bool hazard)
        {
            if (dps <= 0f || duration <= 0f) return;

            if (!ToxicActive || dps > ToxicDps)
            {
                ToxicDps = dps;
                ToxicRemaining = duration;
                _toxicTotal = duration;
                ToxicHazard = hazard;
                ToxicSource = source;
            }
            else
            {
                // Equal or weaker: refresh only. A player-caused refresh also takes over the credit for a hazard Toxic.
                ToxicRemaining = Mathf.Max(ToxicRemaining, duration);
                _toxicTotal = Mathf.Max(_toxicTotal, ToxicRemaining);
                if (!hazard) { ToxicHazard = false; ToxicSource = source; }
            }
        }

        /// <summary>The strongest slow applies; a new hit refreshes the duration. Bosses: half as long.</summary>
        public void ApplySlow(float pct, float duration)
        {
            if (pct <= 0f || duration <= 0f) return;
            if (IsBoss) duration *= BossScale;
            pct = Mathf.Min(pct, 1f - MinSpeedFactor);

            if (!SlowActive || pct > SlowPct)
            {
                SlowPct = pct;
                SlowRemaining = duration;
                _slowTotal = duration;
            }
            else
            {
                SlowRemaining = Mathf.Max(SlowRemaining, duration);
                _slowTotal = Mathf.Max(_slowTotal, SlowRemaining);
            }
        }

        /// <summary>
        /// Stun: ignored while stunned or during the 1 second of immunity after the last stun. Releases a deployed cell weapon.
        /// Returns true if the stun started.
        /// </summary>
        public bool ApplyStun(float duration)
        {
            if (duration <= 0f || Stunned || ImmunityRemaining > 0f) return false;
            if (_stats != null && _stats.IsDead) return false;
            if (IsBoss) duration *= BossScale;

            StunRemaining = duration;
            _stunTotal = duration;
            if (_abilities == null) _abilities = GetComponent<AbilityController>();
            if (_abilities != null) _abilities.OnStunned();
            Stunned_?.Invoke();
            return true;
        }

        /// <summary>
        /// Shove away from (distance &gt; 0) or toward (distance &lt; 0) 'from'. Half as far on bosses. DEF does not reduce it.
        /// </summary>
        public void Shove(Vector2 from, float distance)
        {
            if (_host == null) _host = GetComponent<IKnockbackable>();
            if (_host == null || Mathf.Abs(distance) < 0.0001f) return;
            if (IsBoss) distance *= BossScale;

            Vector2 to = (Vector2)transform.position - from;
            if (to.sqrMagnitude < 0.0001f) to = Random.insideUnitCircle;
            _host.Shove(to.normalized * distance, ShoveSeconds);
        }

        /// <summary>A hazard current acts on this swarm for the next few ticks.</summary>
        public void SetExternalVelocity(Vector2 velocity)
        {
            _external = velocity;
            _externalTimer = 0.15f;
        }

        // ---------------- Visuals ----------------

        static readonly Color ToxicTint = new Color(0.25f, 0.90f, 0.25f, 1f);
        static readonly Color SlowTint = new Color(0.30f, 0.55f, 1.00f, 1f);
        static readonly Color StunTint = new Color(1.00f, 0.95f, 0.25f, 1f);

        /// <summary>Green for Toxic, blue for Slow, a yellow flicker for Stun.</summary>
        public Color Tint(Color baseColor)
        {
            Color c = baseColor;
            if (ToxicActive) c = Color.Lerp(c, ToxicTint, 0.45f);
            if (SlowActive) c = Color.Lerp(c, SlowTint, 0.40f);
            if (Stunned && Mathf.Repeat(Time.time * 8f, 1f) < 0.5f) c = Color.Lerp(c, StunTint, 0.75f);
            return c;
        }
    }
}
