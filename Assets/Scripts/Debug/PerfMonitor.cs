using Cellstorm.Enemies;
using Cellstorm.Progression;
using UnityEngine;

namespace Cellstorm.Debugging
{
    /// <summary>Frame time and object counts, shown with the debug overlay (F12) at the bottom of its text block.</summary>
    public class PerfMonitor : MonoBehaviour
    {
        readonly float[] _samples = new float[240];
        int _index;

        void Update()
        {
            _samples[_index] = Time.unscaledDeltaTime;
            _index = (_index + 1) % _samples.Length;
        }

        void OnGUI()
        {
            if (!DebugVisibility.Show) return;

            float sum = 0f, worst = 0f;
            for (int i = 0; i < _samples.Length; i++)
            {
                sum += _samples[i];
                if (_samples[i] > worst) worst = _samples[i];
            }
            float avg = sum / _samples.Length;
            float memoryMb = System.GC.GetTotalMemory(false) / (1024f * 1024f);

            GUI.Label(new Rect(10, 365, 1500, 25),
                "PERF  avg " + (1f / Mathf.Max(avg, 0.0001f)).ToString("0") + " FPS (" + (avg * 1000f).ToString("0.0") + " ms)   " +
                "worst frame " + (worst * 1000f).ToString("0.0") + " ms   " +
                "enemies " + Enemy.Active.Count + "   pickups " + Pickup.Active.Count + "   GC heap " + memoryMb.ToString("0.0") + " MB");
        }
    }
}
