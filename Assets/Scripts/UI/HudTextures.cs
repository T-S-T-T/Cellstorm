using UnityEngine;

namespace Cellstorm.UI
{
    /// <summary>Procedurally generated HUD shapes (heart, EXP arrow, pie wedges). No art needed yet.</summary>
    public static class HudTextures
    {
        const int PieSteps = 64;

        static Texture2D _heart;
        static Texture2D _arrow;
        static readonly Texture2D[] Pies = new Texture2D[PieSteps + 1];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _heart = null;
            _arrow = null;
            for (int i = 0; i < Pies.Length; i++) Pies[i] = null;
        }

        public static Texture2D Heart
        {
            get
            {
                if (_heart == null) _heart = BuildHeart();
                return _heart;
            }
        }

        public static Texture2D Arrow
        {
            get
            {
                if (_arrow == null) _arrow = BuildArrow();
                return _arrow;
            }
        }

        /// <summary>White wedge covering 'fill' (0..1) of a square, starting at the top, clockwise.</summary>
        public static Texture2D Pie(float fill)
        {
            int step = Mathf.Clamp(Mathf.RoundToInt(fill * PieSteps), 0, PieSteps);
            if (Pies[step] == null) Pies[step] = BuildPie(step / (float)PieSteps);
            return Pies[step];
        }

        static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        static Texture2D BuildHeart()
        {
            const int size = 128;
            var tex = NewTexture(size, size);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = ((x + 0.5f) / size * 2f - 1f) * 1.3f;
                    float ny = ((y + 0.5f) / size * 2f - 1f) * 1.3f + 0.08f;
                    float a = nx * nx + ny * ny - 1f;
                    float f = a * a * a - nx * nx * ny * ny * ny;
                    px[y * size + x] = new Color(1f, 1f, 1f, f <= 0f ? 1f : 0f);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            return tex;
        }

        static Texture2D BuildArrow()
        {
            const int w = 64, h = 96;
            var tex = NewTexture(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = ((x + 0.5f) / w * 2f - 1f);   // -1..1
                    float ny = ((y + 0.5f) / h);              // 0 (bottom) .. 1 (top)
                    bool inside;
                    if (ny < 0.5f) inside = Mathf.Abs(nx) <= 0.28f;                    // shaft
                    else inside = Mathf.Abs(nx) <= (1f - ny) / 0.5f * 0.95f;           // head
                    px[y * w + x] = new Color(1f, 1f, 1f, inside ? 1f : 0f);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            return tex;
        }

        static Texture2D BuildPie(float fill)
        {
            const int size = 64;
            var tex = NewTexture(size, size);
            var px = new Color[size * size];
            float c = (size - 1) * 0.5f;
            float limit = fill * Mathf.PI * 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    float angle = Mathf.Atan2(dx, dy);        // 0 at the top, increasing clockwise
                    if (angle < 0f) angle += Mathf.PI * 2f;
                    px[y * size + x] = new Color(1f, 1f, 1f, angle <= limit && fill > 0f ? 1f : 0f);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            return tex;
        }
    }

    /// <summary>Screen positions of the HUD elements (shared by the HUD and swap mode).</summary>
    public static class HudLayout
    {
        public const float HeartSize = 112f;
        public const float SlotSize = 60f;
        public const float Gap = 10f;
        public const float Margin = 18f;

        public static Rect Heart()
        {
            float cx = Screen.width * 0.5f;
            return new Rect(cx - HeartSize * 0.5f, Screen.height - Margin - HeartSize, HeartSize, HeartSize);
        }

        /// <summary>0 = LMB weapon, 1 = RMB weapon (left of the heart), 2 = Q skill, 3 = E skill (right).</summary>
        public static Rect Slot(int slot)
        {
            var h = Heart();
            float y = Screen.height - Margin - SlotSize;
            float x;
            switch (slot)
            {
                case 0: x = h.x - 24f - SlotSize - Gap - SlotSize; break;
                case 1: x = h.x - 24f - SlotSize; break;
                case 2: x = h.xMax + 110f; break;
                default: x = h.xMax + 110f + SlotSize + Gap; break;
            }
            return new Rect(x, y, SlotSize, SlotSize);
        }

        public static Rect DefLabel()
        {
            var h = Heart();
            return new Rect(h.xMax + 14f, h.y + HeartSize * 0.38f, 96f, 34f);
        }

        public static Rect MenuButton()
        {
            return new Rect(12f, 12f, 92f, 36f);
        }

        public static Rect ExpArrow()
        {
            return new Rect(Screen.width - 76f, 14f, 48f, 72f);
        }
    }
}
