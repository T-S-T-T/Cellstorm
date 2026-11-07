using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// Small visual helpers for V0.2 weapons: the Spore Cloud (a translucent green disc that follows its owner),
    /// the Shockwave ring (an expanding ring) and a plain timed ring. Flat shapes only, no final art (GDD section 11).
    /// </summary>
    public class SporeCloudFx : MonoBehaviour
    {
        Transform _follow;
        float _radius;
        SpriteRenderer _disc;
        SpriteRenderer _ring;

        public static SporeCloudFx Spawn(Transform follow, float radius, Color color)
        {
            var go = new GameObject("SporeCloud");
            go.transform.position = follow.position;
            var fx = go.AddComponent<SporeCloudFx>();
            fx._follow = follow;
            fx._radius = radius;

            fx._disc = Child(go.transform, ProceduralSprites.Hard, radius * 2f, 3);
            fx._disc.color = new Color(color.r, color.g, color.b, 0.22f);
            fx._ring = Child(go.transform, FxSprites.Ring, radius * 2f, 3);
            fx._ring.color = new Color(color.r, color.g, color.b, 0.55f);
            return fx;
        }

        static SpriteRenderer Child(Transform parent, Sprite sprite, float diameter, int order)
        {
            var c = new GameObject("Part");
            c.transform.SetParent(parent, false);
            c.transform.localScale = new Vector3(diameter, diameter, 1f);
            var sr = c.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = SpriteMaterials.Default;
            sr.sortingOrder = order;
            return sr;
        }

        void Update()
        {
            if (_follow == null)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = _follow.position;
            float wobble = 1f + 0.03f * Mathf.Sin(Time.time * 6f);
            transform.localScale = new Vector3(wobble, wobble, 1f);
        }

        public void End()
        {
            Destroy(gameObject);
        }
    }

    /// <summary>An expanding, fading ring (Shockwave burst).</summary>
    public class RingFx : MonoBehaviour
    {
        float _t, _duration, _radius;
        Color _color;
        SpriteRenderer _sr;

        public static void Spawn(Vector2 position, float radius, float duration, Color color)
        {
            var go = new GameObject("RingFx");
            go.transform.position = position;
            var fx = go.AddComponent<RingFx>();
            fx._duration = Mathf.Max(0.05f, duration);
            fx._radius = radius;
            fx._color = color;
            fx._sr = go.AddComponent<SpriteRenderer>();
            fx._sr.sprite = FxSprites.Ring;
            fx._sr.sharedMaterial = SpriteMaterials.Default;
            fx._sr.sortingOrder = 14;
            fx.Apply(0f);
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _duration);
            Apply(k);
            if (k >= 1f) Destroy(gameObject);
        }

        void Apply(float k)
        {
            float d = Mathf.Lerp(0.3f, _radius * 2f, 1f - (1f - k) * (1f - k));
            transform.localScale = new Vector3(d, d, 1f);
            _sr.color = new Color(_color.r, _color.g, _color.b, 0.9f * (1f - k));
        }
    }
}
