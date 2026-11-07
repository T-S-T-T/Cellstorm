using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Data;
using Cellstorm.Progression;
using Cellstorm.Swarm;
using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.Enemies
{
    /// <summary>
    /// A regular enemy, support cell or boss: builds the swarm components, runs one movement behaviour (chase, keep
    /// distance, explode, support) and, if it has a weapon, a weapon AI that uses the same abilities as the player
    /// (Ram, Jaw, Gun, Whip, Spread Shot, Spore Cloud, Mine). It chases the nearest of the player and the player's minions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SwarmStats))]
    public class Enemy : MonoBehaviour, IAbilityHost
    {
        public static readonly List<Enemy> Active = new List<Enemy>();

        /// <summary>Raised when an enemy dies from damage (not when it despawns, explodes, or dies only to hazards).</summary>
        public static event System.Action<Enemy> Killed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active.Clear();
            Killed = null;
        }

        [SerializeField] float turnRateDegPerSec = 240f;

        public EnemySpec Spec { get; private set; }
        public SwarmStats Stats { get; private set; }
        public bool IsBoss => Spec.isBoss;
        public bool IsSupport => Spec.behavior == EnemyBehavior.Support;
        public int ScoreValue => Spec.scoreValue;
        /// <summary>Collider radius (edge distance maths for targeting).</summary>
        public float Radius => _col != null ? _col.radius : 0.3f;
        public ITargetable CurrentTarget => _target;

        /// <summary>Resource point guard: stands near its point until the player comes within 8 units.</summary>
        public bool IsGuard { get; private set; }
        public bool GuardAware { get; private set; }
        public Vector2 GuardCenter { get; private set; }

        public float TurnRateMultiplier { get; set; } = 1f;
        public Vector2 AimDirection =>
            new Vector2(Mathf.Cos(_aimAngle * Mathf.Deg2Rad), Mathf.Sin(_aimAngle * Mathf.Deg2Rad));

        Rigidbody2D _rb;
        CircleCollider2D _col;
        ITargetable _target;
        SwarmBody _player;
        AbilityController _abilities;
        Vector2 _knockback;
        ShoveState _shove;
        float _aimAngle;
        bool _aimInit;
        bool _finished;
        float _retarget;
        float _healTimer;
        float _nextOrb;
        float _convertAccum;

        const float GuardWakeRange = 8f;
        const float GuardLeash = 3f;

        public static Enemy Create(EnemySpec spec, Vector2 position)
        {
            // Ranged enemies always carry the enemy gun, even if their data asset predates weapons.
            if (spec.weapon == null && spec.behavior == EnemyBehavior.KeepDistance)
                spec.weapon = WeaponDefaults.EnemyGun;

            var go = new GameObject(spec.name);
            go.transform.position = position;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.mass = spec.isBoss ? 20f : 1f;

            if (spec.damageMultiplier <= 0f) spec.damageMultiplier = 1f;

            go.AddComponent<CircleCollider2D>();
            var stats = go.AddComponent<SwarmStats>();
            stats.Configure(spec.maxHp, 0f, spec.defense, spec.speed);
            stats.Status.IsBoss = spec.isBoss;               // bosses: half-length stun and slow, half-distance shoves
            var combat = go.AddComponent<SwarmCombat>();
            combat.SetFaction(Faction.Enemy);
            combat.ContactDps = spec.contactDps * spec.damageMultiplier;
            // The visual sets the fixed body size from the max HP when it wakes up.
            go.AddComponent<CircleSwarmVisual>().SetColor(spec.color);

            var enemy = go.AddComponent<Enemy>();
            enemy.Spec = spec;

            if (spec.weapon != null)
            {
                enemy._abilities = go.AddComponent<AbilityController>();
                enemy._abilities.DamageMultiplier = spec.damageMultiplier;
                enemy._abilities.Equip(0, spec.weapon);
            }

            if (spec.behavior == EnemyBehavior.Support) enemy.AddHealRing();
            return enemy;
        }

        /// <summary>Turns this enemy into a resource point guard standing around 'center'.</summary>
        public void MakeGuard(Vector2 center)
        {
            IsGuard = true;
            GuardAware = false;
            GuardCenter = center;
        }

        /// <summary>Counts regular enemies for the 50-enemy cap. Bosses and guards bypass the cap, so they are not counted.</summary>
        public static int CountNonBoss()
        {
            int n = 0;
            for (int i = 0; i < Active.Count; i++)
                if (!Active[i].IsBoss && !Active[i].IsGuard) n++;
            return n;
        }

        void AddHealRing()
        {
            var ring = new GameObject("HealRing");
            ring.transform.SetParent(transform, false);
            float d = Mathf.Max(0.5f, Spec.healRange) * 2f;
            ring.transform.localScale = new Vector3(d, d, 1f);
            var sr = ring.AddComponent<SpriteRenderer>();
            sr.sprite = FxSprites.Ring;
            sr.sharedMaterial = SpriteMaterials.Default;
            sr.sortingOrder = 2;
            sr.color = new Color(0.3f, 1f, 0.4f, 0.45f);
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CircleCollider2D>();
            Stats = GetComponent<SwarmStats>();
            Stats.Died += OnStatsDied;
            Stats.DamagedBy += OnDamagedBy;
            _retarget = Random.value * 0.25f;
            _healTimer = Random.value;
        }

        void OnEnable()
        {
            Active.Add(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
        }

        void OnDestroy()
        {
            if (Stats != null)
            {
                Stats.Died -= OnStatsDied;
                Stats.DamagedBy -= OnDamagedBy;
            }
        }

        void OnStatsDied()
        {
            if (_finished) return;
            _finished = true;

            SpreadToxic();

            // A kill caused only by hazards gives no score and no EXP (GDD V0.2 section 4.4).
            if (!Stats.HazardOnly || IsBoss) Killed?.Invoke(this);
            // SwarmCombat destroys the object.
        }

        /// <summary>Toxic evolution: a poisoned enemy that dies spreads its Toxic to enemies within 2 units.</summary>
        void SpreadToxic()
        {
            var status = Stats.Status;
            if (!status.ToxicActive) return;
            var build = PlayerBuild.Instance;
            if (build == null || build.Abilities == null || !build.Abilities.Mods.Has(Param.ToxicSpread)) return;

            Vector2 c = transform.position;
            for (int i = 0; i < Active.Count; i++)
            {
                var e = Active[i];
                if (e == null || e == this || e.Stats.IsDead) continue;
                if (((Vector2)e.transform.position - c).sqrMagnitude > 4f) continue;     // 2 units
                e.Stats.Status.ApplyToxic(status.ToxicDps, Mathf.Max(0.5f, status.ToxicRemaining), Faction.Player, false);
            }
        }

        /// <summary>Convert: damage from the player drops HP orbs (at most one per 0.5 s per enemy).</summary>
        void OnDamagedBy(DamageInfo info, float dealt)
        {
            if (!ConvertEffect.Active || info.sourceFaction != Faction.Player || info.hazard || dealt <= 0f) return;

            _convertAccum += dealt;
            if (Time.time < _nextOrb) return;
            _nextOrb = Time.time + 0.5f;

            float heal = Mathf.Max(0.5f, _convertAccum * ConvertEffect.Fraction);
            _convertAccum = 0f;
            Pickup.SpawnHeal(PickupType.HpOrb, transform.position, heal);
        }

        public void AddKnockback(Vector2 velocity)
        {
            _knockback = Vector2.ClampMagnitude(_knockback + velocity, 12f);
        }

        public void Shove(Vector2 displacement, float duration)
        {
            _shove.Set(displacement, duration);
        }

        Vector2 _dashVelocity;
        float _dashTimer;

        public void BeginDash(Vector2 direction, float speed, float duration)
        {
            _dashVelocity = direction.normalized * speed;
            _dashTimer = duration;
        }

        public void EndDash()
        {
            _dashTimer = 0f;
        }

        /// <summary>Remove without reward (too far from the player, or its resource point disappeared).</summary>
        public void Despawn()
        {
            if (_finished) return;
            _finished = true;
            Destroy(gameObject);
        }

        bool Stunned => Stats.Status.Stunned;

        void Update()
        {
            if (_finished) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (_player == null) _player = FindAnyObjectByType<SwarmBody>();
            if (_player == null) return;

            if (IsGuard && !GuardAware)
            {
                if (_player.IsDead) return;
                if (((Vector2)_player.transform.position - (Vector2)transform.position).magnitude <= GuardWakeRange)
                    GuardAware = true;
                else
                    return;                 // does nothing until the player comes close
            }

            _retarget -= dt;
            if (!TargetingService.IsValid(_target) || _retarget <= 0f)
            {
                _retarget = 0.25f;
                _target = TargetingService.Nearest(transform.position, _col.radius);
            }
            if (_target == null) return;

            Vector2 to = _target.Position - (Vector2)transform.position;
            float targetAngle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;

            if (!_aimInit)
            {
                _aimAngle = targetAngle;
                _aimInit = true;
            }
            if (!Stunned)
            {
                _aimAngle = Mathf.MoveTowardsAngle(
                    _aimAngle, targetAngle, turnRateDegPerSec * TurnRateMultiplier * dt);
            }

            UpdateWeaponAI(to);
            if (IsSupport) UpdateHealing(dt);
        }

        void UpdateHealing(float dt)
        {
            _healTimer -= dt;
            if (_healTimer > 0f || Stunned) return;
            _healTimer = 1f;

            float range = Mathf.Max(0.5f, Spec.healRange);
            Vector2 c = transform.position;
            for (int i = 0; i < Active.Count; i++)
            {
                var e = Active[i];
                if (e == null || e == this || e.IsSupport || e.Stats.IsDead) continue;
                if (((Vector2)e.transform.position - c).sqrMagnitude > range * range) continue;
                float share = e.IsBoss ? Spec.healFraction * 0.2f : Spec.healFraction;     // 5% per second, bosses 1%
                e.Stats.Heal(e.Stats.MaxHp * share);
            }
        }

        void UpdateWeaponAI(Vector2 to)
        {
            if (_abilities == null) return;
            var ability = _abilities.GetSlot(0);
            if (ability == null) return;

            bool held = false;

            if (TargetingService.IsValid(_target) && !Stunned)
            {
                var data = ability.Data;
                float dist = to.magnitude;
                float edge = dist - _target.Radius - _col.radius;
                float angleError = Vector2.Angle(AimDirection, to);
                bool pulse = (Time.frameCount & 1) == 0;       // abilities that fire on a key press need a fresh press each time

                switch (data.type)
                {
                    case WeaponType.Gun:
                    {
                        // Start forming the gun when in range; fire once complete and lined up.
                        bool inRange = dist <= Spec.preferredRange + 4f;
                        bool aligned = angleError < 8f;
                        held = inRange && !(ability.Phase == AbilityPhase.Primed && aligned);
                        break;
                    }
                    case WeaponType.SpreadShot:
                    {
                        // Approaches to about 6 units; only fires within 6 units.
                        bool inRange = dist <= Spec.preferredRange + 2f;
                        bool canFire = ability.Phase == AbilityPhase.Primed && angleError < 15f && dist <= data.pelletRange;
                        held = inRange && !canFire;
                        break;
                    }
                    case WeaponType.Jaw:
                    {
                        bool close = edge <= 5f;
                        bool inBiteRange = edge <= data.coneRange * 0.9f && angleError < 20f;
                        held = close && !(ability.Phase == AbilityPhase.Primed && inBiteRange);
                        break;
                    }
                    case WeaponType.Whip:
                    {
                        bool close = edge <= 5f;
                        bool inLashRange = edge <= data.whipRange * 0.9f && angleError < 40f;
                        held = close && !(ability.Phase == AbilityPhase.Primed && inLashRange);
                        break;
                    }
                    case WeaponType.Ram:
                        held = dist < 4f && angleError < 15f && pulse;
                        break;
                    case WeaponType.SporeCloud:
                        held = dist < 3f && pulse;
                        break;
                    case WeaponType.Mine:
                        held = dist < 10f && pulse;
                        break;
                }
            }

            _abilities.SetAiHeld(0, held);
        }

        void FixedUpdate()
        {
            if (_finished) return;

            var status = Stats.Status;

            if (_dashTimer > 0f)
            {
                _dashTimer -= Time.fixedDeltaTime;
                _rb.linearVelocity = _dashVelocity + _shove.Step(Time.fixedDeltaTime);
                return;
            }

            // Waiting guards only stay near their point.
            if (IsGuard && !GuardAware)
            {
                Vector2 off = GuardCenter - (Vector2)transform.position;
                _rb.linearVelocity = off.magnitude > GuardLeash ? off.normalized * 1f : Vector2.zero;
                return;
            }

            Vector2 move = Vector2.zero;

            if (TargetingService.IsValid(_target) && !status.Stunned)
            {
                Vector2 to = _target.Position - (Vector2)transform.position;
                float dist = to.magnitude;
                Vector2 dir = dist > 0.0001f ? to / dist : Vector2.zero;
                float speed = SpeedMath.MoveSpeed(Stats.Speed) * status.SpeedFactor;

                switch (Spec.behavior)
                {
                    case EnemyBehavior.Chase:
                        move = dir * speed;
                        break;

                    case EnemyBehavior.KeepDistance:
                    {
                        float error = dist - Spec.preferredRange;
                        if (error > 0.5f) move = dir * speed;
                        else if (error < -0.5f) move = -dir * speed;
                        break;
                    }

                    case EnemyBehavior.Support:
                    {
                        // Follows its target but stops 8 units short, so it stays behind the other enemies.
                        float stand = Spec.supportStandOff > 0f ? Spec.supportStandOff : 8f;
                        float error = dist - stand;
                        if (error > 0.5f) move = dir * speed;
                        else if (error < -1.0f) move = -dir * speed * 0.6f;
                        break;
                    }

                    case EnemyBehavior.Explode:
                    {
                        move = dir * speed;
                        float gap = dist - _target.Radius - _col.radius;
                        if (gap <= Spec.explosionTriggerRange)
                        {
                            Explode();
                            return;
                        }
                        break;
                    }
                }
            }

            _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, 20f * Time.fixedDeltaTime);
            _rb.linearVelocity = move + _knockback + _shove.Step(Time.fixedDeltaTime) + status.External;
        }

        void Explode()
        {
            if (_finished) return;
            _finished = true;

            Vector2 center = transform.position;

            // Overlap query: the player's body, its minions (DEF applies once) and weapon cells inside the radius.
            DamageUtil.Explode(center, Spec.explosionRadius, Spec.explosionDamage * Spec.damageMultiplier, Faction.Enemy);

            ExplosionFx.Spawn(center, Spec.explosionRadius, Spec.color);
            Destroy(gameObject);
        }
    }
}
