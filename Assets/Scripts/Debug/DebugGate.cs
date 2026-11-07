using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// Debug tools only exist in the editor and in Development Builds (Build Settings > Development Build).
    /// In a release build this removes every component of the Cellstorm.Debugging namespace when a scene loads, and in
    /// debug builds it adds the InvariantMonitor that watches the HP and weapon cell rules.
    /// </summary>
    public static class DebugGate
    {
        public static bool Allowed => Application.isEditor || Debug.isDebugBuild;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Apply();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply();
        }

        static void Apply()
        {
            if (!Allowed)
            {
                var all = Object.FindObjectsByType<MonoBehaviour>();
                for (int i = 0; i < all.Length; i++)
                {
                    var mb = all[i];
                    if (mb != null && mb.GetType().Namespace == "Cellstorm.Debugging") Object.Destroy(mb);
                }
                return;
            }

            if (Object.FindAnyObjectByType<InvariantMonitor>() == null)
                new GameObject("InvariantMonitor").AddComponent<InvariantMonitor>();
        }
    }
}
