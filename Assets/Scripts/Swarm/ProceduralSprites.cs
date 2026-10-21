using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>Runtime-generated sprites so no art assets are needed yet.</summary>
    public static class ProceduralSprites
    {
        /// <summary>
        /// The soft sprite's alpha is (1 - d^2)^2. At alpha 0.5 (the default threshold) a single dot
        /// shows up with this fraction of the sprite radius. Used to size dots so that
        /// "dot radius" means the visible radius.
        /// </summary>
        public const float SoftVisibleRadiusFactor = 0.5412f;

        const int Size = 64;
        static Sprite _soft;
        static Sprite _hard;

        /// <summary>1 unit wide radial falloff sprite (the metaball field).</summary>
        public static Sprite Soft
        {
            get
            {
                if (_soft == null) _soft = Build(soft: true);
                return _soft;
            }
        }

        /// <summary>1 unit wide solid circle with a 1px antialiased edge.</summary>
        public static Sprite Hard
        {
            get
            {
                if (_hard == null) _hard = Build(soft: false);
                return _hard;
            }
        }

        static Sprite Build(bool soft)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float nx = (x + 0.5f) / Size * 2f - 1f;
                    float ny = (y + 0.5f) / Size * 2f - 1f;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);

                    float a;
                    if (soft)
                    {
                        float t = Mathf.Clamp01(1f - d * d);
                        a = t * t;
                    }
                    else
                    {
                        a = Mathf.Clamp01((1f - d) * Size * 0.5f);
                    }
                    pixels[y * Size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
