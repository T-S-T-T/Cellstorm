using Cellstorm.Combat;
using Cellstorm.Debugging;
using Cellstorm.Swarm;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cellstorm.EditorTools
{
    public static class Phase2Setup
    {
        [MenuItem("Cellstorm/Phase 2/Setup Game Scene")]
        public static void SetupScene()
        {
            var body = Object.FindAnyObjectByType<SwarmBody>();
            if (body == null)
            {
                EditorUtility.DisplayDialog("Cellstorm",
                    "No SwarmBody found in the open scene. Open the Game scene and run Phase 1 setup first.", "OK");
                return;
            }

            GetOrAdd<SwarmStats>(body.gameObject);
            GetOrAdd<SwarmCombat>(body.gameObject);

            if (Object.FindAnyObjectByType<CombatDebug>() == null)
                new GameObject("CombatDebug").AddComponent<CombatDebug>();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = body.gameObject;
            Debug.Log("Cellstorm Phase 2 scene setup done. Save the scene and press Play.");
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
