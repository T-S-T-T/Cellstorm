using Cellstorm.Combat;
using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>
    /// Simple stand-in look for non-player swarms: one circle sized by cell count, darker when hurt.
    /// Enemies get the metaball look in Phase 7 (or earlier if you want).
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
        Material _mat;

        public void SetColor(Color c)
        {
            color = c;
        }

        void Awake()
        {
            _stats = GetComponent<SwarmStats>();
            _col = GetComponent<CircleCollider2D>();

            var go = new GameObject("Visual");
            go.transform.SetParent(transform, false);
            _spriteT = go.transform;

            _mat = new Material(Shader.Find("Sprites/Default"));
            _sr = go.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralSprites.Hard;
            _sr.sharedMaterial = _mat;
            _sr.sortingOrder = sortingOrder;

            Apply();
        }

        void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }

        void Update()
        {
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
            _sr.color = Color.Lerp(dark, color, frac);
        }
    }
}
