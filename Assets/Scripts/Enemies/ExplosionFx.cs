using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Enemies
{
    /// <summary>Placeholder explosion visual: an expanding, fading circle.</summary>
    public class ExplosionFx : MonoBehaviour
    {
        const float Duration = 0.25f;

        float _t;
        float _radius;
        Color _color;
        SpriteRenderer _sr;
        Material _mat;

        public static void Spawn(Vector2 position, float radius, Color color)
        {
            var go = new GameObject("ExplosionFx");
            go.transform.position = position;
            go.AddComponent<ExplosionFx>().Init(radius, color);
        }

        void Init(float radius, Color color)
        {
            _radius = radius;
            _color = color;

            _mat = new Material(Shader.Find("Sprites/Default"));
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralSprites.Hard;
            _sr.sharedMaterial = _mat;
            _sr.sortingOrder = 15;
            Apply(0f);
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / Duration);
            Apply(k);
            if (k >= 1f) Destroy(gameObject);
        }

        void Apply(float k)
        {
            float size = Mathf.Lerp(0.2f, _radius * 2f, k);
            transform.localScale = new Vector3(size, size, 1f);
            _sr.color = new Color(_color.r, _color.g, _color.b, 0.8f * (1f - k));
        }

        void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }
    }
}
