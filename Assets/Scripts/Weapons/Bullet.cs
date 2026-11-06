using Cellstorm.Combat;
using Cellstorm.Enemies;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// A bullet with a damage value. Hitting a hostile swarm deals that damage (reduced by the target's DEF);
    /// hitting a hostile weapon cell breaks it. Pierce is always 0 against bosses.
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

        int _pierceLeft;
        float _life = 8f;

        public static Bullet Spawn(Vector2 position, Vector2 direction, float speed, float damage, int pierce,
            Faction faction, Color color, float radius)
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

        void OnTriggerEnter2D(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            var go = rb != null ? rb.gameObject : other.gameObject;

            if (go.TryGetComponent<WeaponCell>(out var cell))
            {
                if (cell.Faction == Faction) return;
                cell.Break();
                Consume(false);
                return;
            }

            if (go.TryGetComponent<SwarmCombat>(out var target))
            {
                if (target.Faction == Faction || target.Stats.IsDead) return;
                HitSwarm(target);
            }
        }

        void HitSwarm(SwarmCombat target)
        {
            bool boss = target.TryGetComponent<Enemy>(out var enemy) && enemy.IsBoss;

            target.Stats.TakeDamage(new DamageInfo(Damage, DamageType.Bullet, Faction, transform.position));
            target.ReactToHit(transform.position);
            Consume(boss);
        }

        void Consume(bool targetIsBoss)
        {
            // GDD: pierce is always 0 against bosses.
            if (targetIsBoss || _pierceLeft <= 0) Destroy(gameObject);
            else _pierceLeft--;
        }
    }
}
