using UnityEngine;

namespace Cellstorm.Core
{
    /// <summary>
    /// Put on an empty GameObject in the Boot scene. Warms up input, then goes to the main menu.
    /// Later phases: load settings and save data here.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        void Start()
        {
            InputService.Initialize();
            SceneLoader.LoadMainMenu();
        }
    }
}
