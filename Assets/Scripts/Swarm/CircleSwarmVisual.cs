using Cellstorm.Combat;
using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>
    /// Simple look for non-player swarms: one circle sized by cell count, darker when hurt, and a short white
    /// flash when it takes damage.
    /// </summary>
    public class CircleSwarmVisual : MonoBehaviour
    {
        [SerializeField] Color color = new Color(0.95f, 0.35f, 0.35f, 1f);
        [SerializeField] float radiusPerSqrtCell = 0.15f;
        [SerializeField] int sortingOrder = 5;

        SwarmStats _stats;
        CircleCollider2D _col;
        SpriteRenderer _sr;
        Transform _spriteT;
        float _flash;

        public void SetColor(Color c)
        {
            color = c;
        }

        public Color Color => color;

        void Awake()
        {
            _stats = GetComponent<SwarmStats>();
            _col = GetComponent<CircleCollider2D>();

            var go = new GameObject("Visual");
            go.transform.SetParent(transform, false);
            _spriteT = go.transform;

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

        void OnDamaged(int amount)
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
            int cells = Mathf.Max(1, _stats.Cells);
            float r = radiusPerSqrtCell * Mathf.Sqrt(cells);

            if (_col != null) _col.radius = r;
            _spriteT.localScale = Vector3.one * (r * 2f);

            float frac = Mathf.Clamp01((float)cells / _stats.MaxCells);
            var dark = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f);
            var c = Color.Lerp(dark, color, frac);
            _sr.color = _flash > 0f ? Color.Lerp(c, Color.white, 0.8f) : c;
        }
    }
}
