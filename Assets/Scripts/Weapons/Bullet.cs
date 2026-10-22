using Cellstorm.Combat;
using Cellstorm.Enemies;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// A bullet with its own DEF. Hitting a hostile swarm or weapon cell with lower DEF defeats it
    /// (GDD: higher DEF wins). Equal or lower DEF: the bullet is destroyed and nothing happens.
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
        public float Defense { get; private set; }

        bool _autoWin;
        int _pierceLeft;
        int _attackerCells;
        float _life = 8f;

        public static Bullet Spawn(Vector2 position, Vector2 direction, float speed, float defense, int pierce,
            bool autoWin, Faction faction, Color color, float radius, int attackerCells)
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
            bullet.Defense = defense;
            bullet._autoWin = autoWin;
            bullet._pierceLeft = Mathf.Max(0, pierce);
            bullet._attackerCells = attackerCells;

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
                if (Defense > cell.Defense)
                {
                    cell.Break();
                    Consume(false);
                }
                else
                {
                    Destroy(gameObject);
                }
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
            float targetDef = target.Stats.Defense;

            if (Defense > targetDef)
            {
                int damage = _autoWin
                    ? target.Stats.Cells
                    : Mathf.Max(1, Mathf.RoundToInt(DamageCalculator.ContactDps(
                        _attackerCells, Defense, target.Stats.Cells, targetDef)));

                bool boss = target.TryGetComponent<Enemy>(out var enemy) && enemy.IsBoss;

                target.Stats.TakeDamage(damage);
                target.ReactToHit(transform.position);
                Consume(boss);
            }
            else
            {
                // Equal DEF bounces, lower DEF is blocked. Either way nothing happens.
                Destroy(gameObject);
            }
        }

        void Consume(bool targetIsBoss)
        {
            // GDD: pierce is always 0 against bosses.
            if (targetIsBoss || _pierceLeft <= 0) Destroy(gameObject);
            else _pierceLeft--;
        }
    }
}
