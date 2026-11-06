using Cellstorm.Combat;
using Cellstorm.Core;
using Cellstorm.Swarm;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// TEMPORARY test harness for the player swarm.
    /// 1/2/3/4/5 = max HP 20 / 100 / 500 / 2000 / 10000 (full HP), = and - double / halve max HP,
    /// [ = body size x1, ] = body size x2 (Giant), backquote = simulate a hit on the side you are aiming at (dent).
    /// The size never changes with HP: change HP and watch the radius stay the same.
    /// </summary>
    public class SwarmDebugControls : MonoBehaviour
    {
        [SerializeField] SwarmBody body;

        SwarmVisual _visual;
        float _fps = 60f;

        void Start()
        {
            if (body == null) body = FindAnyObjectByType<SwarmBody>();
            if (body != null) _visual = body.GetComponent<SwarmVisual>();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) _fps = Mathf.Lerp(_fps, 1f / dt, 0.05f);

            var kb = Keyboard.current;
            if (kb == null || body == null) return;

            if (kb.digit1Key.wasPressedThisFrame) body.SetMaxHp(20f);
            if (kb.digit2Key.wasPressedThisFrame) body.SetMaxHp(100f);
            if (kb.digit3Key.wasPressedThisFrame) body.SetMaxHp(500f);
            if (kb.digit4Key.wasPressedThisFrame) body.SetMaxHp(2000f);
            if (kb.digit5Key.wasPressedThisFrame) body.SetMaxHp(10000f);
            if (kb.equalsKey.wasPressedThisFrame) body.SetMaxHp(body.Stats.MaxHp * 2f);
            if (kb.minusKey.wasPressedThisFrame) body.SetMaxHp(body.Stats.MaxHp * 0.5f);
            if (kb.leftBracketKey.wasPressedThisFrame) body.SetSizeMultiplier(1f);
            if (kb.rightBracketKey.wasPressedThisFrame) body.SetSizeMultiplier(2f);

            if (_visual != null && kb.backquoteKey.wasPressedThisFrame)
            {
                Vector2 hit = (Vector2)body.transform.position + body.AimDirection * body.SmoothedRadius;
                _visual.Dent(hit);
            }
        }

        void OnGUI()
        {
            if (!DebugVisibility.Show) return;
            if (body == null) return;
            int visual = _visual != null ? _visual.ActiveVisualCells : 0;
            var s = body.Stats;
            GUI.Label(new Rect(10, 35, 1100, 25),
                $"HP: {s.Hp:F1}/{s.MaxHp:F0}  Radius: {body.Radius:F3} (x{body.SizeMultiplier:F0}, collider {body.ColliderRadius:F3})  " +
                $"Move speed: {body.MoveSpeed:F1} u/s  (dots: {visual})   FPS: {_fps:F0}");
            GUI.Label(new Rect(10, 55, 1100, 25),
                "1-5 = max HP 20/100/500/2000/10000   = / - = double / halve   [ / ] = body size x1 / x2   ` (backtick) = dent");
        }
    }
}
