using Cellstorm.Combat;
using Cellstorm.Enemies;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>Extras a projectile can carry (V0.2): status effects, homing, an explosion on impact, a range limit.</summary>
    public struct BulletOptions
    {
        public StatusPayload status;
        public bool homing;
        public float explodeRadius;     // 0 = no explosion
        public float explodeDamage;
        public float maxRange;          // 0 = unlimited (lives 8 s); pellets use 6
    }

    /// <summary>
    /// A bullet with a damage value. Hitting a hostile swarm or weapon cell deals that damage (reduced by the target's DEF). Pierce is always 0 against bosses.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Bullet : MonoBehaviour
    {
        static Material _material;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _material = null;
        }

        public Faction Faction { get; private set; }
        public float Damage { get; private set; }

        BulletOptions _opt;
        Vector2 _lastPos;
        float _travelled;
        int _pierceLeft;
        float _radius = 0.2f;
        float _life = 8f;

        public static Bullet Spawn(Vector2 position, Vector2 direction, float speed, float damage, int pierce,
            Faction faction, Color color, float radius, BulletOptions options = default)
        {
            var go = new GameObject("Bullet");
            go.transform.position = position;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.linearDamping = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = direction.normalized * speed;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius;

            var bullet = go.AddComponent<Bullet>();
            bullet.Faction = faction;
            bullet.Damage = damage;
            bullet._pierceLeft = Mathf.Max(0, pierce);
            bullet._radius = radius;
            bullet._opt = options;
            bullet._lastPos = position;

            if (_material == null) _material = new Material(Shader.Find("Sprites/Default"));
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * (radius * 2f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSprites.Hard;
            sr.sharedMaterial = _material;
            sr.color = color;
            sr.sortingOrder = 12;

            return bullet;
        }

        public void Kill()
        {
            Destroy(gameObject);
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        void FixedUpdate()
        {
            if (_opt.homing && Faction == Faction.Player) Steer();
            if (_opt.maxRange > 0f)
            {
                Vector2 p = transform.position;
                _travelled += (p - _lastPos).magnitude;
                _lastPos = p;
                if (_travelled >= _opt.maxRange)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            // Weapon cells have no colliders: a bullet that reaches one damages it and is used up.
            if (WeaponCellGroup.DamageFirstTouching(transform.position, _radius, Damage, Faction))
            {
                Explode();
                Consume(false);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            var go = rb != null ? rb.gameObject : other.gameObject;

            if (go.TryGetComponent<SwarmCombat>(out var target))
            {
                if (target.Faction == Faction || target.Stats.IsDead) return;
                HitSwarm(target);
            }
        }

        void HitSwarm(SwarmCombat target)
        {
            bool boss = target.TryGetComponent<Enemy>(out var enemy) && enemy.IsBoss;

            var status = _opt.status;
            status.sourcePos = transform.position;
            target.Stats.TakeDamage(new DamageInfo(Damage, DamageType.Bullet, Faction, transform.position, status));
            target.ReactToHit(transform.position);
            Explode();
            Consume(boss);
        }

        void Explode()
        {
            if (_opt.explodeRadius <= 0f) return;
            Vector2 c = transform.position;
            DamageUtil.Explode(c, _opt.explodeRadius, _opt.explodeDamage, Faction);
            ExplosionFx.Spawn(c, _opt.explodeRadius, new Color(1f, 0.6f, 0.2f));
        }

        /// <summary>Hunter evolution: curve toward the nearest enemy.</summary>
        void Steer()
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;
            Vector2 pos = transform.position;
            Enemy best = null;
            float bestSqr = 100f;                       // 10 units
            for (int i = 0; i < Enemy.Active.Count; i++)
            {
                var e = Enemy.Active[i];
                if (e == null || e.Stats.IsDead) continue;
                float d = ((Vector2)e.transform.position - pos).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = e; }
            }
            if (best == null) return;

            Vector2 v = rb.linearVelocity;
            float speed = v.magnitude;
            if (speed < 0.01f) return;
            float cur = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            Vector2 to = (Vector2)best.transform.position - pos;
            float want = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            float next = Mathf.MoveTowardsAngle(cur, want, 220f * Time.fixedDeltaTime) * Mathf.Deg2Rad;
            rb.linearVelocity = new Vector2(Mathf.Cos(next), Mathf.Sin(next)) * speed;
        }

        void Consume(bool targetIsBoss)
        {
            // GDD: pierce is always 0 against bosses.
            if (targetIsBoss || _pierceLeft <= 0) Destroy(gameObject);
            else _pierceLeft--;
        }
    }
}
