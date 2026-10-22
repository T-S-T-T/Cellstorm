using Cellstorm.Core;
using Cellstorm.Debugging;
using Cellstorm.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cellstorm.UI
{
    /// <summary>
    /// Adds the Phase 6 UI automatically when the MainMenu or Game scene loads, so no scene setup is needed.
    /// MainMenu: the real menu replaces the Phase 0 stub. Game: HUD, swap mode, pause menu, run summary and recorder.
    /// </summary>
    public static class UiBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Ensure(SceneManager.GetActiveScene());
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Ensure(scene);
        }

        static void Ensure(Scene scene)
        {
            if (scene.name == SceneLoader.MainMenu)
            {
                if (Object.FindAnyObjectByType<MainMenuScreen>() == null)
                    new GameObject("MainMenuScreen").AddComponent<MainMenuScreen>();

                var stub = Object.FindAnyObjectByType<MainMenuStub>();
                if (stub != null) Object.Destroy(stub);
            }
            else if (scene.name == SceneLoader.Game)
            {
                var go = new GameObject("Phase6UI");
                AddIfMissing<GameHud>(go);
                AddIfMissing<SwapModeController>(go);
                AddIfMissing<PauseMenu>(go);
                AddIfMissing<RunSummaryScreen>(go);
                AddIfMissing<RunRecorder>(go);
                AddIfMissing<PerfMonitor>(go);
                AddIfMissing<BalanceBot>(go);
            }
        }

        static void AddIfMissing<T>(GameObject host) where T : Component
        {
            if (Object.FindAnyObjectByType<T>() == null) host.AddComponent<T>();
        }
    }
}
