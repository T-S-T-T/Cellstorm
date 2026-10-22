using System.Collections.Generic;
using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>
    /// Draws the swarm as a capped number of soft dots on springs. The dots live on the Metaball
    /// layer and are merged into one blob by MetaballLayer. Cell counts above maxVisualCells only
    /// make the body larger; they do not add simulated objects.
    /// </summary>
    public class SwarmVisual : MonoBehaviour
    {
        class Dot
        {
            public Transform t;
            public SpriteRenderer sr;
            public Vector2 pos, vel, rest;
            public float k, c, seed, jitter;
        }

        const float MaxDt = 0.033f;
        const float GoldenAngle = 2.399963f;

        [SerializeField] SwarmBody body;
        [SerializeField] string metaballLayerName = "Metaball";
        [SerializeField] Color color = new Color(0.30f, 0.85f, 0.55f, 1f);
        [Tooltip("Hard cap on simulated dots. Raise it to stress test.")]
        [SerializeField, Min(1)] int maxVisualCells = 150;

        [Header("Shape")]
        [Tooltip("Dot radius = this * R / sqrt(dots). Higher = blobbier and more merged.")]
        [SerializeField] float dotSizeFactor = 1.4f;

        [Header("Springs")]
        [SerializeField] float innerStiffness = 70f;
        [Tooltip("Outer dots are looser, so the edge lags and stretches when moving.")]
        [SerializeField] float outerStiffness = 35f;
        [SerializeField, Range(0.1f, 1f)] float dampingRatio = 0.4f;
        [Tooltip("Idle wobble, as a fraction of the radius.")]
        [SerializeField] float wobble = 0.03f;
        [SerializeField] float dentStrength = 6f;

        [Header("Debug")]
        [SerializeField] bool showAimMarker = true;

        readonly List<Dot> _dots = new List<Dot>();
        Transform _container;
        Material _dotMaterial;
        Transform _aimMarker;
        int _layer;
        int _active = -1;

        public int ActiveVisualCells => Mathf.Max(0, _active);
        public Color Color => color;
        public int MetaballLayerIndex => _layer;

        void Awake()
        {
            if (body == null) body = GetComponent<SwarmBody>();

            _layer = LayerMask.NameToLayer(metaballLayerName);
            if (_layer < 0)
            {
                Debug.LogError($"SwarmVisual: layer '{metaballLayerName}' does not exist. " +
                               "Run the menu Cellstorm > Phase 1 > Add Metaball Layer.");
                _layer = 0;
            }

            var shader = Shader.Find("Sprites/Default");
            _dotMaterial = new Material(shader);

            _container = new GameObject("SwarmCells").transform;

            if (showAimMarker)
            {
                var go = new GameObject("AimMarker");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSprites.Hard;
                sr.sharedMaterial = _dotMaterial;
                sr.color = new Color(1f, 1f, 1f, 0.9f);
                sr.sortingOrder = 20;
                go.transform.localScale = Vector3.one * 0.25f;
                _aimMarker = go.transform;
            }
        }

        void OnDestroy()
        {
            if (_container != null) Destroy(_container.gameObject);
            if (_aimMarker != null) Destroy(_aimMarker.gameObject);
            if (_dotMaterial != null) Destroy(_dotMaterial);
        }

        void Update()
        {
            if (body == null) return;

            if (body.IsDead)
            {
                if (_container.gameObject.activeSelf) _container.gameObject.SetActive(false);
                if (_aimMarker != null && _aimMarker.gameObject.activeSelf) _aimMarker.gameObject.SetActive(false);
                return;
            }

            float dt = Mathf.Min(Time.deltaTime, MaxDt);

            int want = Mathf.Min(body.CellCount, maxVisualCells);
            if (want != _active) Rebuild(want);

            float R = body.SmoothedRadius;
            float dotR = Mathf.Min(dotSizeFactor * R / Mathf.Sqrt(_active), R);
            float spread = Mathf.Max(0f, R - dotR);
            float diameter = 2f * dotR / ProceduralSprites.SoftVisibleRadiusFactor;
            Vector2 center = body.transform.position;
            float time = Time.time;

            for (int i = 0; i < _active; i++)
            {
                var d = _dots[i];
                Vector2 target = center + d.rest * spread;

                if (wobble > 0f)
                {
                    float nx = Mathf.PerlinNoise(time * 0.7f + d.seed, 0.37f) - 0.5f;
                    float ny = Mathf.PerlinNoise(0.91f, time * 0.7f + d.seed) - 0.5f;
                    target += new Vector2(nx, ny) * (wobble * R * 2f);
                }

                if (dt > 0f)
                {
                    Vector2 acc = (target - d.pos) * d.k - d.vel * d.c;
                    d.vel += acc * dt;
                    d.pos += d.vel * dt;
                }

                d.t.position = new Vector3(d.pos.x, d.pos.y, 0f);
                d.t.localScale = new Vector3(diameter, diameter, 1f);
            }

            if (_aimMarker != null)
            {
                Vector2 p = center + body.AimDirection * (R + 0.25f);
                _aimMarker.position = new Vector3(p.x, p.y, 0f);
            }
        }

        /// <summary>
        /// Visual hit reaction: cells near worldPoint get pushed toward the body centre and then
        /// spring back. Pass strength &lt; 0 to use the default.
        /// </summary>
        public void Dent(Vector2 worldPoint, float strength = -1f, float reachFactor = 0.9f)
        {
            if (body == null || _active <= 0) return;
            if (strength < 0f) strength = dentStrength;

            float R = body.SmoothedRadius;
            float reach = Mathf.Max(0.5f, R * reachFactor);
            Vector2 center = body.transform.position;

            for (int i = 0; i < _active; i++)
            {
                var d = _dots[i];
                float dist = (d.pos - worldPoint).magnitude;
                if (dist >= reach) continue;

                float falloff = 1f - dist / reach;
                Vector2 inward = center - d.pos;
                inward = inward.sqrMagnitude > 1e-6f ? inward.normalized : Vector2.zero;
                d.vel += inward * (strength * falloff * Mathf.Max(1f, R));
            }
        }

        void Rebuild(int n)
        {
            n = Mathf.Max(1, n);
            while (_dots.Count < n) _dots.Add(CreateDot());

            Vector2 center = body.transform.position;

            for (int i = 0; i < _dots.Count; i++)
            {
                var d = _dots[i];
                bool on = i < n;

                if (d.t.gameObject.activeSelf != on)
                {
                    d.t.gameObject.SetActive(on);
                    if (on)
                    {
                        d.pos = center + Random.insideUnitCircle * 0.1f;
                        d.vel = Vector2.zero;
                    }
                }
                if (!on) continue;

                float frac = Mathf.Sqrt((i + 0.5f) / n);
                float angle = i * GoldenAngle;
                d.rest = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * frac;
                d.k = Mathf.Lerp(innerStiffness, outerStiffness, frac) * d.jitter;
                d.c = 2f * Mathf.Sqrt(d.k) * dampingRatio;
            }

            _active = n;
        }

        Dot CreateDot()
        {
            var go = new GameObject("Cell");
            go.layer = _layer;
            go.transform.SetParent(_container, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSprites.Soft;
            sr.sharedMaterial = _dotMaterial;
            sr.color = color;

            Vector2 center = body != null ? (Vector2)body.transform.position : Vector2.zero;
            return new Dot
            {
                t = go.transform,
                sr = sr,
                pos = center + Random.insideUnitCircle * 0.1f,
                seed = Random.value * 100f,
                jitter = Random.Range(0.85f, 1.15f)
            };
        }
    }
}
