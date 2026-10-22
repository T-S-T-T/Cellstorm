using Cellstorm.Core;
using Cellstorm.Swarm;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// TEMPORARY test harness for the Phase 1 decision gate.
    /// 1/2/3/4/5 = 5 / 50 / 500 / 2000 / 10000 cells, = and - double / halve,
    /// left mouse = simulate a hit on the side you are aiming at (dent).
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

            if (kb.digit1Key.wasPressedThisFrame) body.SetCellCount(5);
            if (kb.digit2Key.wasPressedThisFrame) body.SetCellCount(50);
            if (kb.digit3Key.wasPressedThisFrame) body.SetCellCount(500);
            if (kb.digit4Key.wasPressedThisFrame) body.SetCellCount(2000);
            if (kb.digit5Key.wasPressedThisFrame) body.SetCellCount(10000);
            if (kb.equalsKey.wasPressedThisFrame) body.SetCellCount(body.CellCount * 2);
            if (kb.minusKey.wasPressedThisFrame) body.SetCellCount(body.CellCount / 2);

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
            GUI.Label(new Rect(10, 35, 800, 25),
                $"Cells: {body.CellCount}  (simulated dots: {visual})   Radius: {body.Radius:F2}   FPS: {_fps:F0}");
            GUI.Label(new Rect(10, 55, 800, 25),
                "1/2/3/4/5 = 5/50/500/2000/10000 cells   = / - = double / halve   ` (backtick) = dent");
        }
    }
}
