using UnityEngine;

namespace Cellstorm.Meta
{
    /// <summary>Simple player settings stored in PlayerPrefs (key bindings are saved by InputService).</summary>
    public static class GameSettings
    {
        const string TimerKey = "Cellstorm.ShowRunTimer";

        /// <summary>
        /// The GDD hides milestone progress, so the run timer is off by default. Turn it on in Settings.
        /// </summary>
        public static bool ShowRunTimer
        {
            get => PlayerPrefs.GetInt(TimerKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(TimerKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
