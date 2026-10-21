using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cellstorm.Core
{
    /// <summary>Scene names must match the scene file names and be added to Build Settings.</summary>
    public static class SceneLoader
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string Game = "Game";

        public static void LoadMainMenu() => Load(MainMenu);
        public static void LoadGame() => Load(Game);

        static void Load(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
