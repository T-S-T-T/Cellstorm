using Cellstorm.Core;
using UnityEngine;

namespace Cellstorm.Debugging
{
    /// <summary>TEMPORARY. Minimal main menu so the Boot -> MainMenu -> Game flow works.</summary>
    public class MainMenuStub : MonoBehaviour
    {
        void OnGUI()
        {
            float w = 200f, h = 50f;
            var rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            if (GUI.Button(rect, "Play"))
                SceneLoader.LoadGame();
        }
    }
}
