using Cellstorm.Debugging;
using Cellstorm.Swarm;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cellstorm.EditorTools
{
    public static class Phase1Setup
    {
        const string LayerName = "Metaball";

        [MenuItem("Cellstorm/Phase 1/Add Metaball Layer")]
        public static void AddLayerMenu()
        {
            if (EnsureLayer(LayerName))
                Debug.Log($"Layer '{LayerName}' is ready.");
            else
                EditorUtility.DisplayDialog("Cellstorm", "No free user layer slot was found (layers 8-31 are full).", "OK");
        }

        [MenuItem("Cellstorm/Phase 1/Setup Game Scene")]
        public static void SetupScene()
        {
            if (!EnsureLayer(LayerName))
            {
                EditorUtility.DisplayDialog("Cellstorm", "Could not add the Metaball layer (no free user layer slot).", "OK");
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("Cellstorm", "No camera tagged MainCamera in the open scene. Open the Game scene first.", "OK");
                return;
            }

            // Camera
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.12f, 1f);
            GetOrAdd<CameraFollow>(cam.gameObject);

            // Player
            var player = GameObject.Find("Player");
            if (player == null) player = new GameObject("Player");

            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null) Object.DestroyImmediate(sr);
            var oldMover = player.GetComponent<PlaceholderPlayerMover>();
            if (oldMover != null) Object.DestroyImmediate(oldMover);

            player.transform.localScale = Vector3.one;
            GetOrAdd<SwarmBody>(player);
            GetOrAdd<SwarmVisual>(player);

            // Metaball pipeline
            if (Object.FindAnyObjectByType<MetaballLayer>() == null)
                new GameObject("PlayerMetaball").AddComponent<MetaballLayer>();

            // Debug harness
            if (Object.FindAnyObjectByType<SwarmDebugControls>() == null)
                new GameObject("SwarmDebug").AddComponent<SwarmDebugControls>();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = player;
            Debug.Log("Cellstorm Phase 1 scene setup done. Save the scene and press Play.");
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static bool EnsureLayer(string layerName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return false;

            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            if (layers == null) return false;

            for (int i = 8; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return true;
            }

            for (int i = 8; i < layers.arraySize; i++)
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    return true;
                }
            }
            return false;
        }
    }
}
