using UnityEngine;

namespace Cellstorm.UI
{
    /// <summary>Shared IMGUI styles. Call Ensure() at the start of every OnGUI.</summary>
    public static class UiStyles
    {
        public static GUIStyle Title, Heading, Label, LabelCenter, Small, SmallCenter, Button, Glyph, HeartText, DefText;

        static bool _ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _ready = false;
        }

        public static void Ensure()
        {
            if (_ready) return;
            _ready = true;

            Title = Make(48, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white, false);
            Heading = Make(30, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white, false);
            Label = Make(18, TextAnchor.MiddleLeft, FontStyle.Normal, Color.white, false);
            LabelCenter = Make(18, TextAnchor.MiddleCenter, FontStyle.Normal, Color.white, true);
            Small = Make(14, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.85f, 0.85f, 0.85f), false);
            SmallCenter = Make(14, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.85f, 0.85f, 0.85f), true);
            Glyph = Make(16, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white, false);
            HeartText = Make(34, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white, false);
            DefText = Make(26, TextAnchor.MiddleLeft, FontStyle.Bold, new Color(0.45f, 0.70f, 1f), false);

            Button = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
        }

        static GUIStyle Make(int size, TextAnchor anchor, FontStyle fontStyle, Color color, bool wrap)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                alignment = anchor,
                fontStyle = fontStyle,
                wordWrap = wrap
            };
            s.normal.textColor = color;
            return s;
        }
    }

    public static class UiDraw
    {
        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public static void Border(Rect r, float t, Color c)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c);
            Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c);
            Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        public static void Dim(float alpha)
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, alpha));
        }

        public static bool Button(Rect r, string text)
        {
            return GUI.Button(r, text, UiStyles.Button);
        }

        public static string Time(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }
    }
}
