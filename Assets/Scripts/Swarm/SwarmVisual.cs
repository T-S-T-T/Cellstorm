using Cellstorm.Combat;
using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>
    /// The player swarm as ONE soft ball (GDD V0.1.1 section 4.4, option A): a single soft sprite on the Metaball
    /// layer, thresholded by MetaballLayer into a clean outline. It squashes and stretches with movement and hits,
    /// and weapon cells (also soft sprites on the same layer) merge into it. The radius is the fixed body radius,
    /// so it never reads HP.
    /// </summary>
    public class SwarmVisual : MonoBehaviour, IHitVisual
    {
        [SerializeField] SwarmBody body;
        [SerializeField] string metaballLayerName = "Metaball";
        [SerializeField] Color color = new Color(0.30f, 0.85f, 0.55f, 1f);
        [Tooltip("How strongly a hit squashes the ball (1 = default).")]
        [SerializeField] float hitSquash = 1f;

        [Header("Debug")]
        [SerializeField] bool showAimMarker = true;

        readonly SoftDeform _deform = new SoftDeform();
        Rigidbody2D _rb;
        Transform _ball;
        Transform _aimMarker;
        Material _material;
        int _layer;
        float _flash;
        SpriteRenderer _ballSr;

        public Color Color => color;
        public int MetaballLayerIndex => _layer;

        void Awake()
        {
            if (body == null) body = GetComponent<SwarmBody>();
            _rb = GetComponent<Rigidbody2D>();

            _layer = LayerMask.NameToLayer(metaballLayerName);
            if (_layer < 0)
            {
                Debug.LogError($"SwarmVisual: layer '{metaballLayerName}' does not exist. " +
                               "Run the menu Cellstorm > Phase 1 > Add Metaball Layer.");
                _layer = 0;
            }

            _material = SpriteMaterials.Default;

            var go = new GameObject("SoftBall");
            go.layer = _layer;
            _ball = go.transform;
            _ballSr = go.AddComponent<SpriteRenderer>();
            _ballSr.sprite = ProceduralSprites.Soft;
            _ballSr.sharedMaterial = _material;
            _ballSr.color = color;

            if (showAimMarker)
            {
                var m = new GameObject("AimMarker");
                var sr = m.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSprites.Hard;
                sr.sharedMaterial = _material;
                sr.color = new Color(1f, 1f, 1f, 0.9f);
                sr.sortingOrder = 20;
                m.transform.localScale = Vector3.one * 0.2f;
                _aimMarker = m.transform;
            }

            if (body != null && body.Stats != null) body.Stats.Damaged += OnDamaged;
        }

        void OnDestroy()
        {
            if (body != null && body.Stats != null) body.Stats.Damaged -= OnDamaged;
            if (_ball != null) Destroy(_ball.gameObject);
            if (_aimMarker != null) Destroy(_aimMarker.gameObject);
        }

        void OnDamaged(float amount)
        {
            _flash = 0.1f;
        }

        void Update()
        {
            if (body == null) return;

            if (body.IsDead)
            {
                if (_ball.gameObject.activeSelf) _ball.gameObject.SetActive(false);
                if (_aimMarker != null && _aimMarker.gameObject.activeSelf) _aimMarker.gameObject.SetActive(false);
                return;
            }
            if (!_ball.gameObject.activeSelf) _ball.gameObject.SetActive(true);

            Vector2 vel = _rb != null ? _rb.linearVelocity : Vector2.zero;
            _deform.Tick(Time.deltaTime, vel, Time.time);

            float R = body.Radius;
            _ball.position = new Vector3(body.transform.position.x, body.transform.position.y, 0f);
            _deform.Apply(_ball, 2f * R / ProceduralSprites.SoftVisibleRadiusFactor);

            if (_flash > 0f) _flash -= Time.deltaTime;
            var tinted = body != null ? body.Stats.Status.Tint(color) : color;     // green Toxic, blue Slow, yellow Stun flicker
            _ballSr.color = _flash > 0f ? Color.Lerp(tinted, Color.white, 0.55f) : tinted;

            if (_aimMarker != null)
            {
                Vector2 p = (Vector2)body.transform.position + body.AimDirection * (R + 0.22f);
                _aimMarker.position = new Vector3(p.x, p.y, 0f);
            }
        }

        /// <summary>Hit reaction: squash toward the point and ring back.</summary>
        public void Hit(Vector2 worldPoint, float strength)
        {
            if (body == null) return;
            Vector2 dir = worldPoint - (Vector2)body.transform.position;
            _deform.Hit(dir, strength * 0.06f * hitSquash);
        }

        /// <summary>Debug-friendly overload used by the old debug keys. Negative strength = default.</summary>
        public void Dent(Vector2 worldPoint, float strength = -1f, float reachFactor = 0.9f)
        {
            Hit(worldPoint, strength < 0f ? 5f : strength);
        }
    }
}
