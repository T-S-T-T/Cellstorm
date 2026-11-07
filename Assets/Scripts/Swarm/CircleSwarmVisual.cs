using Cellstorm.Combat;
using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>
    /// Enemy look: one soft-looking ball with a dark outline and a FIXED size (set once from the max HP, GDD V0.1.1
    /// section 4.1). It squashes and stretches like the player, goes darker as it loses HP and flashes white when hit.
    /// </summary>
    public class CircleSwarmVisual : MonoBehaviour, IHitVisual
    {
        [SerializeField] Color color = new Color(0.95f, 0.35f, 0.35f, 1f);
        [SerializeField] int sortingOrder = 5;

        readonly SoftDeform _deform = new SoftDeform();
        SwarmStats _stats;
        CircleCollider2D _col;
        Rigidbody2D _rb;
        SpriteRenderer _sr;
        SpriteRenderer _outline;
        Transform _ball;
        float _flash;
        float _radius;

        public void SetColor(Color c)
        {
            color = c;
        }

        public Color Color => color;
        /// <summary>The fixed visual radius.</summary>
        public float Radius => _radius;

        void Awake()
        {
            _stats = GetComponent<SwarmStats>();
            _col = GetComponent<CircleCollider2D>();
            _rb = GetComponent<Rigidbody2D>();

            // Size is decided once, when the swarm appears.
            _radius = SwarmSize.ForEnemy(_stats.MaxHp);
            if (_col != null) _col.radius = SwarmSize.Collider(_radius);

            var go = new GameObject("Visual");
            go.transform.SetParent(transform, false);
            _ball = go.transform;
            _sr = go.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralSprites.Hard;
            _sr.sharedMaterial = SpriteMaterials.Default;
            _sr.sortingOrder = sortingOrder;

            var o = new GameObject("Outline");
            o.transform.SetParent(go.transform, false);
            o.transform.localScale = Vector3.one * 1.14f;
            _outline = o.AddComponent<SpriteRenderer>();
            _outline.sprite = ProceduralSprites.Hard;
            _outline.sharedMaterial = SpriteMaterials.Default;
            _outline.sortingOrder = sortingOrder - 1;

            _stats.Damaged += OnDamaged;
            Apply();
        }

        void OnDestroy()
        {
            if (_stats != null) _stats.Damaged -= OnDamaged;
        }

        void OnDamaged(float amount)
        {
            _flash = 0.12f;
        }

        public void Hit(Vector2 worldPoint, float strength)
        {
            _deform.Hit(worldPoint - (Vector2)transform.position, strength * 0.06f);
        }

        /// <summary>Same call the player visual has, so SwarmCombat and debug code do not care which one it is.</summary>
        public void Dent(Vector2 worldPoint, float strength = -1f, float reachFactor = 0.9f)
        {
            Hit(worldPoint, strength < 0f ? 5f : strength);
        }

        void Update()
        {
            if (_flash > 0f) _flash -= Time.deltaTime;
            Vector2 vel = _rb != null ? _rb.linearVelocity : Vector2.zero;
            _deform.Tick(Time.deltaTime, vel, Time.time);
            _ball.localPosition = Vector3.zero;
            _deform.Apply(_ball, _radius * 2f);
            Apply();
        }

        void Apply()
        {
            float frac = Mathf.Clamp01(_stats.Hp / _stats.MaxHp);
            var dark = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f);
            var c = _stats.Status.Tint(Color.Lerp(dark, color, frac));
            _sr.color = _flash > 0f ? Color.Lerp(c, Color.white, 0.8f) : c;
            _outline.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 1f);
        }
    }
}
