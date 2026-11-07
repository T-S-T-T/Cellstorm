using Cellstorm.Debugging;
using Cellstorm.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cellstorm.World
{
    /// <summary>
    /// Adds the V0.2 world systems to the Game scene at runtime, so the existing scene keeps working without edits:
    /// the resource point director, the hazard director, the off-screen arrows (and, in the editor and development
    /// builds, the V0.2 debug panel). Runs on every scene load and only acts in a scene that has a PlayerBuild.
    /// </summary>
    public static class WorldBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void First()
        {
            Ensure();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Ensure();
        }

        static void Ensure()
        {
            if (Object.FindAnyObjectByType<PlayerBuild>() == null) return;      // not the Game scene
            if (Object.FindAnyObjectByType<ResourcePointDirector>() != null) return;

            var go = new GameObject("V0.2 World");
            go.AddComponent<ResourcePointDirector>();
            go.AddComponent<HazardDirector>();
            go.AddComponent<ResourcePointHud>();
            if (DebugGate.Allowed) go.AddComponent<V02DebugPanel>();
        }
    }
}
