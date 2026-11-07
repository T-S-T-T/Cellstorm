using Cellstorm.Core;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.World
{
    /// <summary>Screen-edge arrows that point to resource points which are off-screen (GDD V0.2 section 10.1).</summary>
    public class ResourcePointHud : MonoBehaviour
    {
        void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State == GameState.LoadoutSelect || ResourcePoint.Active.Count == 0) return;
            var cam = Camera.main;
            if (cam == null) return;
            if (Event.current.type != EventType.Repaint) return;

            GUI.depth = 6;
            float sw = Screen.width, sh = Screen.height;
            var center = new Vector2(sw * 0.5f, sh * 0.5f);
            const float margin = 36f;
            var old = GUI.color;
            var matrix = GUI.matrix;

            foreach (var p in ResourcePoint.Active)
            {
                if (p == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(p.transform.position);
                bool onScreen = sp.z > 0f && sp.x > 0f && sp.x < sw && sp.y > 0f && sp.y < sh;
                if (onScreen) continue;

                // Direction from the screen centre; guiY flips because GUI y goes down.
                Vector2 dir = new Vector2(sp.x - center.x, -(sp.y - center.y));
                if (sp.z < 0f) dir = -dir;
                if (dir.sqrMagnitude < 0.01f) continue;
                dir.Normalize();

                float tx = dir.x != 0f ? (sw * 0.5f - margin) / Mathf.Abs(dir.x) : float.MaxValue;
                float ty = dir.y != 0f ? (sh * 0.5f - margin) / Mathf.Abs(dir.y) : float.MaxValue;
                Vector2 pos = center + dir * Mathf.Min(tx, ty);

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                const float size = 34f;
                GUIUtility.RotateAroundPivot(angle, pos);
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 5f);
                GUI.color = new Color(1f, 0.85f, 0.3f, pulse);
                GUI.DrawTexture(new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size, size), FxSprites.ArrowTexture);
                GUI.matrix = matrix;
            }
            GUI.color = old;
        }
    }
}
