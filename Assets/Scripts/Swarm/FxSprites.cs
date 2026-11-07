using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>Procedural 1-unit sprites for effects: a ring and an arrow. Scale = diameter in units.</summary>
    public static class FxSprites
    {
        const int Size = 128;
        static Sprite _ring;
        static Sprite _arrow;
        static Texture2D _arrowTex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _ring = null;
            _arrow = null;
            _arrowTex = null;
        }

        /// <summary>A thin glowing ring that touches the sprite edge (outer radius 0.5 units).</summary>
        public static Sprite Ring
        {
            get
            {
                if (_ring == null)
                {
                    var tex = NewTexture();
                    var px = new Color[Size * Size];
                    for (int y = 0; y < Size; y++)
                    {
                        for (int x = 0; x < Size; x++)
                        {
                            float nx = (x + 0.5f) / Size * 2f - 1f;
                            float ny = (y + 0.5f) / Size * 2f - 1f;
                            float d = Mathf.Sqrt(nx * nx + ny * ny);
                            float band = Mathf.Clamp01(1f - Mathf.Abs(d - 0.93f) / 0.07f);
                            float glow = d < 0.93f ? Mathf.Clamp01((d - 0.6f) / 0.33f) * 0.25f : 0f;
                            float a = d > 1f ? 0f : Mathf.Max(band, glow);
                            px[y * Size + x] = new Color(1f, 1f, 1f, a);
                        }
                    }
                    tex.SetPixels(px);
                    tex.Apply(false, false);
                    _ring = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
                    _ring.hideFlags = HideFlags.HideAndDontSave;
                }
                return _ring;
            }
        }

        /// <summary>A triangle pointing along +X (rotate it to point elsewhere).</summary>
        public static Sprite Arrow
        {
            get
            {
                if (_arrow == null)
                {
                    _arrowTex = BuildArrow();
                    _arrow = Sprite.Create(_arrowTex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
                    _arrow.hideFlags = HideFlags.HideAndDontSave;
                }
                return _arrow;
            }
        }

        /// <summary>The same arrow as a texture, for GUI drawing.</summary>
        public static Texture2D ArrowTexture
        {
            get
            {
                var _ = Arrow;
                return _arrowTex;
            }
        }

        static Texture2D BuildArrow()
        {
            var tex = NewTexture();
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float nx = (x + 0.5f) / Size * 2f - 1f;      // -1..1, +x = tip
                    float ny = (y + 0.5f) / Size * 2f - 1f;
                    // triangle: tip at (0.9, 0), base at x = -0.7 with half height 0.8
                    float t = (0.9f - nx) / 1.6f;                // 0 at tip, 1 at base
                    bool inside = t >= 0f && t <= 1f && Mathf.Abs(ny) <= 0.8f * t;
                    px[y * Size + x] = new Color(1f, 1f, 1f, inside ? 1f : 0f);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            return tex;
        }

        static Texture2D NewTexture()
        {
            return new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    }
}
