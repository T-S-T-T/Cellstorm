using Cellstorm.Combat;
using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>
    /// Simple look for non-player swarms: one circle with a FIXED size (set once from the max HP, GDD V0.1.1
    /// section 4.1), darker when hurt, and a short white flash when it takes damage.
    /// Replaced by the soft ball visual in Phase 3.
    /// </summary>
    public class CircleSwarmVisual : MonoBehaviour
    {
        [SerializeField] Color color = new Color(0.95f, 0.35f, 0.35f, 1f);
        [SerializeField] int sortingOrder = 5;

        SwarmStats _stats;
        CircleCollider2D _col;
        SpriteRenderer _sr;
        Transform _spriteT;
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

            // Size is decided once, when the swarm appears.
            _radius = SwarmSize.ForEnemy(_stats.MaxHp);
            if (_col != null) _col.radius = SwarmSize.Collider(_radius);

            var go = new GameObject("Visual");
            go.transform.SetParent(transform, false);
            _spriteT = go.transform;
            _spriteT.localScale = Vector3.one * (_radius * 2f);

            _sr = go.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralSprites.Hard;
            _sr.sharedMaterial = SpriteMaterials.Default;
            _sr.sortingOrder = sortingOrder;

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

        void Update()
        {
            if (_flash > 0f) _flash -= Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            float frac = Mathf.Clamp01(_stats.Hp / _stats.MaxHp);
            var dark = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f);
            var c = Color.Lerp(dark, color, frac);
            _sr.color = _flash > 0f ? Color.Lerp(c, Color.white, 0.8f) : c;
        }
    }
}
